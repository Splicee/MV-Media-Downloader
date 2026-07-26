using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal sealed class ToolService
    {
        private const string YtDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        private const string YtDlpChecksumsUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS";
        private const string FfmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
        private const string FfmpegChecksumUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip.sha256";
        private const string DenoUrl = "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";
        private const string DenoChecksumUrl = "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip.sha256sum";

        public ToolService()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        }

        public ToolState Check()
        {
            AppPaths.EnsureDirectories();
            ToolState state = new ToolState();
            state.YtDlpPath = FindExecutable("yt-dlp.exe");
            state.FfmpegPath = FindExecutable("ffmpeg.exe");
            state.FfprobePath = FindExecutable("ffprobe.exe");
            string pluginDirectory = Path.Combine(AppPaths.ExecutableDirectory, "yt-dlp-plugins");
            if (Directory.Exists(pluginDirectory))
                state.PluginDirectory = pluginDirectory;

            if (!string.IsNullOrWhiteSpace(state.YtDlpPath))
                state.YtDlpVersion = FirstLine(ProcessService.Capture(state.YtDlpPath, new[] { "--version" }, 8000));
            if (!string.IsNullOrWhiteSpace(state.FfmpegPath))
                state.FfmpegVersion = ShortFfmpegVersion(ProcessService.Capture(state.FfmpegPath, new[] { "-version" }, 8000));

            string[] runtimes = new[] { "deno.exe", "node.exe" };
            foreach (string runtime in runtimes)
            {
                string path = FindExecutable(runtime);
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                string version = FirstLine(ProcessService.Capture(path, new[] { "--version" }, 8000));
                if (!IsSupportedRuntime(runtime, version))
                    continue;
                state.JsRuntimePath = path;
                state.JsRuntimeName = Path.GetFileNameWithoutExtension(runtime);
                state.JsRuntimeVersion = version;
                break;
            }
            return state;
        }

        public async Task InstallYtDlpAsync(Action<double, string> progress)
        {
            AppPaths.EnsureDirectories();
            string tempPath = Path.Combine(AppPaths.BinDirectory, "yt-dlp.download");
            string finalPath = Path.Combine(AppPaths.BinDirectory, "yt-dlp.exe");
            try
            {
                Report(progress, 0, "Stahuji yt-dlp…");
                await DownloadFileAsync(YtDlpUrl, tempPath, progress, "yt-dlp");
                string sums = await DownloadTextAsync(YtDlpChecksumsUrl);
                string expected = FindHashForFile(sums, "yt-dlp.exe");
                VerifyHash(tempPath, expected);
                File.Copy(tempPath, finalPath, true);
                Report(progress, 100, "yt-dlp je připravené");
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        public async Task InstallFfmpegAsync(Action<double, string> progress)
        {
            AppPaths.EnsureDirectories();
            string archivePath = Path.Combine(AppPaths.BinDirectory, "ffmpeg.download.zip");
            try
            {
                Report(progress, 0, "Stahuji FFmpeg…");
                await DownloadFileAsync(FfmpegUrl, archivePath, progress, "FFmpeg");
                string checksumText = await DownloadTextAsync(FfmpegChecksumUrl);
                VerifyHash(archivePath, ExtractFirstHash(checksumText));
                Report(progress, 92, "Rozbaluji FFmpeg…");
                ExtractExecutables(archivePath, new[] { "ffmpeg.exe", "ffprobe.exe" });
                Report(progress, 100, "FFmpeg je připravený");
            }
            finally
            {
                TryDelete(archivePath);
            }
        }

        public async Task InstallDenoAsync(Action<double, string> progress)
        {
            AppPaths.EnsureDirectories();
            string archivePath = Path.Combine(AppPaths.BinDirectory, "deno.download.zip");
            try
            {
                Report(progress, 0, "Stahuji Deno runtime…");
                await DownloadFileAsync(DenoUrl, archivePath, progress, "Deno");
                string checksumText = await DownloadTextAsync(DenoChecksumUrl);
                VerifyHash(archivePath, ExtractFirstHash(checksumText));
                Report(progress, 92, "Rozbaluji Deno…");
                ExtractExecutables(archivePath, new[] { "deno.exe" });
                Report(progress, 100, "Deno runtime je připravený");
            }
            finally
            {
                TryDelete(archivePath);
            }
        }

        private static async Task DownloadFileAsync(string url, string target, Action<double, string> progress, string label)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.UserAgent, AppInfo.UserAgent);
                int lastPercentage = -1;
                client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs eventArgs)
                {
                    if (eventArgs.ProgressPercentage == lastPercentage)
                        return;
                    lastPercentage = eventArgs.ProgressPercentage;
                    Report(progress, Math.Min(90, eventArgs.ProgressPercentage * 0.9), "Stahuji " + label + "…");
                };
                await client.DownloadFileTaskAsync(new Uri(url), target);
            }
        }

        private static async Task<string> DownloadTextAsync(string url)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.UserAgent, AppInfo.UserAgent);
                return await client.DownloadStringTaskAsync(new Uri(url));
            }
        }

        private static void ExtractExecutables(string archivePath, IEnumerable<string> names)
        {
            HashSet<string> wanted = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            HashSet<string> extracted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fileName = Path.GetFileName(entry.FullName);
                    if (!wanted.Contains(fileName))
                        continue;
                    string target = Path.Combine(AppPaths.BinDirectory, fileName);
                    using (Stream input = entry.Open())
                    using (FileStream output = File.Create(target))
                        input.CopyTo(output);
                    extracted.Add(fileName);
                }
            }

            if (wanted.Any(name => !extracted.Contains(name)))
                throw new InvalidDataException("Archiv neobsahuje všechny očekávané nástroje.");
        }

        private static string FindExecutable(string fileName)
        {
            string[] direct = new[]
            {
                Path.Combine(AppPaths.BinDirectory, fileName),
                Path.Combine(AppPaths.ExecutableDirectory, fileName),
                Path.Combine(AppPaths.ExecutableDirectory, "bin", fileName)
            };
            foreach (string path in direct)
                if (File.Exists(path))
                    return path;

            string pathValue = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string folder in pathValue.Split(Path.PathSeparator))
            {
                try
                {
                    string candidate = Path.Combine(folder.Trim().Trim('\"'), fileName);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch
                {
                }
            }
            return "";
        }

        private static void VerifyHash(string path, string expected)
        {
            if (string.IsNullOrWhiteSpace(expected))
                throw new InvalidDataException("Nepodařilo se získat kontrolní SHA-256 součet.");
            string actual;
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("SHA-256 staženého souboru nesouhlasí.");
        }

        private static string FindHashForFile(string text, string fileName)
        {
            foreach (string line in (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                if (line.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return ExtractFirstHash(line);
            return "";
        }

        private static string ExtractFirstHash(string text)
        {
            Match match = Regex.Match(text ?? "", "(?i)\\b[a-f0-9]{64}\\b");
            return match.Success ? match.Value : "";
        }

        private static string FirstLine(string text)
        {
            return (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        }

        private static string ShortFfmpegVersion(string text)
        {
            Match match = Regex.Match(FirstLine(text), "ffmpeg version\\s+([^\\s]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : FirstLine(text);
        }

        private static bool IsSupportedRuntime(string fileName, string version)
        {
            Match match = Regex.Match(version ?? "", "([0-9]+)(?:\\.([0-9]+))?");
            int major;
            int minor = 0;
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out major))
                return false;
            if (match.Groups[2].Success)
                int.TryParse(match.Groups[2].Value, out minor);
            if (fileName.Equals("deno.exe", StringComparison.OrdinalIgnoreCase))
                return major > 2 || (major == 2 && minor >= 3);
            if (fileName.Equals("node.exe", StringComparison.OrdinalIgnoreCase))
                return major >= 22;
            return false;
        }

        private static void Report(Action<double, string> progress, double value, string message)
        {
            if (progress != null)
                progress(value, message);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
