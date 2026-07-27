using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MVMediaStudio.Core
{
    internal static class DownloadOutputParser
    {
        public const string CurrentItemPrefix = "MV_ITEM:";
        public const string CompletedPathPrefix = "MV_DONE:";

        public static bool TryReadCurrentItem(string line, out string itemName)
        {
            itemName = "";
            if (string.IsNullOrWhiteSpace(line))
                return false;

            if (line.StartsWith(CurrentItemPrefix, StringComparison.Ordinal))
            {
                itemName = Normalize(line.Substring(CurrentItemPrefix.Length));
                return itemName.Length > 0;
            }

            Match destination = Regex.Match(
                line,
                "^\\[(?:download|ExtractAudio)\\]\\s+Destination:\\s+(.+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (destination.Success)
            {
                itemName = DisplayNameFromPath(destination.Groups[1].Value);
                return itemName.Length > 0;
            }

            Match merge = Regex.Match(
                line,
                "^\\[Merger\\].*?\"([^\"]+)\"\\s*$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (merge.Success)
            {
                itemName = DisplayNameFromPath(merge.Groups[1].Value);
                return itemName.Length > 0;
            }
            return false;
        }

        public static string DisplayNameFromPath(string value)
        {
            string path = Normalize(value).Trim('"');
            if (path.Length == 0)
                return "";
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                return Normalize(string.IsNullOrWhiteSpace(fileName) ? path : fileName);
            }
            catch
            {
                return path;
            }
        }

        private static string Normalize(string value)
        {
            string text = (value ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
            text = Regex.Replace(text, "\\s{2,}", " ");
            return text.Length > 240 ? text.Substring(0, 237) + "..." : text;
        }
    }
}
