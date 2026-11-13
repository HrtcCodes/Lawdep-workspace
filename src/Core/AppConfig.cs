using System;
using System.IO;

namespace WorkSpace.Core
{
    public static class AppConfig
    {
        public static string CurrentUser { get { return Environment.UserName; } }
        public static string DataDirectory { get; private set; }

        public static void Load(string[] args)
        {
            string sourceDir = AppDomain.CurrentDomain.BaseDirectory;

            if (args != null && args.Length >= 2 && args.GetValue(0).ToString() == "--local")
            {
                string passedPath = args.GetValue(1).ToString();
                if (Directory.Exists(passedPath))
                {
                    sourceDir = passedPath;
                }
            }

            DataDirectory = Path.Combine(sourceDir, "data");

            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);
        }
    }
}
