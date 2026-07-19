using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal static class ProcessService
    {
        public static Task<int> RunAsync(
            string executable,
            IList<string> arguments,
            Action<string, bool> onLine,
            CancellationToken cancellationToken)
        {
            return Task.Run(delegate
            {
                using (Process process = new Process())
                {
                    process.StartInfo = CreateStartInfo(executable, arguments);
                    process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs eventArgs)
                    {
                        if (!string.IsNullOrWhiteSpace(eventArgs.Data) && onLine != null)
                            onLine(eventArgs.Data, false);
                    };
                    process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs eventArgs)
                    {
                        if (!string.IsNullOrWhiteSpace(eventArgs.Data) && onLine != null)
                            onLine(eventArgs.Data, true);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    while (!process.WaitForExit(150))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            try { process.Kill(); } catch { }
                            process.WaitForExit();
                            return -2;
                        }
                    }
                    process.WaitForExit();
                    return process.ExitCode;
                }
            });
        }

        public static string Capture(string executable, IList<string> arguments, int timeoutMilliseconds)
        {
            using (Process process = new Process())
            {
                process.StartInfo = CreateStartInfo(executable, arguments);
                StringBuilder output = new StringBuilder();
                process.Start();
                output.Append(process.StandardOutput.ReadToEnd());
                output.Append(process.StandardError.ReadToEnd());
                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    try { process.Kill(); } catch { }
                    return "";
                }
                return output.ToString().Trim();
            }
        }

        private static ProcessStartInfo CreateStartInfo(string executable, IList<string> arguments)
        {
            return new ProcessStartInfo
            {
                FileName = executable,
                Arguments = ArgumentUtilities.Join(arguments),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = AppPaths.ExecutableDirectory
            };
        }
    }
}
