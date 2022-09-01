using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Commandr
{
    public class Updater
    {
        public static void DoUpdate()
        {
            if (Checker.NeedsUpdate())
                new Updater().Update();
            else
                MessageBox.Show("No update needed.");
        }

        private void Update()
        {
            var latest = Checker.GetLatestVersion();

            var tempPath = Path.Combine(Path.GetTempPath(), "Commandr");

            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

            Directory.CreateDirectory(tempPath);

            var processInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Directory.GetCurrentDirectory(), "7za.exe"),
                Arguments = String.Format("x {0} -o{1}", Path.Combine(Configuration.Default.InstallationPath, latest), tempPath)
            };

            var process = new Process { StartInfo = processInfo };

            process.Start();
            process.WaitForExit();

            File.WriteAllText(Path.Combine(tempPath, Checker.LastVersionFile), latest);

            Process.Start(Path.Combine(tempPath, "install.bat"), Directory.GetCurrentDirectory());

            Environment.Exit(0);
        }
    }

    public static class Checker
    {
        internal static String LastVersionFile = "LastVersion.log";

        public static Boolean NeedsUpdate()
        {
            try
            {
                if (!File.Exists(LastVersionFile))
                    return true;

                String currentVersion = GetLatestVersion();

                String lastVersion = File.ReadAllText(LastVersionFile);

                if (lastVersion != currentVersion)
                    return true;
            }
            catch (Exception ex)
            {
                Framework.Utils.HandleException(ex);
            }

            return false;
        }

        public static String GetLatestVersion()
        {
            var files = new DirectoryInfo(Configuration.Default.InstallationPath).GetFiles("*.pkg.7z").OrderByDescending(f => f.LastWriteTime);

            if (!files.Any())
                throw new Exception("No package found.");

            var latestFile = files.First();

            return latestFile.Name;
        }
    }
}