using System;
using System.Linq;
using System.Web.Script.Serialization;

namespace MVMediaStudio.Core
{
    internal sealed class UpdateReleaseInfo
    {
        public Version Version;
        public string TagName;
        public string PackageUrl;
        public string ChecksumUrl;
        public string Sha256;
        public string ReleaseUrl;
    }

    internal static class UpdateMetadata
    {
        public const string PackageAssetName = "MV-Media-Downloader-win-x64.zip";
        public const string ChecksumAssetName = "MV-Media-Downloader-win-x64.zip.sha256";

        public static UpdateReleaseInfo ParseRelease(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("GitHub nevrátil informace o vydání.");

            GitHubRelease release = new JavaScriptSerializer().Deserialize<GitHubRelease>(json);
            if (release == null || release.draft || release.prerelease)
                throw new InvalidOperationException("Poslední vydání není stabilní.");

            Version version = ParseVersion(release.tag_name);
            GitHubAsset package = FindAsset(release.assets, PackageAssetName);
            GitHubAsset checksum = FindAsset(release.assets, ChecksumAssetName);
            if (package == null || checksum == null)
                throw new InvalidOperationException("Vydání neobsahuje aktualizační ZIP a jeho kontrolní součet.");

            string digest = package.digest ?? "";
            if (digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                digest = digest.Substring(7);
            else
                digest = "";

            return new UpdateReleaseInfo
            {
                Version = version,
                TagName = release.tag_name ?? version.ToString(),
                PackageUrl = RequireHttps(package.browser_download_url),
                ChecksumUrl = RequireHttps(checksum.browser_download_url),
                Sha256 = NormalizeHash(digest),
                ReleaseUrl = release.html_url ?? ""
            };
        }

        public static Version ParseVersion(string value)
        {
            string text = (value ?? "").Trim().TrimStart('v', 'V');
            int suffix = text.IndexOfAny(new[] { '-', '+' });
            if (suffix >= 0)
                text = text.Substring(0, suffix);
            Version result;
            if (!Version.TryParse(text, out result))
                throw new InvalidOperationException("Vydání má neplatné číslo verze.");
            return result;
        }

        public static string ParseChecksum(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            string first = text.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return NormalizeHash(first);
        }

        public static string NormalizeHash(string value)
        {
            string hash = (value ?? "").Trim().ToLowerInvariant();
            if (hash.Length != 64 || hash.Any(character => !Uri.IsHexDigit(character)))
                return "";
            return hash;
        }

        private static GitHubAsset FindAsset(GitHubAsset[] assets, string name)
        {
            return (assets ?? new GitHubAsset[0]).FirstOrDefault(asset =>
                asset != null && string.Equals(asset.name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static string RequireHttps(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Aktualizační soubor nemá bezpečnou HTTPS adresu.");
            return uri.AbsoluteUri;
        }

        private sealed class GitHubRelease
        {
            public string tag_name { get; set; }
            public string html_url { get; set; }
            public bool draft { get; set; }
            public bool prerelease { get; set; }
            public GitHubAsset[] assets { get; set; }
        }

        private sealed class GitHubAsset
        {
            public string name { get; set; }
            public string browser_download_url { get; set; }
            public string digest { get; set; }
        }
    }
}
