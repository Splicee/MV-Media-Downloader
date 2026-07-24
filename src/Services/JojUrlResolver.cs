using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MVMediaStudio.Services
{
    internal sealed class DownloadUrlResolution
    {
        public readonly List<string> Urls = new List<string>();
        public readonly List<string> Notes = new List<string>();
    }

    internal static class JojUrlResolver
    {
        private static readonly Regex MediaUrlPattern = new Regex(
            @"(?:https?:)?//media\.joj\.sk/embed/[A-Za-z0-9_-]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static async Task<DownloadUrlResolution> ResolveAsync(IList<string> urls)
        {
            if (urls == null)
                throw new ArgumentNullException("urls");

            DownloadUrlResolution result = new DownloadUrlResolution();
            foreach (string url in urls)
            {
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                {
                    result.Urls.Add(url);
                    continue;
                }

                string host = uri.Host.ToLowerInvariant();
                if (host == "media.joj.sk")
                {
                    result.Urls.Add(url);
                    continue;
                }

                if (host == "play.joj.sk")
                {
                    result.Urls.Add(url);
                    result.Notes.Add("JOJ Play: použije se přihlášení z Chrome a pouze nešifrovaný H.264/HLS zdroj.");
                    continue;
                }

                if (host != "joj.sk" && host != "www.joj.sk")
                {
                    result.Urls.Add(url);
                    continue;
                }

                string html;
                try
                {
                    html = await DownloadPageAsync(url);
                }
                catch (Exception error)
                {
                    throw new InvalidOperationException("Stránku JOJ se nepodařilo načíst. Zkontroluj připojení a zkus otevřít konkrétní epizodu v prohlížeči.", error);
                }

                string mediaUrl = ExtractPublicMediaUrl(html);
                if (string.IsNullOrWhiteSpace(mediaUrl))
                    throw new InvalidOperationException("Na odkazu JOJ nebyla nalezena veřejně dostupná epizoda. V archivu nejprve otevři konkrétní epizodu. Obsah JOJ Play Premium nebo s DRM aplikace nestahuje.");

                result.Urls.Add(mediaUrl);
                result.Notes.Add("JOJ: " + url + " -> " + mediaUrl);
            }
            return result;
        }

        public static string ExtractPublicMediaUrl(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "";

            string decoded = WebUtility.HtmlDecode(html).Replace("\\/", "/");
            Match match = MediaUrlPattern.Match(decoded);
            if (!match.Success)
                return "";
            return match.Value.StartsWith("//", StringComparison.Ordinal) ? "https:" + match.Value : match.Value;
        }

        private static Task<string> DownloadPageAsync(string url)
        {
            return Task.Run(delegate
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                using (JojWebClient client = new JojWebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 MVMediaDownloader/3.1.0";
                    client.Headers[HttpRequestHeader.Accept] = "text/html,application/xhtml+xml";
                    return client.DownloadString(url);
                }
            });
        }

        private sealed class JojWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                request.Timeout = 15000;
                HttpWebRequest http = request as HttpWebRequest;
                if (http != null)
                {
                    http.ReadWriteTimeout = 15000;
                    http.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                }
                return request;
            }
        }
    }
}
