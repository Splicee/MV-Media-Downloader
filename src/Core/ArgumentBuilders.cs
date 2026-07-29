using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MVMediaStudio.Core
{
    internal static class ArgumentUtilities
    {
        public static string Join(IEnumerable<string> arguments)
        {
            StringBuilder text = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (text.Length > 0)
                    text.Append(' ');
                text.Append(Quote(argument));
            }
            return text.ToString();
        }

        public static string Quote(string value)
        {
            if (value == null)
                return "\"\"";
            if (value.Length > 0 && value.IndexOfAny(new[] { ' ', '\t', '\"' }) < 0)
                return value;

            StringBuilder result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char character in value)
            {
                if (character == '\\')
                {
                    slashes++;
                    continue;
                }

                if (character == '\"')
                    result.Append('\\', slashes * 2 + 1);
                else
                    result.Append('\\', slashes);
                result.Append(character);
                slashes = 0;
            }
            result.Append('\\', slashes * 2);
            result.Append('\"');
            return result.ToString();
        }
    }

    internal static class DownloadArgumentBuilder
    {
        public static List<string> Build(DownloadOptions options, IList<string> urls, ToolState tools)
        {
            if (options == null)
                throw new ArgumentNullException("options");
            if (urls == null || urls.Count == 0)
                throw new ArgumentException("Není vložen žádný odkaz.");

            List<string> args = new List<string>();
            args.Add("--encoding");
            args.Add("utf-8");
            args.Add("--newline");
            args.Add("--progress");
            args.Add("--progress-delta");
            args.Add("0.5");
            args.Add("--continue");
            args.Add("--part");
            args.Add("--console-title");
            args.Add("--windows-filenames");
            args.Add("--trim-filenames");
            args.Add("180");
            args.Add("-P");
            args.Add(options.OutputDirectory);
            args.Add("-o");
            args.Add("%(title)s [%(id)s].%(ext)s");
            args.Add("--print");
            args.Add("before_dl:" + DownloadOutputParser.CurrentItemPrefix + "%(title)s");
            args.Add("--print");
            args.Add("after_move:MV_DONE:%(filepath)s");

            if (NeedsBrowserImpersonation(urls))
            {
                args.Add("--impersonate");
                args.Add("chrome");
            }

            AddFormat(args, options.Preset, options.Quality, tools != null && tools.HasFfmpeg);

            string rateLimit = NormalizeRateLimit(options.RateLimit);
            if (rateLimit.Length > 0)
            {
                args.Add("--limit-rate");
                args.Add(rateLimit);
            }

            if (!options.Playlist)
                args.Add("--no-playlist");
            if (options.NoOverwrite)
                args.Add("--no-overwrites");
            if (options.CookiesFromBrowser)
            {
                args.Add("--cookies-from-browser");
                args.Add(string.IsNullOrWhiteSpace(options.CookieBrowserSpec) ? "chrome" : options.CookieBrowserSpec);
            }
            if (options.Subtitles)
            {
                args.Add("--write-subs");
                args.Add("--write-auto-subs");
                args.Add("--sub-langs");
                args.Add("cs,en");
                args.Add("--convert-subs");
                args.Add("srt");
            }
            if (tools != null && tools.HasFfmpeg)
            {
                args.Add("--ffmpeg-location");
                args.Add(Path.GetDirectoryName(tools.FfmpegPath));
            }
            if (tools != null && tools.HasJsRuntime)
            {
                args.Add("--js-runtimes");
                args.Add(tools.JsRuntimeName.ToLowerInvariant() + ":" + tools.JsRuntimePath);
            }
            if (tools != null && !string.IsNullOrWhiteSpace(tools.PluginDirectory) && Directory.Exists(tools.PluginDirectory))
            {
                args.Add("--plugin-dirs");
                args.Add(tools.PluginDirectory);
            }
            if (!string.IsNullOrWhiteSpace(options.ExtraArguments))
                args.AddRange(Split(options.ExtraArguments));

            foreach (string url in urls)
                args.Add(url);
            return args;
        }

        internal static bool NeedsBrowserImpersonation(IEnumerable<string> urls)
        {
            if (urls == null)
                return false;
            foreach (string url in urls)
            {
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    continue;
                string host = uri.Host.TrimStart('.').ToLowerInvariant();
                if (host == "mujrozhlas.cz" || host.EndsWith(".mujrozhlas.cz", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public static void AddFormat(List<string> args, string preset, string quality, bool hasFfmpeg)
        {
            string height = QualityFilter(quality);
            string mode = string.IsNullOrWhiteSpace(preset) ? "mp4-h264" : preset.ToLowerInvariant();

            if (mode == "audio-mp3")
            {
                args.AddRange(new[] { "-f", "ba/bestaudio/best", "-x", "--audio-format", "mp3", "--audio-quality", "0" });
                return;
            }
            if (mode == "audio-m4a")
            {
                args.AddRange(new[] { "-f", "ba[ext=m4a]/ba/bestaudio/best", "-x", "--audio-format", "m4a" });
                return;
            }
            if (mode == "audio-opus")
            {
                args.AddRange(new[] { "-f", "ba[acodec^=opus]/ba/bestaudio/best", "-x", "--audio-format", "opus" });
                return;
            }
            if (mode == "audio-flac")
            {
                args.AddRange(new[] { "-f", "ba/bestaudio/best", "-x", "--audio-format", "flac" });
                return;
            }
            if (mode == "video-only")
            {
                args.AddRange(new[] { "-f", "bv*[vcodec^=avc1]" + height + "/bv*[vcodec!*=av01]" + height + "/bv*" + height + "/bv*" });
                return;
            }
            if (mode == "mkv-best")
            {
                args.AddRange(new[] { "-f", hasFfmpeg ? "bv*" + height + "+ba/b" + height + "/best" : "b" + height + "/best" + height + "/best", "--merge-output-format", "mkv" });
                if (hasFfmpeg)
                    args.AddRange(new[] { "--remux-video", "mkv" });
                return;
            }
            if (mode == "webm")
            {
                args.AddRange(new[] { "-f", "bv*[ext=webm]" + height + "+ba[ext=webm]/b[ext=webm]" + height + "/best" + height });
                return;
            }

            string selector;
            if (hasFfmpeg)
            {
                selector = "bv*[ext=mp4][vcodec^=avc1]" + height + "+ba[ext=m4a]/" +
                    "bv*[ext=mp4][vcodec!*=av01]" + height + "+ba[ext=m4a]/" +
                    "b[ext=mp4][vcodec^=avc1]" + height + "/" +
                    "b[ext=mp4][vcodec!*=av01]" + height + "/best" + height + "[vcodec!*=av01]/best" + height + "/best[vcodec!*=av01]/best";
            }
            else
            {
                selector = "b[ext=mp4][vcodec^=avc1]" + height + "/b[ext=mp4][vcodec!*=av01]" + height +
                    "/best" + height + "[vcodec!*=av01]/best" + height + "/best[vcodec!*=av01]/best";
            }
            args.AddRange(new[] { "-f", selector });
            if (hasFfmpeg)
                args.AddRange(new[] { "--merge-output-format", "mp4" });
        }

        private static string QualityFilter(string quality)
        {
            switch ((quality ?? "").Trim())
            {
                case "2160": return "[height<=2160]";
                case "1440": return "[height<=1440]";
                case "1080": return "[height<=1080]";
                case "720": return "[height<=720]";
                case "480": return "[height<=480]";
                default: return "";
            }
        }

        private static string NormalizeRateLimit(string value)
        {
            string normalized = (value ?? "").Trim().ToUpperInvariant();
            if (normalized.Length == 0)
                return "";
            if (!Regex.IsMatch(normalized, "^[1-9][0-9]*(?:\\.[0-9]+)?[KMG]$", RegexOptions.CultureInvariant))
                throw new ArgumentException("Omezení rychlosti nemá platný formát.");
            return normalized;
        }

        private static IEnumerable<string> Split(string text)
        {
            List<string> result = new List<string>();
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            foreach (char character in text)
            {
                if (character == '\"')
                {
                    quoted = !quoted;
                    continue;
                }
                if (char.IsWhiteSpace(character) && !quoted)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(character);
                }
            }
            if (current.Length > 0)
                result.Add(current.ToString());
            return result;
        }
    }

    internal static class DirectMediaArgumentBuilder
    {
        private static readonly HashSet<string> MediaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".webm", ".avi", ".mov", ".m4v",
            ".mp3", ".m4a", ".aac", ".opus", ".ogg", ".flac", ".wav"
        };

        internal static bool IsMediaPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                MediaExtensions.Contains(Path.GetExtension(path));
        }

        public static DirectPostProcessPlan Build(
            string inputPath,
            string preset,
            string quality,
            bool subtitles,
            bool noOverwrite,
            bool preserveInput,
            MediaInfo media)
        {
            if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
                throw new FileNotFoundException("Stažený soubor neexistuje.", inputPath);

            DirectPostProcessPlan plan = new DirectPostProcessPlan
            {
                OutputPath = inputPath,
                PreserveInput = preserveInput,
                ProfileLabel = "Původní soubor"
            };
            if (!IsMediaPath(inputPath))
                return plan;

            string mode = string.IsNullOrWhiteSpace(preset) ? "mp4-h264" : preset.ToLowerInvariant();
            string inputExtension = Path.GetExtension(inputPath).ToLowerInvariant();
            int maximumHeight = QualityHeight(quality);
            bool needsScale = maximumHeight > 0 && media != null && media.Height > maximumHeight;
            string outputExtension;

            switch (mode)
            {
                case "mkv-best":
                    outputExtension = ".mkv";
                    plan.ProfileLabel = needsScale ? "MKV / H.264" : "MKV / původní kvalita";
                    if (inputExtension == outputExtension && !needsScale)
                        return plan;
                    break;
                case "webm":
                    outputExtension = ".webm";
                    plan.ProfileLabel = "WebM / VP9";
                    if (inputExtension == outputExtension && !needsScale)
                        return plan;
                    break;
                case "audio-m4a":
                    outputExtension = ".m4a";
                    plan.ProfileLabel = "M4A / AAC";
                    if (inputExtension == outputExtension)
                        return plan;
                    break;
                case "audio-mp3":
                    outputExtension = ".mp3";
                    plan.ProfileLabel = "MP3";
                    if (inputExtension == outputExtension)
                        return plan;
                    break;
                case "audio-opus":
                    outputExtension = ".opus";
                    plan.ProfileLabel = "Opus";
                    if (inputExtension == outputExtension)
                        return plan;
                    break;
                case "audio-flac":
                    outputExtension = ".flac";
                    plan.ProfileLabel = "FLAC";
                    if (inputExtension == outputExtension)
                        return plan;
                    break;
                case "video-only":
                    outputExtension = ".mp4";
                    plan.ProfileLabel = "MP4 / H.264 bez zvuku";
                    break;
                default:
                    mode = "mp4-h264";
                    outputExtension = ".mp4";
                    plan.ProfileLabel = "MP4 / H.264";
                    if (inputExtension == outputExtension &&
                        media != null &&
                        string.Equals(media.Codec, "H.264", StringComparison.OrdinalIgnoreCase) &&
                        !needsScale)
                        return plan;
                    break;
            }

            string desiredPath = Path.ChangeExtension(inputPath, outputExtension);
            bool samePath = string.Equals(
                Path.GetFullPath(desiredPath),
                Path.GetFullPath(inputPath),
                StringComparison.OrdinalIgnoreCase);

            if (samePath && preserveInput)
            {
                desiredPath = Path.Combine(
                    Path.GetDirectoryName(inputPath),
                    Path.GetFileNameWithoutExtension(inputPath) + " - převedeno" + outputExtension);
                samePath = false;
            }
            if (!samePath && File.Exists(desiredPath))
            {
                if (noOverwrite)
                {
                    plan.ExistingOutput = true;
                    plan.OutputPath = desiredPath;
                    return plan;
                }
                desiredPath = UniquePath(
                    Path.GetDirectoryName(desiredPath),
                    Path.GetFileNameWithoutExtension(desiredPath),
                    outputExtension);
            }

            plan.Required = true;
            plan.ReplaceInput = samePath;
            plan.OutputPath = samePath ? inputPath : desiredPath;
            plan.WorkingOutputPath = TemporaryPath(plan.OutputPath, outputExtension);
            plan.DurationSeconds = media == null ? 0 : media.DurationSeconds;

            List<string> args = plan.Arguments;
            args.AddRange(new[] { "-y", "-hide_banner", "-loglevel", "warning", "-i", inputPath });

            if (mode == "audio-m4a" || mode == "audio-mp3" || mode == "audio-opus" || mode == "audio-flac")
            {
                args.AddRange(new[] { "-map", "0:a:0", "-vn", "-sn", "-map_metadata", "0" });
                if (mode == "audio-mp3")
                    args.AddRange(new[] { "-c:a", "libmp3lame", "-b:a", "320k" });
                else if (mode == "audio-opus")
                    args.AddRange(new[] { "-c:a", "libopus", "-b:a", "192k" });
                else if (mode == "audio-flac")
                    args.AddRange(new[] { "-c:a", "flac" });
                else
                    args.AddRange(new[] { "-c:a", "aac", "-b:a", "256k", "-movflags", "+faststart" });
            }
            else if (mode == "mkv-best" && !needsScale)
            {
                args.AddRange(new[] { "-map", "0:v?", "-map", "0:a?" });
                if (subtitles)
                    args.AddRange(new[] { "-map", "0:s?" });
                args.AddRange(new[] { "-map_metadata", "0", "-map_chapters", "0", "-c", "copy" });
            }
            else if (mode == "webm")
            {
                args.AddRange(new[] { "-map", "0:v:0", "-map", "0:a?" });
                if (subtitles)
                    args.AddRange(new[] { "-map", "0:s?" });
                args.AddRange(new[] { "-c:v", "libvpx-vp9", "-pix_fmt", "yuv420p", "-crf", "30", "-b:v", "0", "-c:a", "libopus", "-b:a", "160k" });
                if (subtitles)
                    args.AddRange(new[] { "-c:s", "webvtt" });
                AddScale(args, needsScale, maximumHeight);
            }
            else
            {
                args.AddRange(new[] { "-map", "0:v:0" });
                if (mode != "video-only")
                    args.AddRange(new[] { "-map", "0:a?" });
                if (subtitles && mode != "video-only")
                    args.AddRange(new[] { "-map", "0:s?" });
                args.AddRange(new[] { "-c:v", "libx264", "-preset", "medium", "-pix_fmt", "yuv420p", "-crf", "21" });
                AddScale(args, needsScale, maximumHeight);

                if (mode == "mkv-best")
                {
                    args.AddRange(new[] { "-c:a", "copy" });
                    if (subtitles)
                        args.AddRange(new[] { "-c:s", "copy" });
                }
                else if (mode == "video-only")
                {
                    args.AddRange(new[] { "-an", "-sn", "-movflags", "+faststart" });
                }
                else
                {
                    args.AddRange(new[] { "-c:a", "aac", "-b:a", "192k" });
                    if (subtitles)
                        args.AddRange(new[] { "-c:s", "mov_text" });
                    args.AddRange(new[] { "-movflags", "+faststart" });
                }
            }

            args.AddRange(new[] { "-progress", "pipe:1", "-nostats", plan.WorkingOutputPath });
            return plan;
        }

        private static void AddScale(List<string> args, bool required, int height)
        {
            if (required)
                args.AddRange(new[] { "-vf", "scale=-2:" + height });
        }

        private static int QualityHeight(string quality)
        {
            int value;
            return int.TryParse((quality ?? "").Trim(), out value) &&
                (value == 480 || value == 720 || value == 1080 || value == 1440 || value == 2160) ? value : 0;
        }

        private static string TemporaryPath(string outputPath, string extension)
        {
            return Path.Combine(
                Path.GetDirectoryName(outputPath),
                Path.GetFileNameWithoutExtension(outputPath) + ".mvtmp-" + Guid.NewGuid().ToString("N") + extension);
        }

        private static string UniquePath(string directory, string baseName, string extension)
        {
            string path = Path.Combine(directory, baseName + extension);
            for (int index = 2; File.Exists(path); index++)
                path = Path.Combine(directory, baseName + " (" + index + ")" + extension);
            return path;
        }
    }

    internal static class ConversionArgumentBuilder
    {
        public static List<string> Build(ConversionOptions options, out string outputPath)
        {
            if (options == null)
                throw new ArgumentNullException("options");
            if (string.IsNullOrWhiteSpace(options.InputPath) || !File.Exists(options.InputPath))
                throw new FileNotFoundException("Vstupní soubor neexistuje.", options.InputPath);
            Directory.CreateDirectory(options.OutputDirectory);

            string format = NormalizeFormat(options.Format);
            outputPath = UniquePath(options.OutputDirectory, Path.GetFileNameWithoutExtension(options.InputPath), format);
            List<string> args = new List<string>();
            args.AddRange(new[] { "-y", "-hide_banner", "-i", options.InputPath });
            args.AddRange(new[] { "-map", "0:v:0", "-map", "0:a?", "-sn", "-map_metadata", "0", "-map_chapters", "0" });

            string codec = NormalizeCodec(options.Codec);
            if (codec == "h265")
                args.AddRange(new[] { "-c:v", "libx265", "-preset", "medium" });
            else if (codec == "av1")
                args.AddRange(new[] { "-c:v", "libaom-av1", "-cpu-used", "6" });
            else
                args.AddRange(new[] { "-c:v", "libx264", "-preset", "medium" });
            args.AddRange(new[] { "-pix_fmt", "yuv420p" });
            if (codec == "h265" && (format == "mp4" || format == "mov"))
                args.AddRange(new[] { "-tag:v", "hvc1" });

            if (string.Equals(options.RateControl, "bitrate", StringComparison.OrdinalIgnoreCase))
                args.AddRange(new[] { "-b:v", NormalizeVideoBitrate(options.VideoBitrate) });
            else
                args.AddRange(new[] { "-crf", NormalizeCrf(options.Crf) });

            string audioCodec = NormalizeAudioCodec(options.AudioCodec);
            if (format == "webm")
                audioCodec = "opus";
            if (audioCodec == "mp3")
                args.AddRange(new[] { "-c:a", "libmp3lame" });
            else if (audioCodec == "opus")
                args.AddRange(new[] { "-c:a", "libopus" });
            else if (audioCodec == "flac")
                args.AddRange(new[] { "-c:a", "flac" });
            else
                args.AddRange(new[] { "-c:a", "aac" });
            if (audioCodec != "flac")
                args.AddRange(new[] { "-b:a", NormalizeAudioBitrate(options.AudioBitrate) });
            if (format == "mp4" || format == "mov")
                args.AddRange(new[] { "-movflags", "+faststart" });
            args.AddRange(new[] { "-progress", "pipe:1", "-nostats", outputPath });
            return args;
        }

        private static string NormalizeFormat(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "mkv": return "mkv";
                case "webm": return "webm";
                case "mov": return "mov";
                case "avi": return "avi";
                default: return "mp4";
            }
        }

        private static string NormalizeCodec(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "h265": return "h265";
                case "av1": return "av1";
                default: return "h264";
            }
        }

        private static string NormalizeAudioCodec(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "mp3": return "mp3";
                case "opus": return "opus";
                case "flac": return "flac";
                default: return "aac";
            }
        }

        private static string NormalizeCrf(string value)
        {
            int number;
            return int.TryParse(value, out number) && number >= 0 && number <= 51 ? number.ToString() : "23";
        }

        private static string NormalizeVideoBitrate(string value)
        {
            string text = (value ?? "").Trim().ToLowerInvariant();
            return System.Text.RegularExpressions.Regex.IsMatch(text, "^[0-9]{2,6}[km]$") ? text : "6000k";
        }

        private static string NormalizeAudioBitrate(string value)
        {
            string text = (value ?? "").Trim().ToLowerInvariant();
            return text == "128k" || text == "192k" || text == "256k" || text == "320k" ? text : "192k";
        }

        private static string UniquePath(string folder, string baseName, string extension)
        {
            string path = Path.Combine(folder, baseName + "." + extension);
            int index = 2;
            while (File.Exists(path))
            {
                path = Path.Combine(folder, baseName + " (" + index + ")." + extension);
                index++;
            }
            return path;
        }
    }
}
