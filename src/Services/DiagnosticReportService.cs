using System;
using System.IO;
using System.Reflection;
using System.Text;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal static class DiagnosticReportService
    {
        private const string IssueBaseUrl = "https://github.com/Splicee/MV-Media-Downloader/issues/new";

        public static string Build(string area, string log, ToolState tools)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("MV Media Downloader - diagnosticky report");
            report.AppendLine("Cas: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
            report.AppendLine("Cast: " + area);
            report.AppendLine("Verze: " + Assembly.GetExecutingAssembly().GetName().Version);
            report.AppendLine("System: " + Environment.OSVersion + (Environment.Is64BitOperatingSystem ? " x64" : " x86"));
            report.AppendLine("yt-dlp: " + Value(tools == null ? null : tools.YtDlpVersion));
            report.AppendLine("FFmpeg: " + Value(tools == null ? null : tools.FfmpegVersion));
            report.AppendLine("JS runtime: " + Value(tools == null ? null : (tools.JsRuntimeName + " " + tools.JsRuntimeVersion)));
            report.AppendLine();
            report.AppendLine("--- Aktualni log ---");
            report.AppendLine(Tail(log, 50000));
            string errors = ReadTail(AppPaths.ErrorLogPath, 20000);
            if (errors.Length > 0)
            {
                report.AppendLine();
                report.AppendLine("--- Interni chyby ---");
                report.AppendLine(errors);
            }

            return DiagnosticRedactor.Redact(report.ToString());
        }

        public static string Save(string path, string report)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Nebyla vybrána cesta pro diagnostický report.", "path");
            string normalized = string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase) ?
                path :
                Path.ChangeExtension(path, ".txt");
            string directory = Path.GetDirectoryName(Path.GetFullPath(normalized));
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Cílovou složku reportu nelze určit.");
            Directory.CreateDirectory(directory);
            File.WriteAllText(normalized, report ?? "", new UTF8Encoding(false));
            return normalized;
        }

        public static string Create(string area, string log, ToolState tools)
        {
            AppPaths.EnsureDirectories();
            string path = Path.Combine(AppPaths.ReportDirectory, SuggestedFileName(area));
            return Save(path, Build(area, log, tools));
        }

        public static string SuggestedFileName(string area)
        {
            string part = string.IsNullOrWhiteSpace(area) ? "aplikace" : area.Trim().ToLowerInvariant();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                part = part.Replace(invalid, '-');
            string fileName = "mv-media-report-" + part + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
            return fileName;
        }

        public static string BuildIssueUrl(string area, string fileName)
        {
            string title = "Chyba v aplikaci - " + area;
            string body = "Popište prosím, co se stalo a jak lze chybu zopakovat.\r\n\r\n" +
                "Potom do tohoto hlášení přetáhněte uložený diagnostický soubor `" +
                Path.GetFileName(fileName ?? "diagnosticky-report.txt") + "`.";
            return IssueBaseUrl + "?title=" + Uri.EscapeDataString(title) + "&body=" + Uri.EscapeDataString(body);
        }

        public static string BuildEmailUrl(string area, string fileName)
        {
            string subject = "MV Media Downloader - chyba - " + area;
            string body = "Dobrý den,\r\n\r\npopisuji chybu v aplikaci MV Media Downloader.\r\n\r\n" +
                "K této zprávě přikládám uložený diagnostický soubor " +
                Path.GetFileName(fileName ?? "diagnosticky-report.txt") + ".\r\n\r\n" +
                "Popis chyby:\r\n";
            return "mailto:?subject=" + Uri.EscapeDataString(subject) + "&body=" + Uri.EscapeDataString(body);
        }

        public static string Create(string area, string log, ToolState tools, string path)
        {
            string savedPath = Save(path, Build(area, log, tools));
            return savedPath;
        }

        private static string Value(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "nezjisteno" : value.Trim();
        }

        private static string ReadTail(string path, int limit)
        {
            try { return File.Exists(path) ? Tail(File.ReadAllText(path), limit) : ""; }
            catch { return ""; }
        }

        private static string Tail(string text, int limit)
        {
            string value = text ?? "";
            return value.Length <= limit ? value : value.Substring(value.Length - limit);
        }
    }
}
