using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal sealed class DownloadUrlResolution
    {
        public readonly List<string> Urls = new List<string>();
        public readonly List<string> Notes = new List<string>();
    }

    internal static class JojUrlResolver
    {
        private static readonly HttpClient Client = CreateClient();
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

        private static async Task<string> DownloadPageAsync(string url)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.UserAgent.ParseAdd(AppInfo.BrowserUserAgent);
                request.Headers.Accept.ParseAdd("text/html");
                request.Headers.Accept.ParseAdd("application/xhtml+xml");
                using (HttpResponseMessage response = await Client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }

        private static HttpClient CreateClient()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }
    }
}
