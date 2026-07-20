using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MVMediaStudio.Core;
using MVMediaStudio.Services;

namespace MVMediaStudio.Tests
{
    internal static class IntegrationTests
    {
        private static readonly StringBuilder Report = new StringBuilder();
        private static int failures;
        private static string runDirectory;
        private static ToolState tools;

        public static int Main()
        {
            try
            {
                return RunAsync().GetAwaiter().GetResult();
            }
            catch (Exception error)
            {
                Log("FATÁLNÍ CHYBA: " + error);
                failures++;
                SaveReport();
                return 1;
            }
        }

        private static async Task<int> RunAsync()
        {
            string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).FullName;
            runDirectory = Path.Combine(projectRoot, "artifacts", "real-tests", "run-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(runDirectory);
            Log("MV Media Downloader 3.0.4 – reálné integrační testy");
            Log("Start: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Log("Výstup: " + runDirectory);

            ToolService service = new ToolService();
            tools = service.Check();
            if (!tools.HasFfmpeg || !tools.HasFfprobe)
            {
                Log("FFmpeg chybí, spouštím stejnou ověřenou instalaci jako aplikace.");
                await service.InstallFfmpegAsync(ToolProgress);
            }
            if (!tools.HasJsRuntime)
            {
                Log("JS runtime chybí, spouštím instalaci Deno.");
                await service.InstallDenoAsync(ToolProgress);
            }
            tools = service.Check();
            Require(tools.HasYtDlp, "yt-dlp je dostupné");
            Require(tools.HasFfmpeg && tools.HasFfprobe, "FFmpeg a FFprobe jsou dostupné");
            Require(tools.HasJsRuntime, "podporovaný JS runtime je dostupný");
            Log("yt-dlp: " + tools.YtDlpVersion);
            Log("FFmpeg: " + tools.FfmpegVersion);
            Log("JS runtime: " + tools.JsRuntimeName + " " + tools.JsRuntimeVersion);

            List<DownloadCase> downloads = new List<DownloadCase>
            {
                new DownloadCase("D1-w3c-mp4-h264", "https://media.w3.org/wai/perspective-videos/video-captions.mp4", "mp4-h264", "480", ".mp4"),
                new DownloadCase("D2-w3c-sintel-mp3", "https://media.w3.org/2010/05/sintel/trailer.mp4", "audio-mp3", "auto", ".mp3"),
                new DownloadCase("D3-w3c-mkv", "https://media.w3.org/wai/perspective-videos/understandable-content.mp4", "mkv-best", "720", ".mkv"),
                new DownloadCase("D4-w3c-webm", "https://media.w3.org/2010/05/video/movie_300.webm", "webm", "auto", ".webm")
            };

            foreach (DownloadCase test in downloads)
                await RunDownloadAsync(test);

            string fallbackVideo = downloads.Where(item => item.OutputPath != null && !item.OutputPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)).Select(item => item.OutputPath).FirstOrDefault();
            string mp4Input = File.Exists(downloads[0].OutputPath) ? downloads[0].OutputPath : fallbackVideo;
            string mkvInput = File.Exists(downloads[2].OutputPath) ? downloads[2].OutputPath : fallbackVideo;
            Require(!string.IsNullOrWhiteSpace(mp4Input) && File.Exists(mp4Input), "video vstup pro konverze existuje");
            Require(!string.IsNullOrWhiteSpace(mkvInput) && File.Exists(mkvInput), "druhý vstup pro konverze existuje");

            List<ConversionCase> conversions = new List<ConversionCase>
            {
                new ConversionCase("C1-mp4-h264-crf", mp4Input, "mp4", "h264", "crf", "23", "6000k", "H.264"),
                new ConversionCase("C2-mkv-h265-crf", mkvInput, "mkv", "h265", "crf", "28", "6000k", "H.265 / HEVC"),
                new ConversionCase("C3-webm-av1-crf", mp4Input, "webm", "av1", "crf", "28", "6000k", "AV1"),
                new ConversionCase("C4-avi-h264-bitrate", mkvInput, "avi", "h264", "bitrate", "23", "2500k", "H.264")
            };

            foreach (ConversionCase test in conversions)
                await RunConversionAsync(test);

            Log("");
            Log(failures == 0 ? "VÝSLEDEK: Všech 8 reálných testů prošlo." : "VÝSLEDEK: Selhání " + failures + ".");
            Log("Konec: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            SaveReport();
            Console.WriteLine("REPORT=" + Path.Combine(runDirectory, "integration-report.txt"));
            return failures == 0 ? 0 : 1;
        }

        private static async Task RunDownloadAsync(DownloadCase test)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string outputDirectory = Path.Combine(runDirectory, "downloads", test.Name);
            Directory.CreateDirectory(outputDirectory);
            Log("");
            Log("START " + test.Name + " | " + test.Preset + " | " + test.Url);

            DownloadOptions options = new DownloadOptions
            {
                Preset = test.Preset,
                Quality = test.Quality,
                OutputDirectory = outputDirectory,
                Playlist = false,
                Subtitles = false,
                CookiesFromBrowser = false,
                NoOverwrite = true,
                ExtraArguments = "--retries 3 --fragment-retries 3 --socket-timeout 30 --download-sections *0-12 --force-keyframes-at-cuts"
            };
            DownloadUrlResolution resolution = await JojUrlResolver.ResolveAsync(new[] { test.Url });
            List<string> args = DownloadArgumentBuilder.Build(options, resolution.Urls, tools);
            string logPath = Path.Combine(outputDirectory, "yt-dlp.log");
            int exitCode = await ProcessService.RunAsync(
                tools.YtDlpPath,
                args,
                delegate(string line, bool error)
                {
                    lock (Report)
                    {
                        File.AppendAllText(logPath, (error ? "! " : "") + line + Environment.NewLine, Encoding.UTF8);
                    }
                },
                CancellationToken.None);
            stopwatch.Stop();

            string[] mediaFiles = Directory.GetFiles(outputDirectory)
                .Where(path => !path.EndsWith(".log", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(path => new FileInfo(path).Length)
                .ToArray();
            test.OutputPath = mediaFiles.FirstOrDefault();
            bool extensionOk = test.OutputPath != null && test.OutputPath.EndsWith(test.ExpectedExtension, StringComparison.OrdinalIgnoreCase);
            bool fileOk = test.OutputPath != null && new FileInfo(test.OutputPath).Length > 1024;
            Pass(exitCode == 0 && extensionOk && fileOk, test.Name + " stažení", "exit=" + exitCode + ", soubor=" + (test.OutputPath ?? "žádný"));
            if (fileOk && test.ExpectedExtension != ".mp3")
            {
                MediaInfo media = MediaProbeService.Probe(tools.FfprobePath, test.OutputPath);
                Pass(media.Width > 0 && media.Height > 0 && media.DurationSeconds > 0, test.Name + " FFprobe", media.TechnicalSummary + ", " + media.DurationSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
            }
            Log("Čas: " + stopwatch.Elapsed.TotalSeconds.ToString("0.0") + " s");
        }

        private static async Task RunConversionAsync(ConversionCase test)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string outputDirectory = Path.Combine(runDirectory, "conversions", test.Name);
            Directory.CreateDirectory(outputDirectory);
            Log("");
            Log("START " + test.Name + " | " + test.Format + " / " + test.Codec + " / " + test.RateControl);
            MediaInfo sourceInfo = MediaProbeService.Probe(tools.FfprobePath, test.InputPath);
            Log("Zdroj: " + sourceInfo.TechnicalSummary);

            ConversionOptions options = new ConversionOptions
            {
                InputPath = test.InputPath,
                OutputDirectory = outputDirectory,
                Format = test.Format,
                Codec = test.Codec,
                RateControl = test.RateControl,
                Crf = test.Crf,
                VideoBitrate = test.VideoBitrate,
                AudioBitrate = "192k"
            };
            string outputPath;
            List<string> args = ConversionArgumentBuilder.Build(options, out outputPath);
            string logPath = Path.Combine(outputDirectory, "ffmpeg.log");
            int exitCode = await ProcessService.RunAsync(
                tools.FfmpegPath,
                args,
                delegate(string line, bool error)
                {
                    lock (Report)
                    {
                        File.AppendAllText(logPath, (error ? "! " : "") + line + Environment.NewLine, Encoding.UTF8);
                    }
                },
                CancellationToken.None);
            stopwatch.Stop();

            MediaInfo outputInfo = File.Exists(outputPath) ? MediaProbeService.Probe(tools.FfprobePath, outputPath) : new MediaInfo();
            bool converted = exitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 1024;
            bool codecOk = string.Equals(outputInfo.Codec, test.ExpectedCodec, StringComparison.OrdinalIgnoreCase);
            Pass(converted && codecOk, test.Name + " konverze", "exit=" + exitCode + ", kodek=" + outputInfo.Codec + ", soubor=" + outputPath);

            if (converted)
            {
                List<string> decodeArgs = new List<string> { "-v", "error", "-i", outputPath, "-f", "null", "-" };
                int decodeExit = await ProcessService.RunAsync(tools.FfmpegPath, decodeArgs, delegate(string line, bool error) { File.AppendAllText(logPath, (error ? "! " : "") + line + Environment.NewLine, Encoding.UTF8); }, CancellationToken.None);
                Pass(decodeExit == 0, test.Name + " úplné dekódování", "exit=" + decodeExit);
            }
            Log("Výstup: " + outputInfo.TechnicalSummary);
            Log("Čas: " + stopwatch.Elapsed.TotalSeconds.ToString("0.0") + " s");
        }

        private static void ToolProgress(double progress, string message)
        {
            Console.WriteLine(message + " " + progress.ToString("0") + " %");
        }

        private static void Require(bool condition, string name)
        {
            if (!condition)
                throw new InvalidOperationException("Nesplněná podmínka: " + name);
            Log("OK: " + name);
        }

        private static void Pass(bool condition, string name, string detail)
        {
            if (condition)
                Log("OK: " + name + " | " + detail);
            else
            {
                Log("CHYBA: " + name + " | " + detail);
                failures++;
            }
        }

        private static void Log(string line)
        {
            lock (Report)
            {
                Report.AppendLine(line);
                Console.WriteLine(line);
            }
        }

        private static void SaveReport()
        {
            if (string.IsNullOrWhiteSpace(runDirectory))
                return;
            File.WriteAllText(Path.Combine(runDirectory, "integration-report.txt"), Report.ToString(), Encoding.UTF8);
        }

        private sealed class DownloadCase
        {
            public readonly string Name;
            public readonly string Url;
            public readonly string Preset;
            public readonly string Quality;
            public readonly string ExpectedExtension;
            public string OutputPath;

            public DownloadCase(string name, string url, string preset, string quality, string expectedExtension)
            {
                Name = name;
                Url = url;
                Preset = preset;
                Quality = quality;
                ExpectedExtension = expectedExtension;
            }
        }

        private sealed class ConversionCase
        {
            public readonly string Name;
            public readonly string InputPath;
            public readonly string Format;
            public readonly string Codec;
            public readonly string RateControl;
            public readonly string Crf;
            public readonly string VideoBitrate;
            public readonly string ExpectedCodec;

            public ConversionCase(string name, string inputPath, string format, string codec, string rateControl, string crf, string videoBitrate, string expectedCodec)
            {
                Name = name;
                InputPath = inputPath;
                Format = format;
                Codec = codec;
                RateControl = rateControl;
                Crf = crf;
                VideoBitrate = videoBitrate;
                ExpectedCodec = expectedCodec;
            }
        }
    }
}
