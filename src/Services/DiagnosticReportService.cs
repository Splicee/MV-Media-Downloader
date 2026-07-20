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

        public static string Create(string area, string log, ToolState tools)
        {
            AppPaths.EnsureDirectories();
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

            string safe = DiagnosticRedactor.Redact(report.ToString());
            string fileName = "report-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
            string path = Path.Combine(AppPaths.ReportDirectory, fileName);
            File.WriteAllText(path, safe, new UTF8Encoding(false));
            return path;
        }

        public static string BuildIssueUrl(string area, string report)
        {
            string title = "Chyba v aplikaci - " + area;
            string body = "Automaticky pripraveny ocisteny diagnosticky souhrn.\r\n\r\n" + Tail(report, 1700) +
                "\r\n\r\nKompletni report je zkopirovany ve schrance a lze jej vlozit do komentare.";
            return IssueBaseUrl + "?title=" + Uri.EscapeDataString(title) + "&body=" + Uri.EscapeDataString(body);
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
