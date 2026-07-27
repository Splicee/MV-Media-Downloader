using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal static class UpdateService
    {
        private static readonly HttpClient Client = CreateClient();
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
            string json = await DownloadTextAsync(endpoint).ConfigureAwait(false);

            UpdateReleaseInfo release = UpdateMetadata.ParseRelease(json);
            if (string.IsNullOrWhiteSpace(release.Sha256))
                release.Sha256 = UpdateMetadata.ParseChecksum(
                    await DownloadTextAsync(release.ChecksumUrl).ConfigureAwait(false));
            if (string.IsNullOrWhiteSpace(release.Sha256))
                throw new InvalidOperationException("Vydání nemá platný SHA-256 kontrolní součet.");
            return release;
        }

        public static async Task<string> DownloadAsync(UpdateReleaseInfo release, Action<double, string> progress)
        {
            AppPaths.EnsureDirectories();
            string finalPath = Path.Combine(AppPaths.UpdateDirectory, "MV-Media-Downloader-" + release.Version + ".zip");
            string temporaryPath = finalPath + ".download";
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            try
            {
                using (HttpResponseMessage response = await Client.GetAsync(
                    release.PackageUrl,
                    HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    long total = response.Content.Headers.ContentLength ?? -1;
                    using (Stream input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (FileStream output = new FileStream(
                        temporaryPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        65536,
                        FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        byte[] buffer = new byte[65536];
                        long received = 0;
                        int lastPercentage = -1;
                        int read;
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                            received += read;
                            if (total <= 0 || progress == null)
                                continue;
                            int percentage = (int)Math.Min(100, received * 100L / total);
                            if (percentage == lastPercentage)
                                continue;
                            lastPercentage = percentage;
                            progress(percentage, "Stahuji aktualizaci");
                        }
                    }
                }

                string actual = ComputeSha256(temporaryPath);
                if (!string.Equals(actual, release.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 aktualizace nesouhlasí. Soubor nebude použit.");
                if (File.Exists(finalPath))
                    File.Delete(finalPath);
                File.Move(temporaryPath, finalPath);
                return finalPath;
            }
            catch
            {
                try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
                throw;
            }
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

        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(AppInfo.UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private static async Task<string> DownloadTextAsync(string url)
        {
            using (HttpResponseMessage response = await Client.GetAsync(url).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        private static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
