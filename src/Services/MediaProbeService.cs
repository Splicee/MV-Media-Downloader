using System;
using System.Collections.Generic;
using System.Globalization;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal static class MediaProbeService
    {
        public static MediaInfo Probe(string ffprobePath, string sourcePath)
        {
            MediaInfo media = new MediaInfo();
            if (string.IsNullOrWhiteSpace(ffprobePath))
                return media;

            List<string> args = new List<string>();
            args.AddRange(new[]
            {
                "-v", "error",
                "-select_streams", "v:0",
                "-show_entries", "stream=codec_name,profile,width,height,bit_rate:format=duration",
                "-of", "default=noprint_wrappers=1",
                sourcePath
            });

            string output = ProcessService.Capture(ffprobePath, args, 15000);
            Dictionary<string, string> values = Parse(output);
            string value;
            if (values.TryGetValue("codec_name", out value))
                media.Codec = FriendlyCodec(value);
            if (values.TryGetValue("profile", out value))
                media.Profile = value;
            if (values.TryGetValue("width", out value))
                int.TryParse(value, out media.Width);
            if (values.TryGetValue("height", out value))
                int.TryParse(value, out media.Height);
            if (values.TryGetValue("bit_rate", out value))
                long.TryParse(value, out media.Bitrate);
            if (values.TryGetValue("duration", out value))
                double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out media.DurationSeconds);
            return media;
        }

        private static Dictionary<string, string> Parse(string text)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int split = rawLine.IndexOf('=');
                if (split > 0)
                    values[rawLine.Substring(0, split).Trim()] = rawLine.Substring(split + 1).Trim();
            }
            return values;
        }

        private static string FriendlyCodec(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "h264": return "H.264";
                case "hevc": return "H.265 / HEVC";
                case "av1": return "AV1";
                case "vp9": return "VP9";
                case "mpeg4": return "MPEG-4";
                default: return string.IsNullOrWhiteSpace(value) ? "—" : value.ToUpperInvariant();
            }
        }
    }
}
