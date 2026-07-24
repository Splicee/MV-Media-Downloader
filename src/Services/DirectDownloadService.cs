using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal static class DirectDownloadService
    {
        public static Task<string> DownloadAsync(
            DirectDownloadItem item,
            string outputDirectory,
            bool noOverwrite,
            Func<long> rateLimitBytes,
            Action<DirectDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            return Task.Run(delegate
            {
                Directory.CreateDirectory(outputDirectory);
                string safeName = SafeFileName(item.FileName);
                string finalPath = UniquePath(outputDirectory, safeName, noOverwrite);
                if (noOverwrite && File.Exists(finalPath))
                {
                    Report(progress, item, finalPath, new FileInfo(finalPath).Length, new FileInfo(finalPath).Length, 0, true, true, false);
                    return finalPath;
                }

                string partPath = finalPath + ".part";
                long existing = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
                HttpWebRequest request = CreateRequest(item.DownloadUrl, existing);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    bool resumed = existing > 0 && response.StatusCode == HttpStatusCode.PartialContent;
                    if (!resumed)
                        existing = 0;
                    long responseLength = response.ContentLength;
                    long total = responseLength > 0 ? existing + responseLength : item.ExpectedSize;
                    FileMode mode = resumed ? FileMode.Append : FileMode.Create;
                    using (Stream input = response.GetResponseStream())
                    using (FileStream output = new FileStream(partPath, mode, FileAccess.Write, FileShare.Read, 65536, FileOptions.SequentialScan))
                    {
                        byte[] buffer = new byte[65536];
                        long received = existing;
                        long windowBytes = 0;
                        long activeLimit = 0;
                        Stopwatch speedWatch = Stopwatch.StartNew();
                        Stopwatch reportWatch = Stopwatch.StartNew();
                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            int read = input.Read(buffer, 0, buffer.Length);
                            if (read <= 0)
                                break;
                            output.Write(buffer, 0, read);
                            received += read;
                            windowBytes += read;

                            long limit = rateLimitBytes == null ? 0 : Math.Max(0, rateLimitBytes());
                            if (limit != activeLimit)
                            {
                                activeLimit = limit;
                                windowBytes = 0;
                                speedWatch.Restart();
                            }
                            Throttle(windowBytes, activeLimit, speedWatch, cancellationToken);

                            if (reportWatch.ElapsedMilliseconds >= 250)
                            {
                                double speed = speedWatch.Elapsed.TotalSeconds > 0 ? windowBytes / speedWatch.Elapsed.TotalSeconds : 0;
                                Report(progress, item, finalPath, received, total, speed, false, false, resumed);
                                reportWatch.Restart();
                            }
                        }
                        output.Flush(true);
                    }
                }

                if (File.Exists(finalPath))
                    File.Delete(finalPath);
                File.Move(partPath, finalPath);
                long size = new FileInfo(finalPath).Length;
                Report(progress, item, finalPath, size, size, 0, true, false, existing > 0);
                return finalPath;
            }, cancellationToken);
        }

        private static HttpWebRequest CreateRequest(string url, long existing)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "MV-Media-Downloader/3.1.0";
            request.AllowAutoRedirect = true;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;
            if (existing > 0)
                request.AddRange(existing);
            return request;
        }

        private static void Throttle(long bytes, long limit, Stopwatch watch, CancellationToken cancellationToken)
        {
            if (limit <= 0 || bytes <= 0)
                return;
            double expectedMilliseconds = bytes * 1000d / limit;
            while (expectedMilliseconds > watch.Elapsed.TotalMilliseconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int delay = (int)Math.Min(200, expectedMilliseconds - watch.Elapsed.TotalMilliseconds);
                if (delay > 0)
                    Thread.Sleep(delay);
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
}
