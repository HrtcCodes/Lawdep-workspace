using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace WorkSpace.Core
{
    public class NetworkStorage
    {
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public string GetFilePath(string fileName)
        {
            return Path.Combine(AppConfig.DataDirectory, GetStoredFileName(fileName));
        }

        private string GetStoredFileName(string fileName)
        {
            if (fileName.Equals("team.json", StringComparison.OrdinalIgnoreCase)) return "team.dat";
            if (fileName.Equals("tasks.json", StringComparison.OrdinalIgnoreCase)) return "tasks.dat";
            if (fileName.Equals("registries.json", StringComparison.OrdinalIgnoreCase)) return "registries.dat";
            return fileName;
        }

        public DateTime GetLastModified(string fileName)
        {
            string path = GetFilePath(fileName);
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }

        private bool IsTeamFile(string fileName)
        {
            return fileName.Equals("team.json", StringComparison.OrdinalIgnoreCase) ||
                   fileName.Equals("team.dat", StringComparison.OrdinalIgnoreCase);
        }

        public T Read<T>(string fileName) where T : new()
        {
            string path = GetFilePath(fileName);
            if (!File.Exists(path)) return new T();

            byte[] storedBytes = File.ReadAllBytes(path);
            if (storedBytes.Length == 0) return new T();

            string json;
            if (IsTeamFile(fileName))
            {
                json = Encoding.UTF8.GetString(Decompress(storedBytes));
            }
            else
            {
                string encrypted = Encoding.UTF8.GetString(storedBytes);
                string compressedBase64 = CryptoService.DecryptString(encrypted);
                byte[] compressed = Convert.FromBase64String(compressedBase64);
                json = Encoding.UTF8.GetString(Decompress(compressed));
            }

            return _serializer.Deserialize<T>(json);
        }

        public void Update<T>(string fileName, Action<T> updateAction) where T : new()
        {
            string targetPath = GetFilePath(fileName);
            string lockPath = targetPath + ".lock";
            string tempPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            string bakPath = targetPath + ".bak";

            int attempts = 0;
            while (true)
            {
                try
                {
                    using (var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1024, FileOptions.DeleteOnClose))
                    {
                        T data = Read<T>(fileName);
                        updateAction(data);

                        byte[] storedBytes = SerializeForStorage(fileName, data);
                        File.WriteAllBytes(tempPath, storedBytes);

                        if (File.Exists(targetPath))
                        {
                            if (File.Exists(bakPath)) File.Delete(bakPath);
                            File.Replace(tempPath, targetPath, bakPath);
                        }
                        else
                        {
                            File.Move(tempPath, targetPath);
                        }

                        LogAudit(fileName);
                        break;
                    }
                }
                catch (IOException)
                {
                    attempts++;
                    if (attempts > 100) throw new Exception("Файл заблокирован другим пользователем слишком долго.");
                    Thread.Sleep(50);
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }
        }

        // Direct replacement is used when data must be re-encrypted after a team-key change.
        public void Overwrite<T>(string fileName, T newData)
        {
            string targetPath = GetFilePath(fileName);
            string lockPath = targetPath + ".lock";
            string tempPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            string bakPath = targetPath + ".bak";

            int attempts = 0;
            while (true)
            {
                try
                {
                    using (var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1024, FileOptions.DeleteOnClose))
                    {
                        byte[] storedBytes = SerializeForStorage(fileName, newData);
                        File.WriteAllBytes(tempPath, storedBytes);

                        if (File.Exists(targetPath))
                        {
                            if (File.Exists(bakPath)) File.Delete(bakPath);
                            File.Replace(tempPath, targetPath, bakPath);
                        }
                        else
                        {
                            File.Move(tempPath, targetPath);
                        }

                        LogAudit(fileName);
                        break;
                    }
                }
                catch (IOException)
                {
                    attempts++;
                    if (attempts > 100) throw new Exception("Файл заблокирован другим пользователем слишком долго.");
                    Thread.Sleep(50);
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }
        }

        private byte[] SerializeForStorage<T>(string fileName, T data)
        {
            string json = _serializer.Serialize(data);
            byte[] compressed = Compress(Encoding.UTF8.GetBytes(json));

            if (IsTeamFile(fileName))
                return compressed;

            // Keep the existing encryption implementation unchanged; feed it the compressed payload.
            string compressedBase64 = Convert.ToBase64String(compressed);
            string encrypted = CryptoService.EncryptString(compressedBase64);
            return Encoding.UTF8.GetBytes(encrypted);
        }

        private byte[] Compress(byte[] input)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
                    gzip.Write(input, 0, input.Length);
                return output.ToArray();
            }
        }

        private byte[] Decompress(byte[] input)
        {
            using (var inputStream = new MemoryStream(input))
            using (var gzip = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        private void LogAudit(string fileName)
        {
            try
            {
                string logPath = GetFilePath("audit.log");
                string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1} изменил {2}{3}",
                    DateTime.Now, AppConfig.CurrentUser, fileName, Environment.NewLine);
                File.AppendAllText(logPath, line, Encoding.UTF8);
            }
            catch { }
        }
    }
}
