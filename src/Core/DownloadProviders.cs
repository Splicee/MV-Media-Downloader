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

            if (host == "oneplay.cz" || host.EndsWith(".oneplay.cz", StringComparison.Ordinal))
            {
                route.Kind = DownloadProviderKind.Unsupported;
                route.Provider = "Oneplay";
                route.Message = "Předplacený obsah Oneplay je chráněný a aplikace jej nestahuje.";
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
            else
            {
                route.Provider = ProviderForHost(host);
                route.Message = GuidanceForHost(host);
            }
            return route;
        }

        public static string ProviderForHost(string host)
        {
            string value = (host ?? "").Trim().TrimStart('.').ToLowerInvariant();
            if (Matches(value, "youtube.com") || Matches(value, "youtu.be") || Matches(value, "youtube-nocookie.com"))
                return "YouTube";
            if (Matches(value, "ceskatelevize.cz"))
                return "Česká televize";
            if (Matches(value, "iprima.cz"))
                return "Prima";
            if (Matches(value, "nova.cz"))
                return "TV Nova";
            if (Matches(value, "joj.sk"))
                return "JOJ";
            if (Matches(value, "stream.cz"))
                return "Stream.cz";
            if (Matches(value, "televizeseznam.cz"))
                return "Televize Seznam";
            if (Matches(value, "seznamzpravy.cz"))
                return "Seznam Zprávy";
            if (Matches(value, "mujrozhlas.cz"))
                return "MůjRozhlas";
            if (Matches(value, "rozhlas.cz"))
                return "Český rozhlas";
            if (Matches(value, "tvnoe.cz"))
                return "TV Noe";
            if (Matches(value, "aktualne.cz") || Matches(value, "dvtv.cz"))
                return "DVTV / Aktuálně";
            if (Matches(value, "playtvak.cz") || Matches(value, "idnes.cz") ||
                Matches(value, "lidovky.cz") || Matches(value, "metro.cz"))
                return "iDNES / Playtvak";
            if (Matches(value, "vimeo.com"))
                return "Vimeo";
            if (Matches(value, "dailymotion.com") || Matches(value, "dai.ly"))
                return "Dailymotion";
            if (Matches(value, "twitch.tv"))
                return "Twitch";
            if (Matches(value, "kick.com"))
                return "Kick";
            if (Matches(value, "tiktok.com"))
                return "TikTok";
            if (Matches(value, "instagram.com"))
                return "Instagram";
            if (Matches(value, "facebook.com") || Matches(value, "fb.watch"))
                return "Facebook";
            if (Matches(value, "x.com") || Matches(value, "twitter.com"))
                return "X";
            if (Matches(value, "soundcloud.com"))
                return "SoundCloud";
            if (Matches(value, "bandcamp.com"))
                return "Bandcamp";
            if (Matches(value, "mixcloud.com"))
                return "Mixcloud";
            if (Matches(value, "podcasts.apple.com"))
                return "Apple Podcasts";
            if (Matches(value, "reddit.com") || Matches(value, "redd.it"))
                return "Reddit";
            if (Matches(value, "rumble.com"))
                return "Rumble";
            if (Matches(value, "streamable.com"))
                return "Streamable";
            return "Další web";
        }

        public static string GuidanceForHost(string host)
        {
            string value = (host ?? "").Trim().TrimStart('.').ToLowerInvariant();
            if (Matches(value, "ceskatelevize.cz"))
                return "ČT občas mění přehrávač. Při chybě HTTP 410 nejprve aktualizuj yt-dlp; DRM obsah stáhnout nelze.";
            if (Matches(value, "cnn.iprima.cz"))
                return "CNN Prima mění přehrávač. Při chybě extraktoru nejprve aktualizuj yt-dlp.";
            if (Matches(value, "iprima.cz"))
                return "Prima+ vyžaduje účet přímo pro yt-dlp; samotné cookies z prohlížeče nemusí stačit.";
            if (Matches(value, "nova.cz"))
                return "Veřejná videa TV Nova fungují; placený nebo DRM obsah stáhnout nelze.";
            if (Matches(value, "joj.sk"))
                return "U části obsahu JOJ Play je nutné přihlášení přes tlačítko Přihlásit JOJ Play.";
            if (Matches(value, "seznamzpravy.cz"))
                return "Při přesměrování na souhlas zapni Přihlášení z prohlížeče.";
            if (Matches(value, "playtvak.cz") || Matches(value, "idnes.cz") ||
                Matches(value, "lidovky.cz") || Matches(value, "metro.cz"))
                return "Extraktor iDNES / Playtvak je v aktuálním yt-dlp označený jako dočasně nefunkční.";
            return "";
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

        private static bool Matches(string host, string domain)
        {
            return string.Equals(host, domain, StringComparison.Ordinal) ||
                host.EndsWith("." + domain, StringComparison.Ordinal);
        }
    }
}
