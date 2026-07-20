using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal static class UpdateService
    {
        public const string RepositoryOwner = "Splicee";
        public const string RepositoryName = "MV-Media-Downloader";

        public static bool IsConfigured
        {
            get { return RepositoryOwner.IndexOf("__", StringComparison.Ordinal) < 0; }
        }

        public static async Task<UpdateReleaseInfo> CheckLatestAsync()
        {
            if (!IsConfigured)
                throw new InvalidOperationException("Aktualizační kanál zatím není propojený s GitHub účtem.");

            string endpoint = "https://api.github.com/repos/" + RepositoryOwner + "/" + RepositoryName + "/releases/latest";
            string json = await Task.Run(delegate
            {
                using (WebClient client = CreateClient())
                    return client.DownloadString(endpoint);
            });

            UpdateReleaseInfo release = UpdateMetadata.ParseRelease(json);
            if (string.IsNullOrWhiteSpace(release.Sha256))
            {
                string checksum = await Task.Run(delegate
                {
                    using (WebClient client = CreateClient())
                        return client.DownloadString(release.ChecksumUrl);
                });
                release.Sha256 = UpdateMetadata.ParseChecksum(checksum);
            }
            if (string.IsNullOrWhiteSpace(release.Sha256))
                throw new InvalidOperationException("Vydání nemá platný SHA-256 kontrolní součet.");
            return release;
        }

        public static Task<string> DownloadAsync(UpdateReleaseInfo release, Action<double, string> progress)
        {
            AppPaths.EnsureDirectories();
            string finalPath = Path.Combine(AppPaths.UpdateDirectory, "MV-Media-Downloader-" + release.Version + ".zip");
            string temporaryPath = finalPath + ".download";
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
            WebClient client = CreateClient();
            client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs eventArgs)
            {
                if (progress != null)
                    progress(eventArgs.ProgressPercentage, "Stahuji aktualizaci");
            };
            client.DownloadFileCompleted += delegate(object sender, System.ComponentModel.AsyncCompletedEventArgs eventArgs)
            {
                try
                {
                    if (eventArgs.Cancelled)
                        throw new OperationCanceledException("Stahování aktualizace bylo zrušeno.");
                    if (eventArgs.Error != null)
                        throw eventArgs.Error;
                    string actual = ComputeSha256(temporaryPath);
                    if (!string.Equals(actual, release.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("SHA-256 aktualizace nesouhlasí. Soubor nebude použit.");
                    if (File.Exists(finalPath))
                        File.Delete(finalPath);
                    File.Move(temporaryPath, finalPath);
                    completion.TrySetResult(finalPath);
                }
                catch (Exception error)
                {
                    try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
                    completion.TrySetException(error);
                }
                finally
                {
                    client.Dispose();
                }
            };
            client.DownloadFileAsync(new Uri(release.PackageUrl), temporaryPath);
            return completion.Task;
        }

        public static void LaunchUpdater(UpdateReleaseInfo release, string packagePath)
        {
            string updaterName = "MV Media Downloader Updater.exe";
            string updaterPath = Path.Combine(AppPaths.ExecutableDirectory, updaterName);
            if (!File.Exists(updaterPath))
                throw new FileNotFoundException("V instalační složce chybí aktualizátor.", updaterPath);

            string tempRoot = Path.Combine(Path.GetTempPath(), "MVMediaDownloader");
            Directory.CreateDirectory(tempRoot);
            string temporaryUpdater = Path.Combine(tempRoot, "updater-" + Guid.NewGuid().ToString("N") + ".exe");
            string healthFile = Path.Combine(tempRoot, "mv-media-health-" + Guid.NewGuid().ToString("N") + ".ok");
            File.Copy(updaterPath, temporaryUpdater, true);

            string arguments = ArgumentUtilities.Join(new[]
            {
                "--package", packagePath,
                "--target", AppPaths.ExecutableDirectory,
                "--app", "MV Media Downloader.exe",
                "--pid", Process.GetCurrentProcess().Id.ToString(),
                "--version", release.Version.ToString(3),
                "--health", healthFile
            });
            Process.Start(new ProcessStartInfo
            {
                FileName = temporaryUpdater,
                Arguments = arguments,
                WorkingDirectory = tempRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            Application.Current.Shutdown();
        }

        public static string ArgumentValue(string[] arguments, string name)
        {
            if (arguments == null)
                return "";
            for (int index = 0; index + 1 < arguments.Length; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                    return arguments[index + 1] ?? "";
            }
            return "";
        }

        public static void SignalHealthy(string[] arguments)
        {
            string path = ArgumentValue(arguments, "--update-health-file");
            if (string.IsNullOrWhiteSpace(path))
                return;
            try
            {
                string fullPath = Path.GetFullPath(path);
                string tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "MVMediaDownloader")) + Path.DirectorySeparatorChar;
                if (!fullPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) ||
                    !Path.GetFileName(fullPath).StartsWith("mv-media-health-", StringComparison.OrdinalIgnoreCase))
                    return;
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(fullPath, "ok", Encoding.ASCII);
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
            }
        }

        private static WebClient CreateClient()
        {
            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.UserAgent] = "MV-Media-Downloader/3.0.3";
            client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
            return client;
        }

        private static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
