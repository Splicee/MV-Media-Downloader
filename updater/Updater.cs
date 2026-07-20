using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

[assembly: AssemblyTitle("MV Media Downloader Updater")]
[assembly: AssemblyCompany("MV")]
[assembly: AssemblyProduct("MV Media Downloader")]
[assembly: AssemblyVersion("3.0.3.0")]
[assembly: AssemblyFileVersion("3.0.3.0")]

namespace MVMediaDownloaderUpdater
{
    internal sealed class UpdateTransaction
    {
        public string TargetDirectory;
        public string BackupDirectory;
        public readonly List<string> BackedUpFiles = new List<string>();
        public readonly List<string> AddedFiles = new List<string>();
    }

    internal static class Updater
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Any(value => string.Equals(value, "--self-test", StringComparison.OrdinalIgnoreCase)))
                    return RunSelfTest();
                RunUpdate(ParseArguments(args));
                return 0;
            }
            catch (Exception error)
            {
                WriteLog("Aktualizace selhala: " + error);
                return 1;
            }
        }

        private static void RunUpdate(Dictionary<string, string> options)
        {
            string package = RequireFile(options, "--package");
            string target = RequireDirectory(options, "--target");
            string appName = RequireSimpleFileName(options, "--app");
            string version = Require(options, "--version");
            string health = Path.GetFullPath(Require(options, "--health"));
            int processId;
            if (!int.TryParse(Require(options, "--pid"), out processId))
                throw new ArgumentException("Neplatné PID aplikace.");

            WaitForExit(processId, TimeSpan.FromSeconds(60));
            string work = Path.Combine(Path.GetTempPath(), "MVMediaDownloader", "apply-" + Guid.NewGuid().ToString("N"));
            string staged = Path.Combine(work, "staged");
            string backup = Path.Combine(work, "backup");
            UpdateTransaction transaction = null;
            Process started = null;
            try
            {
                ExtractAndValidate(package, staged, appName);
                transaction = Apply(staged, target, backup);
                if (File.Exists(health))
                    File.Delete(health);
                started = StartApp(Path.Combine(target, appName), new[] { "--updated", version, "--update-health-file", health });
                if (!WaitForHealth(health, started, TimeSpan.FromSeconds(35)))
                    throw new InvalidOperationException("Nová verze nepotvrdila úspěšné spuštění.");
                WriteLog("Aktualizace na verzi " + version + " byla dokončena.");
                TryDelete(package);
            }
            catch (Exception error)
            {
                if (started != null)
                {
                    try { if (!started.HasExited) started.Kill(); } catch { }
                    try { started.WaitForExit(5000); } catch { }
                }
                if (transaction != null)
                    Rollback(transaction);
                WriteLog("Proběhl návrat k předchozí verzi: " + error.Message);
                StartApp(Path.Combine(target, appName), new[] { "--update-failed", error.Message });
                throw;
            }
            finally
            {
                TryDeleteDirectory(work);
                TryDelete(health);
            }
        }

        internal static void ExtractAndValidate(string package, string staged, string appName)
        {
            Directory.CreateDirectory(staged);
            string root = Path.GetFullPath(staged) + Path.DirectorySeparatorChar;
            using (ZipArchive archive = ZipFile.OpenRead(package))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destination = Path.GetFullPath(Path.Combine(staged, entry.FullName));
                    if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Balíček obsahuje nebezpečnou cestu.");
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    entry.ExtractToFile(destination, true);
                }
            }
            if (!File.Exists(Path.Combine(staged, appName)))
                throw new InvalidDataException("Balíček neobsahuje " + appName + ".");
        }

        internal static UpdateTransaction Apply(string staged, string target, string backup)
        {
            UpdateTransaction transaction = new UpdateTransaction
            {
                TargetDirectory = Path.GetFullPath(target),
                BackupDirectory = Path.GetFullPath(backup)
            };
            Directory.CreateDirectory(transaction.TargetDirectory);
            Directory.CreateDirectory(transaction.BackupDirectory);

            try
            {
                foreach (string source in Directory.GetFiles(staged, "*", SearchOption.AllDirectories))
                {
                    string relative = RelativePath(staged, source);
                    string destination = Path.Combine(transaction.TargetDirectory, relative);
                    if (File.Exists(destination))
                    {
                        string backupFile = Path.Combine(transaction.BackupDirectory, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupFile));
                        File.Copy(destination, backupFile, true);
                        transaction.BackedUpFiles.Add(relative);
                    }
                    else
                    {
                        transaction.AddedFiles.Add(relative);
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(source, destination, true);
                }
                return transaction;
            }
            catch
            {
                Rollback(transaction);
                throw;
            }
        }

        internal static void Rollback(UpdateTransaction transaction)
        {
            foreach (string relative in transaction.AddedFiles.OrderByDescending(value => value.Length))
                TryDelete(Path.Combine(transaction.TargetDirectory, relative));
            foreach (string relative in transaction.BackedUpFiles)
            {
                string source = Path.Combine(transaction.BackupDirectory, relative);
                string destination = Path.Combine(transaction.TargetDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination, true);
            }
        }

        private static bool WaitForHealth(string path, Process process, TimeSpan timeout)
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.Elapsed < timeout)
            {
                if (File.Exists(path))
                    return true;
                try { if (process.HasExited) return false; } catch { return false; }
                Thread.Sleep(250);
            }
            return File.Exists(path);
        }

        private static void WaitForExit(int processId, TimeSpan timeout)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                        throw new TimeoutException("Aplikace se před aktualizací neukončila.");
                }
            }
            catch (ArgumentException)
            {
            }
        }

        private static Process StartApp(string path, IEnumerable<string> arguments)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Aplikaci nelze po aktualizaci spustit.", path);
            return Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = JoinArguments(arguments),
                WorkingDirectory = Path.GetDirectoryName(path),
                UseShellExecute = true
            });
        }

        private static Dictionary<string, string> ParseArguments(string[] args)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException("Neplatné argumenty aktualizátoru.");
                result[args[index]] = args[index + 1];
            }
            return result;
        }

        private static string Require(Dictionary<string, string> options, string name)
        {
            string value;
            if (!options.TryGetValue(name, out value) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Chybí argument " + name + ".");
            return value;
        }

        private static string RequireFile(Dictionary<string, string> options, string name)
        {
            string path = Path.GetFullPath(Require(options, name));
            if (!File.Exists(path))
                throw new FileNotFoundException("Soubor aktualizace nebyl nalezen.", path);
            return path;
        }

        private static string RequireDirectory(Dictionary<string, string> options, string name)
        {
            string path = Path.GetFullPath(Require(options, name));
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("Cílová složka nebyla nalezena: " + path);
            return path;
        }

        private static string RequireSimpleFileName(Dictionary<string, string> options, string name)
        {
            string value = Require(options, name);
            if (!string.Equals(value, Path.GetFileName(value), StringComparison.Ordinal) || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Neplatný název aplikace.");
            return value;
        }

        private static string RelativePath(string root, string path)
        {
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(path);
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Soubor neleží v aktualizační složce.");
            return full.Substring(prefix.Length);
        }

        private static string JoinArguments(IEnumerable<string> values)
        {
            return string.Join(" ", values.Select(Quote).ToArray());
        }

        private static string Quote(string value)
        {
            if (value == null)
                return "\"\"";
            if (value.Length > 0 && value.IndexOfAny(new[] { ' ', '\t', '\"' }) < 0)
                return value;

            StringBuilder result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char character in value)
            {
                if (character == '\\')
                {
                    slashes++;
                    continue;
                }
                if (character == '\"')
                    result.Append('\\', slashes * 2 + 1);
                else
                    result.Append('\\', slashes);
                result.Append(character);
                slashes = 0;
            }
            result.Append('\\', slashes * 2);
            result.Append('\"');
            return result.ToString();
        }

        private static void WriteLog(string message)
        {
            try
            {
                string directory = Path.Combine(Path.GetTempPath(), "MVMediaDownloader");
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, "updater.log"), DateTime.Now.ToString("s") + " " + message + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void TryDeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
        }

        private static int RunSelfTest()
        {
            string root = Path.Combine(Path.GetTempPath(), "mv-media-updater-test-" + Guid.NewGuid().ToString("N"));
            string target = Path.Combine(root, "target");
            string source = Path.Combine(root, "source");
            string staged = Path.Combine(root, "staged");
            string backup = Path.Combine(root, "backup");
            string package = Path.Combine(root, "update.zip");
            try
            {
                Directory.CreateDirectory(target);
                Directory.CreateDirectory(source);
                File.WriteAllText(Path.Combine(target, "MV Media Downloader.exe"), "old", Encoding.UTF8);
                File.WriteAllText(Path.Combine(source, "MV Media Downloader.exe"), "new", Encoding.UTF8);
                Directory.CreateDirectory(Path.Combine(source, "support"));
                File.WriteAllText(Path.Combine(source, "support", "added.txt"), "added", Encoding.UTF8);
                ZipFile.CreateFromDirectory(source, package);

                ExtractAndValidate(package, staged, "MV Media Downloader.exe");
                UpdateTransaction transaction = Apply(staged, target, backup);
                Assert(File.ReadAllText(Path.Combine(target, "MV Media Downloader.exe"), Encoding.UTF8) == "new", "nová verze se použila");
                Assert(File.Exists(Path.Combine(target, "support", "added.txt")), "nový soubor se přidal");
                Rollback(transaction);
                Assert(File.ReadAllText(Path.Combine(target, "MV Media Downloader.exe"), Encoding.UTF8) == "old", "původní verze se obnovila");
                Assert(!File.Exists(Path.Combine(target, "support", "added.txt")), "nový soubor se při návratu odstranil");

                string invalidSource = Path.Combine(root, "invalid-source");
                string invalidPackage = Path.Combine(root, "invalid.zip");
                Directory.CreateDirectory(invalidSource);
                File.WriteAllText(Path.Combine(invalidSource, "readme.txt"), "invalid");
                ZipFile.CreateFromDirectory(invalidSource, invalidPackage);
                bool rejected = false;
                try { ExtractAndValidate(invalidPackage, Path.Combine(root, "invalid-staged"), "MV Media Downloader.exe"); }
                catch (InvalidDataException) { rejected = true; }
                Assert(rejected, "neúplný balíček se odmítl");
                Console.WriteLine("Všechny testy aktualizátoru prošly.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                return 1;
            }
            finally
            {
                TryDeleteDirectory(root);
            }
        }

        private static void Assert(bool value, string name)
        {
            if (!value)
                throw new InvalidOperationException("Test selhal: " + name);
            Console.WriteLine("OK: " + name);
        }
    }
}
