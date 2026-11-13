using System;
using System.IO;
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
            return Path.Combine(AppConfig.DataDirectory, fileName);
        }

        public DateTime GetLastModified(string fileName)
        {
            string path = GetFilePath(fileName);
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }

        private bool IsHybridTeamFile(string fileName)
        {
            return fileName.Equals("team.json", StringComparison.OrdinalIgnoreCase);
        }

        public T Read<T>(string fileName) where T : new()
        {
            string path = GetFilePath(fileName);
            if (!File.Exists(path)) return new T();

            string rawContent = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(rawContent)) return new T();

            string json = IsHybridTeamFile(fileName) ? rawContent : CryptoService.DecryptString(rawContent);
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

                        string json = _serializer.Serialize(data);
                        string contentToWrite = IsHybridTeamFile(fileName) ? json : CryptoService.EncryptString(json);

                        File.WriteAllText(tempPath, contentToWrite, Encoding.UTF8);

                        if (File.Exists(targetPath))
                            File.Replace(tempPath, targetPath, bakPath);
                        else
                            File.Move(tempPath, targetPath);

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
            }
        }
        // Прямая перезапись файла без предварительного чтения (нужно при смене ключа шифрования)
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
                        string json = _serializer.Serialize(newData);
                        string contentToWrite = IsHybridTeamFile(fileName) ? json : CryptoService.EncryptString(json);

                        File.WriteAllText(tempPath, contentToWrite, Encoding.UTF8);

                        if (File.Exists(targetPath))
                            File.Replace(tempPath, targetPath, bakPath);
                        else
                            File.Move(tempPath, targetPath);

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
