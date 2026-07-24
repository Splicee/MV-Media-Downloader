using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MVMediaStudio.Core
{
    internal enum DownloadProviderKind
    {
        YtDlp,
        Webshare,
        Direct,
        Unsupported
    }

    internal sealed class DownloadRoute
    {
        public string Url;
        public DownloadProviderKind Kind;
        public string Provider;
        public string Message;
    }

    internal sealed class DirectDownloadItem
    {
        public string Provider;
        public string SourceUrl;
        public string DownloadUrl;
        public string FileName;
        public long ExpectedSize;
    }

    internal sealed class DirectDownloadProgress
    {
        public string Provider;
        public string FileName;
        public string OutputPath;
        public long BytesReceived;
        public long TotalBytes;
        public double BytesPerSecond;
        public bool Completed;
        public bool Skipped;
        public bool Resumed;

        public double Percentage
        {
            get
            {
                return TotalBytes > 0 ? Math.Max(0, Math.Min(100, BytesReceived * 100d / TotalBytes)) : 0;
            }
        }
    }

    internal static class DownloadSourceRouter
    {
        private static readonly HashSet<string> DirectExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".webm", ".avi", ".mov", ".m4v",
            ".mp3", ".m4a", ".aac", ".opus", ".ogg", ".flac", ".wav",
            ".zip", ".7z", ".rar", ".pdf"
        };

        public static DownloadRoute Classify(string url)
        {
            DownloadRoute route = new DownloadRoute { Url = url, Kind = DownloadProviderKind.YtDlp, Provider = "yt-dlp" };
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                route.Kind = DownloadProviderKind.Unsupported;
                route.Provider = "Neznámý zdroj";
                route.Message = "Odkaz není platná HTTP adresa.";
                return route;
            }

            string host = uri.Host.TrimStart('.').ToLowerInvariant();
            if (host == "webshare.cz" || host.EndsWith(".webshare.cz", StringComparison.Ordinal))
            {
                route.Kind = DownloadProviderKind.Webshare;
                route.Provider = "Webshare";
                return route;
            }

            if (host == "prehraj.to" || host.EndsWith(".prehraj.to", StringComparison.Ordinal))
            {
                route.Kind = DownloadProviderKind.Unsupported;
                route.Provider = "Přehraj.to";
                route.Message = "Přehraj.to neposkytuje povolené veřejné API. Použij oficiální odkaz ke stažení nebo přímý CDN odkaz z vlastního účtu.";
                return route;
            }

            string extension = Path.GetExtension(uri.AbsolutePath);
            if (DirectExtensions.Contains(extension) ||
                host.EndsWith(".premiumcdn.net", StringComparison.Ordinal) ||
                host == "premiumcdn.net")
            {
                route.Kind = DownloadProviderKind.Direct;
                route.Provider = "Přímé stažení";
            }
            return route;
        }

        public static string ExtractWebshareIdent(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                return "";
            string value = uri.AbsolutePath + "/" + (uri.Fragment ?? "").TrimStart('#');
            Match match = Regex.Match(value, "(?:^|/)file/([A-Za-z0-9_-]+)(?:/|$)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "";
        }

        public static string FileNameFromUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                return "download.bin";
            string name = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));
            return string.IsNullOrWhiteSpace(name) ? "download.bin" : name;
        }
    }
}
