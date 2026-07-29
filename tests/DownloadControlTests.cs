using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MVMediaStudio.Core;
using MVMediaStudio.Services;

namespace MVMediaStudio.Tests
{
    internal static class DownloadControlTests
    {
        private const string ParentMode = "--download-control-parent";
        private const string ChildMode = "--download-control-child";
        private static int failures;

        public static bool TryRunHelper(string[] arguments, out int result)
        {
            result = 0;
            if (arguments == null || arguments.Length == 0)
                return false;
            if (string.Equals(arguments[0], ChildMode, StringComparison.Ordinal))
            {
                Thread.Sleep(30000);
                return true;
            }
            if (!string.Equals(arguments[0], ParentMode, StringComparison.Ordinal) || arguments.Length < 2)
                return false;

            string executable;
            IList<string> childArguments = CreateSelfArguments(
                out executable,
                ChildMode);
            Process child = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = ArgumentUtilities.Join(childArguments),
                UseShellExecute = false,
                CreateNoWindow = true
            });
            File.WriteAllText(arguments[1], child.Id.ToString(), Encoding.ASCII);
            child.WaitForExit();
            return true;
        }

        public static int Run()
        {
            failures = 0;
            TestRateControl();
            TestLiveDirectRateChange();
            TestDirectResume();
            TestNumberedDirectResume();
            TestCompletePartPromotion();
            TestProcessTreeCancellation();
            TestBlockedHttpCancellation();
            return failures;
        }

        private static void TestRateControl()
        {
            DownloadRateControl control = new DownloadRateControl("3000K");
            Check(control.ReadBytesPerSecond() == 3000L * 1024, "limit 3000 odpovídá 3000 KB/s");
            Task.Run(delegate { control.Set("1500K"); }).Wait();
            Check(control.ReadBytesPerSecond() == 1500L * 1024, "změna limitu je viditelná ve stahovacím vlákně");
            control.Set("");
            Check(control.ReadBytesPerSecond() == 0, "vypnutí limitu obnoví neomezenou rychlost");
            Check(
                DownloadRateControl.CanApply(true, true, false, false, true, true),
                "limit lze potvrdit během stahování i před prvním procentním výpisem");
            Check(
                !DownloadRateControl.CanApply(true, true, true, false, true, true),
                "limit nelze měnit během probíhajícího rušení");
        }

        private static void TestLiveDirectRateChange()
        {
            string output = Path.Combine(Path.GetTempPath(), "mv-media-rate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            DownloadRateControl control = new DownloadRateControl("2048K");
            int changed = 0;
            using (PayloadHttpServer server = new PayloadHttpServer(768 * 1024))
            {
                try
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    string path = DirectDownloadService.DownloadAsync(
                        new DirectDownloadItem
                        {
                            Provider = "Lokální test",
                            SourceUrl = server.Url,
                            DownloadUrl = server.Url,
                            FileName = "rate-test.bin",
                            ExpectedSize = 768 * 1024
                        },
                        output,
                        false,
                        control.ReadBytesPerSecond,
                        delegate(DirectDownloadProgress progress)
                        {
                            if (!progress.Completed && Interlocked.Exchange(ref changed, 1) == 0)
                                control.Set("128K");
                        },
                        CancellationToken.None).GetAwaiter().GetResult();
                    watch.Stop();
                    Check(changed == 1, "limit se změní během aktuálního přímého přenosu");
                    Check(File.Exists(path) && new FileInfo(path).Length == 768 * 1024, "změna limitu nepoškodí stažený soubor");
                    Check(watch.ElapsedMilliseconds >= 1100, "nižší limit skutečně zpomalí aktuální přenos");
                }
                finally
                {
                    try { Directory.Delete(output, true); } catch { }
                }
            }
        }

        private static void TestProcessTreeCancellation()
        {
            string marker = Path.Combine(Path.GetTempPath(), "mv-media-child-" + Guid.NewGuid().ToString("N") + ".pid");
            CancellationTokenSource cancellation = new CancellationTokenSource();
            Task<int> running = null;
            int childPid = 0;
            try
            {
                string executable;
                IList<string> parentArguments = CreateSelfArguments(
                    out executable,
                    ParentMode,
                    marker);
                running = ProcessService.RunAsync(
                    executable,
                    parentArguments,
                    null,
                    cancellation.Token);
                if (!WaitForFile(marker, 4000) || !int.TryParse(File.ReadAllText(marker), out childPid))
                {
                    Check(false, "test zrušení spustí pomocný podproces");
                    return;
                }

                Stopwatch watch = Stopwatch.StartNew();
                cancellation.Cancel();
                bool completed = Task.WhenAny(running, Task.Delay(3000)).Result == running;
                watch.Stop();
                int exitCode = completed && !running.IsFaulted && !running.IsCanceled ? running.Result : int.MinValue;
                Check(completed && exitCode == -2 && watch.ElapsedMilliseconds < 2500, "zrušení procesu se vrátí bez čekání");
                Thread.Sleep(250);
                Check(!IsAlive(childPid), "zrušení ukončí i podproces stahovacího nástroje");
            }
            finally
            {
                cancellation.Cancel();
                KillIfAlive(childPid);
                if (running != null)
                {
                    try { running.Wait(2000); } catch { }
                }
                cancellation.Dispose();
                try { File.Delete(marker); } catch { }
            }
        }

        private static void TestDirectResume()
        {
            string output = Path.Combine(Path.GetTempPath(), "mv-media-resume-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            const int totalSize = 384 * 1024;
            const int partialSize = 96 * 1024;
            string finalPath = Path.Combine(output, "resume-test.bin");
            using (FileStream partial = File.Create(finalPath + ".part"))
                partial.SetLength(partialSize);

            using (RangeHttpServer server = new RangeHttpServer(totalSize, false))
            {
                try
                {
                    string result = DirectDownloadService.DownloadAsync(
                        new DirectDownloadItem
                        {
                            Provider = "Lokální test",
                            SourceUrl = server.Url,
                            DownloadUrl = server.Url,
                            FileName = "resume-test.bin",
                            ExpectedSize = totalSize
                        },
                        output,
                        false,
                        null,
                        null,
                        CancellationToken.None).GetAwaiter().GetResult();
                    Check(
                        string.Equals(result, finalPath, StringComparison.OrdinalIgnoreCase),
                        "rozpracovaný soubor se při povoleném přejmenování neopustí");
                    Check(server.RequestedStart == partialSize, "HTTP přenos naváže přesně za uloženou částí");
                    Check(
                        File.Exists(result) &&
                        new FileInfo(result).Length == totalSize &&
                        !File.Exists(finalPath + ".part"),
                        "navázaný přenos vytvoří jeden úplný výsledný soubor");
                }
                finally
                {
                    try { Directory.Delete(output, true); } catch { }
                }
            }
        }

        private static void TestCompletePartPromotion()
        {
            string output = Path.Combine(Path.GetTempPath(), "mv-media-complete-part-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            const int totalSize = 128 * 1024;
            string finalPath = Path.Combine(output, "complete-test.bin");
            using (FileStream partial = File.Create(finalPath + ".part"))
                partial.SetLength(totalSize);

            using (RangeHttpServer server = new RangeHttpServer(totalSize, true))
            {
                try
                {
                    string result = DirectDownloadService.DownloadAsync(
                        new DirectDownloadItem
                        {
                            Provider = "Lokální test",
                            SourceUrl = server.Url,
                            DownloadUrl = server.Url,
                            FileName = "complete-test.bin",
                            ExpectedSize = totalSize
                        },
                        output,
                        false,
                        null,
                        null,
                        CancellationToken.None).GetAwaiter().GetResult();
                    Check(server.RequestedStart == totalSize, "úplná část se ověří požadavkem Range");
                    Check(
                        File.Exists(result) &&
                        new FileInfo(result).Length == totalSize &&
                        !File.Exists(finalPath + ".part"),
                        "HTTP 416 povýší již úplnou část na výsledný soubor");
                }
                finally
                {
                    try { Directory.Delete(output, true); } catch { }
                }
            }
        }

        private static void TestNumberedDirectResume()
        {
            string output = Path.Combine(Path.GetTempPath(), "mv-media-numbered-resume-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            const int totalSize = 256 * 1024;
            const int partialSize = 48 * 1024;
            string originalPath = Path.Combine(output, "resume-test.bin");
            string numberedPath = Path.Combine(output, "resume-test (2).bin");
            File.WriteAllText(originalPath, "původní výsledek");
            using (FileStream partial = File.Create(numberedPath + ".part"))
                partial.SetLength(partialSize);

            using (RangeHttpServer server = new RangeHttpServer(totalSize, false))
            {
                try
                {
                    string result = DirectDownloadService.DownloadAsync(
                        new DirectDownloadItem
                        {
                            Provider = "Lokální test",
                            SourceUrl = server.Url,
                            DownloadUrl = server.Url,
                            FileName = "resume-test.bin",
                            ExpectedSize = totalSize
                        },
                        output,
                        false,
                        null,
                        null,
                        CancellationToken.None).GetAwaiter().GetResult();
                    Check(
                        string.Equals(result, numberedPath, StringComparison.OrdinalIgnoreCase),
                        "číslovaný rozpracovaný soubor dostane přednost před novým názvem");
                    Check(server.RequestedStart == partialSize, "číslovaný .part pokračuje od správného místa");
                    Check(
                        File.Exists(result) &&
                        new FileInfo(result).Length == totalSize &&
                        !File.Exists(numberedPath + ".part"),
                        "číslované navázání dokončí původní rozpracovaný soubor");
                }
                finally
                {
                    try { Directory.Delete(output, true); } catch { }
                }
            }
        }

        private static void TestBlockedHttpCancellation()
        {
            string output = Path.Combine(Path.GetTempPath(), "mv-media-http-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            CancellationTokenSource cancellation = new CancellationTokenSource();
            using (PausedHttpServer server = new PausedHttpServer())
            {
                Task<string> download = null;
                try
                {
                    download = DirectDownloadService.DownloadAsync(
                        new DirectDownloadItem
                        {
                            Provider = "Lokální test",
                            SourceUrl = server.Url,
                            DownloadUrl = server.Url,
                            FileName = "cancel-test.bin",
                            ExpectedSize = 1024 * 1024
                        },
                        output,
                        false,
                        null,
                        null,
                        cancellation.Token);
                    if (!server.HeadersSent.Wait(4000))
                    {
                        Check(false, "lokální test naváže HTTP přenos");
                        return;
                    }
                    Thread.Sleep(300);

                    Stopwatch watch = Stopwatch.StartNew();
                    cancellation.Cancel();
                    bool completed = Task.WhenAny(download, Task.Delay(2500)).Result == download;
                    watch.Stop();
                    bool cancelled = completed && IsCancelled(download);
                    Check(cancelled && watch.ElapsedMilliseconds < 2000, "zrušení přeruší i zablokované síťové čtení");
                }
                finally
                {
                    cancellation.Cancel();
                    server.Release();
                    if (download != null)
                    {
                        try { download.Wait(3000); } catch { }
                    }
                    cancellation.Dispose();
                    try { Directory.Delete(output, true); } catch { }
                }
            }
        }

        private static bool IsCancelled(Task<string> task)
        {
            try
            {
                task.GetAwaiter().GetResult();
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool WaitForFile(string path, int timeoutMilliseconds)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeoutMilliseconds)
            {
                if (File.Exists(path))
                    return true;
                Thread.Sleep(25);
            }
            return false;
        }

        private static IList<string> CreateSelfArguments(out string executable, params string[] arguments)
        {
            executable = Process.GetCurrentProcess().MainModule.FileName;
            List<string> result = new List<string>();
            if (string.Equals(
                Path.GetFileNameWithoutExtension(executable),
                "dotnet",
                StringComparison.OrdinalIgnoreCase))
            {
                result.Add(Assembly.GetExecutingAssembly().Location);
            }
            result.AddRange(arguments);
            return result;
        }

        private static bool IsAlive(int processId)
        {
            if (processId <= 0)
                return false;
            try
            {
                using (Process process = Process.GetProcessById(processId))
                    return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private static void KillIfAlive(int processId)
        {
            if (processId <= 0)
                return;
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    if (!process.HasExited)
                        process.Kill();
                }
            }
            catch { }
        }

        private static void Check(bool condition, string name)
        {
            if (condition)
                Console.WriteLine("OK: " + name);
            else
            {
                failures++;
                Console.WriteLine("CHYBA: " + name);
            }
        }

        private sealed class PausedHttpServer : IDisposable
        {
            private readonly TcpListener listener;
            private readonly ManualResetEventSlim release = new ManualResetEventSlim(false);
            private readonly Task worker;
            private TcpClient client;

            public PausedHttpServer()
            {
                listener = new TcpListener(IPAddress.Loopback, 0);
                HeadersSent = new ManualResetEventSlim(false);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Url = "http://127.0.0.1:" + port + "/media.bin";
                worker = Task.Run((Action)Serve);
            }

            public string Url { get; private set; }
            public ManualResetEventSlim HeadersSent { get; private set; }

            public void Release()
            {
                release.Set();
                try { if (client != null) client.Close(); } catch { }
                try { listener.Stop(); } catch { }
            }

            public void Dispose()
            {
                Release();
                try { worker.Wait(2000); } catch { }
                HeadersSent.Dispose();
                release.Dispose();
            }

            private void Serve()
            {
                try
                {
                    client = listener.AcceptTcpClient();
                    using (NetworkStream stream = client.GetStream())
                    {
                        ReadHeaders(stream);
                        byte[] header = Encoding.ASCII.GetBytes(
                            "HTTP/1.1 200 OK\r\n" +
                            "Content-Length: 1048576\r\n" +
                            "Content-Type: application/octet-stream\r\n" +
                            "Connection: close\r\n\r\n");
                        stream.Write(header, 0, header.Length);
                        stream.Flush();
                        HeadersSent.Set();
                        release.Wait(10000);
                    }
                }
                catch
                {
                    HeadersSent.Set();
                }
            }

            private static void ReadHeaders(Stream stream)
            {
                int matched = 0;
                byte[] end = { 13, 10, 13, 10 };
                while (matched < end.Length)
                {
                    int value = stream.ReadByte();
                    if (value < 0)
                        return;
                    matched = value == end[matched] ? matched + 1 : value == end[0] ? 1 : 0;
                }
            }
        }

        private sealed class PayloadHttpServer : IDisposable
        {
            private readonly TcpListener listener;
            private readonly Task worker;
            private readonly int payloadSize;
            private TcpClient client;

            public PayloadHttpServer(int payloadSize)
            {
                this.payloadSize = payloadSize;
                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Url = "http://127.0.0.1:" + port + "/payload.bin";
                worker = Task.Run((Action)Serve);
            }

            public string Url { get; private set; }

            public void Dispose()
            {
                try { if (client != null) client.Close(); } catch { }
                try { listener.Stop(); } catch { }
                try { worker.Wait(2000); } catch { }
            }

            private void Serve()
            {
                try
                {
                    client = listener.AcceptTcpClient();
                    using (NetworkStream stream = client.GetStream())
                    {
                        ReadHeaders(stream);
                        byte[] header = Encoding.ASCII.GetBytes(
                            "HTTP/1.1 200 OK\r\n" +
                            "Content-Length: " + payloadSize + "\r\n" +
                            "Content-Type: application/octet-stream\r\n" +
                            "Connection: close\r\n\r\n");
                        stream.Write(header, 0, header.Length);
                        byte[] payload = new byte[65536];
                        int remaining = payloadSize;
                        while (remaining > 0)
                        {
                            int count = Math.Min(payload.Length, remaining);
                            stream.Write(payload, 0, count);
                            remaining -= count;
                        }
                        stream.Flush();
                    }
                }
                catch { }
            }

            private static void ReadHeaders(Stream stream)
            {
                int matched = 0;
                byte[] end = { 13, 10, 13, 10 };
                while (matched < end.Length)
                {
                    int value = stream.ReadByte();
                    if (value < 0)
                        return;
                    matched = value == end[matched] ? matched + 1 : value == end[0] ? 1 : 0;
                }
            }
        }

        private sealed class RangeHttpServer : IDisposable
        {
            private readonly TcpListener listener;
            private readonly Task worker;
            private readonly int payloadSize;
            private readonly bool rejectCompleteRange;
            private TcpClient client;

            public RangeHttpServer(int payloadSize, bool rejectCompleteRange)
            {
                this.payloadSize = payloadSize;
                this.rejectCompleteRange = rejectCompleteRange;
                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Url = "http://127.0.0.1:" + port + "/range.bin";
                worker = Task.Run((Action)Serve);
            }

            public string Url { get; private set; }
            public long RequestedStart { get; private set; } = -1;

            public void Dispose()
            {
                try { if (client != null) client.Close(); } catch { }
                try { listener.Stop(); } catch { }
                try { worker.Wait(2000); } catch { }
            }

            private void Serve()
            {
                try
                {
                    client = listener.AcceptTcpClient();
                    using (NetworkStream stream = client.GetStream())
                    {
                        string headers = ReadRequestHeaders(stream);
                        RequestedStart = ParseRangeStart(headers);
                        if (rejectCompleteRange && RequestedStart == payloadSize)
                        {
                            WriteAscii(
                                stream,
                                "HTTP/1.1 416 Range Not Satisfiable\r\n" +
                                "Content-Range: bytes */" + payloadSize + "\r\n" +
                                "Content-Length: 0\r\n" +
                                "Connection: close\r\n\r\n");
                            return;
                        }

                        int start = RequestedStart >= 0 ? (int)Math.Min(payloadSize, RequestedStart) : 0;
                        int remaining = payloadSize - start;
                        string status = start > 0 ? "206 Partial Content" : "200 OK";
                        string contentRange = start > 0
                            ? "Content-Range: bytes " + start + "-" + (payloadSize - 1) + "/" + payloadSize + "\r\n"
                            : "";
                        WriteAscii(
                            stream,
                            "HTTP/1.1 " + status + "\r\n" +
                            contentRange +
                            "Content-Length: " + remaining + "\r\n" +
                            "Content-Type: application/octet-stream\r\n" +
                            "Connection: close\r\n\r\n");
                        byte[] payload = new byte[65536];
                        while (remaining > 0)
                        {
                            int count = Math.Min(payload.Length, remaining);
                            stream.Write(payload, 0, count);
                            remaining -= count;
                        }
                        stream.Flush();
                    }
                }
                catch
                {
                }
            }

            private static string ReadRequestHeaders(Stream stream)
            {
                MemoryStream buffer = new MemoryStream();
                int matched = 0;
                byte[] end = { 13, 10, 13, 10 };
                while (matched < end.Length)
                {
                    int value = stream.ReadByte();
                    if (value < 0)
                        break;
                    buffer.WriteByte((byte)value);
                    matched = value == end[matched] ? matched + 1 : value == end[0] ? 1 : 0;
                }
                return Encoding.ASCII.GetString(buffer.ToArray());
            }

            private static long ParseRangeStart(string headers)
            {
                foreach (string line in (headers ?? "").Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!line.StartsWith("Range:", StringComparison.OrdinalIgnoreCase))
                        continue;
                    int equals = line.IndexOf('=');
                    int dash = line.IndexOf('-', equals + 1);
                    long value;
                    if (equals >= 0 && dash > equals &&
                        long.TryParse(line.Substring(equals + 1, dash - equals - 1).Trim(), out value))
                        return value;
                }
                return -1;
            }

            private static void WriteAscii(Stream stream, string value)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(value);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
        }
    }
}
