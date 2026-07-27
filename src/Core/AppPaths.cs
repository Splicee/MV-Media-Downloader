using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MVMediaStudio.Core
{
    internal static class AppPaths
    {
        public static readonly string DataDirectory = SelectDataDirectory();
        public static readonly string BinDirectory = Path.Combine(DataDirectory, "bin");
        public static readonly string LogDirectory = Path.Combine(DataDirectory, "logs");
        public static readonly string ReportDirectory = Path.Combine(DataDirectory, "reports");
        public static readonly string SettingsPath = Path.Combine(DataDirectory, "settings.ini");
        public static readonly string WebshareSessionPath = Path.Combine(DataDirectory, "webshare.session");
        public static readonly string DownloadLogPath = Path.Combine(LogDirectory, "download.log");
        public static readonly string ConversionLogPath = Path.Combine(LogDirectory, "conversion.log");
        public static readonly string ErrorLogPath = Path.Combine(LogDirectory, "errors.log");
        public static readonly string UpdateDirectory = Path.Combine(DataDirectory, "updates");
        public static readonly string DefaultDownloadDirectory = SelectDefaultDownloadDirectory();

        public static string ExecutableDirectory
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(BinDirectory);
            Directory.CreateDirectory(LogDirectory);
            Directory.CreateDirectory(ReportDirectory);
            Directory.CreateDirectory(UpdateDirectory);
            Directory.CreateDirectory(DefaultDownloadDirectory);
        }

        public static void WriteError(Exception error)
        {
            try
            {
                EnsureDirectories();
                string detail = error == null ? "Neznámá chyba" : DiagnosticRedactor.Redact(error.ToString());
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    detail + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(ErrorLogPath, line, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static string SelectDataDirectory()
        {
            List<string> candidates = new List<string>();
            string configured = Environment.GetEnvironmentVariable("MV_MEDIA_DOWNLOADER_DATA_DIR");
            if (!string.IsNullOrWhiteSpace(configured))
                candidates.Add(Path.GetFullPath(configured));
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(local))
                candidates.Add(Path.Combine(local, "MV", "MediaDownloader"));
            candidates.Add(Path.Combine(ExecutableDirectory, "data"));
            candidates.Add(Path.Combine(Path.GetTempPath(), "MVMediaDownloader"));

            foreach (string candidate in candidates)
            {
                try
                {
                    Directory.CreateDirectory(candidate);
                    string test = Path.Combine(candidate, ".write-test.tmp");
                    File.WriteAllText(test, "ok", Encoding.ASCII);
                    File.Delete(test);
                    return candidate;
                }
                catch
                {
                }
            }

            return Path.Combine(Path.GetTempPath(), "MVMediaDownloader");
        }

        private static string SelectDefaultDownloadDirectory()
        {
            if (!string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("MV_MEDIA_DOWNLOADER_DATA_DIR")))
                return Path.Combine(DataDirectory, "downloads");
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "MV Media Downloader");
        }
    }
}
