using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal static class DirectDownloadService
    {
        private static readonly HttpClient Client = CreateClient();

        public static async Task<string> DownloadAsync(
            DirectDownloadItem item,
            string outputDirectory,
            bool noOverwrite,
            Func<long> rateLimitBytes,
            Action<DirectDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(outputDirectory);
            string safeName = SafeFileName(item.FileName);
            string finalPath = UniquePath(outputDirectory, safeName, noOverwrite);
            if (noOverwrite && File.Exists(finalPath))
            {
                long existingSize = new FileInfo(finalPath).Length;
                Report(progress, item, finalPath, existingSize, existingSize, 0, true, true, false);
                return finalPath;
            }

            string partPath = finalPath + ".part";
            long existing = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, item.DownloadUrl))
            {
                request.Headers.UserAgent.ParseAdd(AppInfo.UserAgent);
                if (existing > 0)
                    request.Headers.Range = new RangeHeaderValue(existing, null);

                using (HttpResponseMessage response = await Client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    bool resumed = existing > 0 && response.StatusCode == HttpStatusCode.PartialContent;
                    if (!resumed)
                        existing = 0;
                    long responseLength = response.Content.Headers.ContentLength ?? -1;
                    long total = responseLength > 0 ? existing + responseLength : item.ExpectedSize;
                    FileMode mode = resumed ? FileMode.Append : FileMode.Create;
                    using (Stream input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                    using (FileStream output = new FileStream(
                        partPath,
                        mode,
                        FileAccess.Write,
                        FileShare.Read,
                        65536,
                        FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        byte[] buffer = new byte[65536];
                        long received = existing;
                        long windowBytes = 0;
                        long activeLimit = 0;
                        Stopwatch speedWatch = Stopwatch.StartNew();
                        Stopwatch reportWatch = Stopwatch.StartNew();
                        while (true)
                        {
                            int read = await input.ReadAsync(
                                buffer,
                                0,
                                buffer.Length,
                                cancellationToken).ConfigureAwait(false);
                            if (read <= 0)
                                break;
                            await output.WriteAsync(
                                buffer,
                                0,
                                read,
                                cancellationToken).ConfigureAwait(false);
                            received += read;
                            windowBytes += read;

                            long limit = rateLimitBytes == null ? 0 : Math.Max(0, rateLimitBytes());
                            if (limit != activeLimit)
                            {
                                activeLimit = limit;
                                windowBytes = 0;
                                speedWatch.Restart();
                            }
                            await ThrottleAsync(
                                windowBytes,
                                activeLimit,
                                speedWatch,
                                cancellationToken).ConfigureAwait(false);

                            if (reportWatch.ElapsedMilliseconds >= 250)
                            {
                                double speed = speedWatch.Elapsed.TotalSeconds > 0
                                    ? windowBytes / speedWatch.Elapsed.TotalSeconds
                                    : 0;
                                Report(progress, item, finalPath, received, total, speed, false, false, resumed);
                                reportWatch.Restart();
                            }
                        }
                        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                        output.Flush(true);
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Move(partPath, finalPath);
            long size = new FileInfo(finalPath).Length;
            Report(progress, item, finalPath, size, size, 0, true, false, existing > 0);
            return finalPath;
        }

        private static HttpClient CreateClient()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            return new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        private static async Task ThrottleAsync(
            long bytes,
            long limit,
            Stopwatch watch,
            CancellationToken cancellationToken)
        {
            if (limit <= 0 || bytes <= 0)
                return;
            double expectedMilliseconds = bytes * 1000d / limit;
            while (expectedMilliseconds > watch.Elapsed.TotalMilliseconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int delay = (int)Math.Min(200, expectedMilliseconds - watch.Elapsed.TotalMilliseconds);
                if (delay > 0)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                else
                    break;
            }
        }

        private static string SafeFileName(string value)
        {
            string name = string.IsNullOrWhiteSpace(value) ? "download.bin" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            name = name.Trim('.', ' ');
            return string.IsNullOrWhiteSpace(name) ? "download.bin" : name;
        }

        private static string UniquePath(string directory, string fileName, bool noOverwrite)
        {
            string path = Path.Combine(directory, fileName);
            if (noOverwrite || (!File.Exists(path) && !File.Exists(path + ".part")))
                return path;
            string stem = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            for (int number = 2; number < 10000; number++)
            {
                string candidate = Path.Combine(directory, stem + " (" + number + ")" + extension);
                if (!File.Exists(candidate) && !File.Exists(candidate + ".part"))
                    return candidate;
            }
            return Path.Combine(directory, stem + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension);
        }

        private static void Report(
            Action<DirectDownloadProgress> callback,
            DirectDownloadItem item,
            string outputPath,
            long received,
            long total,
            double speed,
            bool completed,
            bool skipped,
            bool resumed)
        {
            if (callback == null)
                return;
            callback(new DirectDownloadProgress
            {
                Provider = item.Provider,
                FileName = item.FileName,
                OutputPath = outputPath,
                BytesReceived = received,
                TotalBytes = total,
                BytesPerSecond = speed,
                Completed = completed,
                Skipped = skipped,
                Resumed = resumed
            });
        }
    }

    internal static class DirectMediaPostProcessService
    {
        public static async Task<DirectPostProcessResult> ProcessAsync(
            string ffmpegPath,
            string ffprobePath,
            string sourcePath,
            string preset,
            string quality,
            bool subtitles,
            bool noOverwrite,
            bool preserveInput,
            Action<DirectPostProcessProgress> progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ffmpegPath) || !File.Exists(ffmpegPath))
                throw new FileNotFoundException("FFmpeg není připravený pro převod staženého souboru.", ffmpegPath);

            MediaInfo media = await Task.Run(
                delegate { return MediaProbeService.Probe(ffprobePath, sourcePath); },
                cancellationToken);
            DirectPostProcessPlan plan = DirectMediaArgumentBuilder.Build(
                sourcePath,
                preset,
                quality,
                subtitles,
                noOverwrite,
                preserveInput,
                media);

            if (plan.ExistingOutput)
            {
                return new DirectPostProcessResult
                {
                    OutputPath = plan.OutputPath,
                    ProfileLabel = plan.ProfileLabel,
                    Skipped = true
                };
            }
            if (!plan.Required)
            {
                return new DirectPostProcessResult
                {
                    OutputPath = sourcePath,
                    ProfileLabel = plan.ProfileLabel,
                    Skipped = preserveInput
                };
            }

            StringBuilder diagnostics = new StringBuilder();
            object diagnosticsLock = new object();
            int exitCode = await ProcessService.RunAsync(
                ffmpegPath,
                plan.Arguments,
                delegate(string line, bool isError)
                {
                    double percentage;
                    if (TryReadProgress(line, plan.DurationSeconds, out percentage))
                    {
                        if (progress != null)
                            progress(new DirectPostProcessProgress { Percentage = percentage, ProfileLabel = plan.ProfileLabel });
                        return;
                    }
                    if (!isError)
                        return;
                    lock (diagnosticsLock)
                    {
                        diagnostics.AppendLine(line);
                        if (diagnostics.Length > 12000)
                            diagnostics.Remove(0, diagnostics.Length - 9000);
                    }
                },
                cancellationToken);

            if (exitCode == -2)
            {
                DeleteTemporary(plan.WorkingOutputPath);
                throw new OperationCanceledException(cancellationToken);
            }
            if (exitCode != 0)
            {
                DeleteTemporary(plan.WorkingOutputPath);
                string detail;
                lock (diagnosticsLock)
                    detail = Tail(diagnostics.ToString(), 1600);
                throw new InvalidOperationException(
                    "FFmpeg nedokončil převod do profilu " + plan.ProfileLabel + "." +
                    (string.IsNullOrWhiteSpace(detail) ? "" : "\n" + detail));
            }
            if (!File.Exists(plan.WorkingOutputPath) || new FileInfo(plan.WorkingOutputPath).Length == 0)
            {
                DeleteTemporary(plan.WorkingOutputPath);
                throw new InvalidOperationException("FFmpeg nevytvořil výsledný soubor.");
            }

            try
            {
                if (plan.ReplaceInput)
                {
                    string backupPath = sourcePath + ".mvbackup-" + Guid.NewGuid().ToString("N");
                    File.Replace(plan.WorkingOutputPath, sourcePath, backupPath, true);
                    DeleteTemporary(backupPath);
                }
                else
                {
                    File.Move(plan.WorkingOutputPath, plan.OutputPath);
                    if (!plan.PreserveInput)
                        DeleteTemporary(sourcePath);
                }
            }
            catch
            {
                DeleteTemporary(plan.WorkingOutputPath);
                throw;
            }

            return new DirectPostProcessResult
            {
                OutputPath = plan.OutputPath,
                ProfileLabel = plan.ProfileLabel,
                Processed = true
            };
        }

        private static bool TryReadProgress(string line, double durationSeconds, out double percentage)
        {
            percentage = 0;
            if (durationSeconds <= 0 ||
                (!line.StartsWith("out_time_ms=", StringComparison.OrdinalIgnoreCase) &&
                 !line.StartsWith("out_time_us=", StringComparison.OrdinalIgnoreCase)))
                return false;
            int split = line.IndexOf('=');
            double microseconds;
            if (split < 0 || !double.TryParse(
                line.Substring(split + 1),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out microseconds))
                return false;
            percentage = Math.Max(0, Math.Min(99, microseconds / 1000000d / durationSeconds * 100d));
            return true;
        }

        private static string Tail(string value, int maximumLength)
        {
            string text = (value ?? "").Trim();
            return text.Length <= maximumLength ? text : text.Substring(text.Length - maximumLength);
        }

        private static void DeleteTemporary(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;
            try { File.Delete(path); } catch { }
        }
    }
}
