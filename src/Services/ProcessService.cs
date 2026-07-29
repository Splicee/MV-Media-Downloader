using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
                    process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs eventArgs)
                    {
                        if (!string.IsNullOrWhiteSpace(eventArgs.Data) && onLine != null)
                            onLine(eventArgs.Data, false);
                    };
                    process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs eventArgs)
                    {
                        if (!string.IsNullOrWhiteSpace(eventArgs.Data) && onLine != null)
                            onLine(eventArgs.Data, true);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    while (!process.WaitForExit(100))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            TerminateProcessTree(process);
                            try { process.WaitForExit(3000); } catch { }
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
                process.Start();
                Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
                Task<string> standardError = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    TerminateProcessTree(process);
                    try { process.WaitForExit(3000); } catch { }
                    return "";
                }
                process.WaitForExit();
                try
                {
                    Task.WaitAll(new Task[] { standardOutput, standardError }, 3000);
                }
                catch
                {
                    return "";
                }
                return (standardOutput.Result + standardError.Result).Trim();
            }
        }

        private static void TerminateProcessTree(Process process)
        {
            int processId;
            try
            {
                if (process == null || process.HasExited)
                    return;
                processId = process.Id;
            }
            catch
            {
                return;
            }

            List<int> descendants = GetDescendantProcessIds(processId);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    using (Process terminator = new Process())
                    {
                        terminator.StartInfo = new ProcessStartInfo
                        {
                            FileName = "taskkill.exe",
                            Arguments = "/PID " + processId + " /T /F",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        terminator.Start();
                        terminator.WaitForExit(3000);
                    }
                }
                catch { }
            }

            for (int index = descendants.Count - 1; index >= 0; index--)
                KillProcess(descendants[index]);

            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch { }
        }

        private static void KillProcess(int processId)
        {
            try
            {
                using (Process descendant = Process.GetProcessById(processId))
                {
                    if (descendant.HasExited)
                        return;
                    descendant.Kill();
                    descendant.WaitForExit(1000);
                }
            }
            catch { }
        }

        private static List<int> GetDescendantProcessIds(int rootProcessId)
        {
            List<int> result = new List<int>();
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return result;

            Dictionary<int, List<int>> children = new Dictionary<int, List<int>>();
            IntPtr snapshot = CreateToolhelp32Snapshot(0x00000002, 0);
            if (snapshot == new IntPtr(-1))
                return result;

            try
            {
                ProcessEntry entry = new ProcessEntry();
                entry.Size = (uint)Marshal.SizeOf(typeof(ProcessEntry));
                if (!Process32First(snapshot, ref entry))
                    return result;

                do
                {
                    int parentId = unchecked((int)entry.ParentProcessId);
                    int childId = unchecked((int)entry.ProcessId);
                    List<int> childIds;
                    if (!children.TryGetValue(parentId, out childIds))
                    {
                        childIds = new List<int>();
                        children[parentId] = childIds;
                    }
                    childIds.Add(childId);
                    entry.Size = (uint)Marshal.SizeOf(typeof(ProcessEntry));
                }
                while (Process32Next(snapshot, ref entry));
            }
            finally
            {
                CloseHandle(snapshot);
            }

            Queue<int> pending = new Queue<int>();
            pending.Enqueue(rootProcessId);
            while (pending.Count > 0)
            {
                int parentId = pending.Dequeue();
                List<int> childIds;
                if (!children.TryGetValue(parentId, out childIds))
                    continue;
                foreach (int childId in childIds)
                {
                    if (result.Contains(childId))
                        continue;
                    result.Add(childId);
                    pending.Enqueue(childId);
                }
            }
            return result;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ProcessEntry
        {
            public uint Size;
            public uint Usage;
            public uint ProcessId;
            public IntPtr DefaultHeapId;
            public uint ModuleId;
            public uint Threads;
            public uint ParentProcessId;
            public int BasePriority;
            public uint Flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string ExecutableFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry entry);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry entry);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private static ProcessStartInfo CreateStartInfo(string executable, IList<string> arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
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
            startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";
            startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            return startInfo;
        }
    }
}
