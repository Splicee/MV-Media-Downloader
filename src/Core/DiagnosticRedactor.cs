using System;
using System.Text.RegularExpressions;

namespace MVMediaStudio.Core
{
    internal static class DiagnosticRedactor
    {
        public static string Redact(string value)
        {
            string text = value ?? "";
            text = ReplacePath(text, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "%USERPROFILE%");
            text = ReplacePath(text, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "%LOCALAPPDATA%");
            text = ReplacePath(text, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "%APPDATA%");
            text = Regex.Replace(text, @"AIza[0-9A-Za-z_-]{20,}", "<odstraneny-klic>");
            text = Regex.Replace(text, @"\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}(?:\.[A-Za-z0-9_-]{5,})?", "<odstraneny-token>");
            text = Regex.Replace(text, @"(?im)(authorization\s*:\s*bearer\s+)(\S+)", "$1<odstraneno>");
            text = Regex.Replace(text, @"(?im)((?:authorization|cookie|set-cookie|tivio-custom-token|refresh_token|access_token|api[_-]?key)\s*[:=]\s*)([^\s;]+)", "$1<odstraneno>");
            text = Regex.Replace(text, @"([?&][A-Za-z0-9_.~-]+)=([^&\s]+)", "$1=<odstraneno>");
            return text;
        }

        private static string ReplacePath(string text, string path, string replacement)
        {
            if (string.IsNullOrWhiteSpace(path))
                return text;
            return Regex.Replace(text, Regex.Escape(path), replacement, RegexOptions.IgnoreCase);
        }
    }
}
