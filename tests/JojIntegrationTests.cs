using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MVMediaStudio.Core;
using MVMediaStudio.Services;

namespace MVMediaStudio.Tests
{
    internal static class JojIntegrationTests
    {
        private const string ArchiveUrl = "https://www.joj.sk/relacie/7-krimi/epizody";

        public static int Main()
        {
            try
            {
                return RunAsync().GetAwaiter().GetResult();
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                return 1;
            }
        }

        private static async Task<int> RunAsync()
        {
            string root = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).FullName;
            string outputDirectory = Path.Combine(root, "artifacts", "joj-test", "run-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(outputDirectory);
            string reportPath = Path.Combine(outputDirectory, "joj-report.txt");
            StringBuilder report = new StringBuilder();

            ToolService service = new ToolService();
            ToolState tools = service.Check();
            if (!tools.HasFfmpeg || !tools.HasFfprobe)
                await service.InstallFfmpegAsync(null);
            tools = service.Check();
            Require(tools.HasYtDlp, "Chybí yt-dlp.");
            Require(tools.HasFfmpeg && tools.HasFfprobe, "Chybí FFmpeg nebo FFprobe.");

            string episodeUrl = await FindCurrentEpisodeAsync();
            DownloadUrlResolution resolution = await JojUrlResolver.ResolveAsync(new[] { episodeUrl });
            Require(resolution.Urls.Count == 1 && resolution.Urls[0].StartsWith("https://media.joj.sk/embed/", StringComparison.OrdinalIgnoreCase), "Odkaz epizody se nepřevedl na veřejný přehrávač JOJ.");
            report.AppendLine("Zdroj: " + episodeUrl);
            report.AppendLine("Převedeno: " + resolution.Urls[0]);

            DownloadOptions options = new DownloadOptions
            {
                Preset = "mp4-h264",
                Quality = "720",
                OutputDirectory = outputDirectory,
                Playlist = false,
                Subtitles = false,
                CookiesFromBrowser = false,
                NoOverwrite = true,
                ExtraArguments = "--retries 3 --socket-timeout 30 --download-sections *0-8 --force-keyframes-at-cuts"
            };
            List<string> arguments = DownloadArgumentBuilder.Build(options, resolution.Urls, tools);
            int exitCode = await ProcessService.RunAsync(
                tools.YtDlpPath,
                arguments,
                delegate(string line, bool error)
                {
                    lock (report)
                        report.AppendLine((error ? "! " : "") + line);
                },
                CancellationToken.None);

            string mediaPath = Directory.GetFiles(outputDirectory, "*.mp4").OrderByDescending(path => new FileInfo(path).Length).FirstOrDefault();
            Require(exitCode == 0, "yt-dlp skončil s kódem " + exitCode + ".");
            Require(mediaPath != null && new FileInfo(mediaPath).Length > 1024, "Nevznikl platný MP4 soubor.");

            MediaInfo media = MediaProbeService.Probe(tools.FfprobePath, mediaPath);
            Require(media.Width > 0 && media.Height > 0 && media.DurationSeconds > 0, "FFprobe nepotvrdil platné video.");
            report.AppendLine("Výsledek: " + media.TechnicalSummary + " · " + media.DurationSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
            report.AppendLine("STAV: TEST PROŠEL");
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
            Console.WriteLine("JOJ test prošel.");
            Console.WriteLine("REPORT=" + reportPath);
            return 0;
        }

        private static async Task<string> FindCurrentEpisodeAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("MV-Media-Downloader-Integration-Test/1.0");
                string html = await client.GetStringAsync(ArchiveUrl);
                Match match = Regex.Match(
                    html,
                    @"https://www\.joj\.sk/relacia/7-krimi/epizoda/[0-9]+-[^""'<>\s]+",
                    RegexOptions.IgnoreCase);
                Require(match.Success, "Ve veřejném archivu JOJ nebyla nalezena aktuální epizoda.");
                return match.Value;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
