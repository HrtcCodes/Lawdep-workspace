using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WorkSpace.Core
{
    public static class CryptoService
    {
        private const string AppSecretSalt = "WorkSpace_Team_Key_Salt_2026_#842";

        public static byte[] TeamKey { get; set; }

        public static byte[] DeriveKeyFromLogins(IEnumerable<string> logins)
        {
            var sorted = logins
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim().ToLowerInvariant())
                .Distinct()
                .OrderBy(l => l)
                .ToArray();

            string combined = string.Join("|", sorted) + "|" + AppSecretSalt;
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
            }
        }

        public static string EncryptString(string plainText)
        {
            return EncryptWithKey(plainText, TeamKey);
        }

        public static string DecryptString(string storedText)
        {
            return DecryptWithKey(storedText, TeamKey);
        }

        public static string EncryptWithKey(string plainText, byte[] key)
        {
            if (key == null) throw new InvalidOperationException("Ключ команды не сформирован.");
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = EncryptBytes(plainBytes, key);
            return "ENC:" + Convert.ToBase64String(cipherBytes);
        }

        public static string DecryptWithKey(string storedText, byte[] key)
        {
            if (string.IsNullOrWhiteSpace(storedText)) return storedText;
            if (!storedText.StartsWith("ENC:")) return storedText;

            if (key == null) throw new InvalidOperationException("Нет ключа для расшифровки базы данных.");
            byte[] cipherBytes = Convert.FromBase64String(storedText.Substring(4));
            byte[] plainBytes = DecryptBytes(cipherBytes, key);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private static byte[] EncryptBytes(byte[] data, byte[] key)
        {
            using (var aes = new AesManaged { KeySize = 256, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            {
                aes.GenerateIV();
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(key, aes.IV), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                    }
                    return ms.ToArray();
                }
            }
        }

        private static byte[] DecryptBytes(byte[] cipherData, byte[] key)
        {
            using (var aes = new AesManaged { KeySize = 256, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            {
                byte[] iv = new byte[16];
                Array.Copy(cipherData, 0, iv, 0, 16);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(key, iv), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherData, 16, cipherData.Length - 16);
                    }
                    return ms.ToArray();
                }
            }
        }
    }
}
