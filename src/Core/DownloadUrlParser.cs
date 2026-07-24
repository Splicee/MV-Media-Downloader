using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MVMediaStudio.Core
{
    internal static class DownloadUrlParser
    {
        private static readonly Regex UrlPattern = new Regex(
            @"https?://[^\s<>()\[\]{}""'`*]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static List<string> Parse(string text)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text))
                return result;

            foreach (Match match in UrlPattern.Matches(text))
            {
                string candidate = match.Value.TrimEnd('.', ',', ';', ':', ')', ']', '}');
                Uri uri;
                if (Uri.TryCreate(candidate, UriKind.Absolute, out uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                    seen.Add(candidate))
                    result.Add(candidate);
            }

            return result;
        }
    }
}
