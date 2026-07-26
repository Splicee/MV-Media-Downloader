using System;
using System.Collections.Generic;
using System.IO;

namespace MVMediaStudio.Core
{
    internal static class MediaFileSupport
    {
        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ".mp4",
            ".mkv",
            ".avi",
            ".mov",
            ".webm",
            ".m4v",
            ".ts",
            ".mts",
            ".m2ts",
            ".wmv",
            ".flv",
            ".mpeg",
            ".mpg",
            ".vob",
            ".ogv"
        };

        public const string VideoDialogFilter =
            "Video soubory|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.m4v;*.ts;*.mts;*.m2ts;*.wmv;*.flv;*.mpeg;*.mpg;*.vob;*.ogv|Všechny soubory|*.*";

        public static bool IsSupportedVideo(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                VideoExtensions.Contains(Path.GetExtension(path));
        }
    }
}
