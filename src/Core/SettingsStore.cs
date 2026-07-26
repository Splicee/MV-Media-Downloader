using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MVMediaStudio.Core
{
    internal sealed class AppSettings
    {
        public string DownloadDirectory = AppPaths.DefaultDownloadDirectory;
        public string ConversionDirectory = AppPaths.DefaultDownloadDirectory;
        public string DownloadPreset = "mp4-h264";
        public string DownloadQuality = "1080";
        public string DownloadRateLimit = "";
        public string CookieBrowser = "chrome";
        public string WebshareUserName = "";
        public string ConversionFormat = "mp4";
        public string ConversionCodec = "h264";
        public string ConversionAudioCodec = "aac";
        public string Theme = "dark";
        public bool AdvancedMode;
        public bool UseBrowserCookies;
        public bool NoOverwrite = true;
        public bool Subtitles;
        public bool AutoUpdate = true;

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();
            try
            {
                if (!File.Exists(AppPaths.SettingsPath))
                    return settings;

                Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string line in File.ReadAllLines(AppPaths.SettingsPath, Encoding.UTF8))
                {
                    int split = line.IndexOf('=');
                    if (split > 0)
                        values[line.Substring(0, split).Trim()] = line.Substring(split + 1).Trim();
                }

                settings.DownloadDirectory = Get(values, "DownloadDirectory", settings.DownloadDirectory);
                settings.ConversionDirectory = Get(values, "ConversionDirectory", settings.ConversionDirectory);
                settings.DownloadPreset = Get(values, "DownloadPreset", settings.DownloadPreset);
                settings.DownloadQuality = Get(values, "DownloadQuality", settings.DownloadQuality);
                settings.DownloadRateLimit = Get(values, "DownloadRateLimit", settings.DownloadRateLimit, true);
                settings.CookieBrowser = Get(values, "CookieBrowser", settings.CookieBrowser);
                settings.WebshareUserName = Get(values, "WebshareUserName", settings.WebshareUserName, true);
                settings.ConversionFormat = Get(values, "ConversionFormat", settings.ConversionFormat);
                settings.ConversionCodec = Get(values, "ConversionCodec", settings.ConversionCodec);
                settings.ConversionAudioCodec = Get(values, "ConversionAudioCodec", settings.ConversionAudioCodec);
                settings.Theme = Get(values, "Theme", settings.Theme);
                settings.AdvancedMode = GetBool(values, "AdvancedMode", settings.AdvancedMode);
                settings.UseBrowserCookies = GetBool(values, "UseBrowserCookies", settings.UseBrowserCookies);
                settings.NoOverwrite = GetBool(values, "NoOverwrite", settings.NoOverwrite);
                settings.Subtitles = GetBool(values, "Subtitles", settings.Subtitles);
                settings.AutoUpdate = GetBool(values, "AutoUpdate", settings.AutoUpdate);
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
            }
            return settings;
        }

        public void Save()
        {
            try
            {
                AppPaths.EnsureDirectories();
                string[] lines = new[]
                {
                    "DownloadDirectory=" + DownloadDirectory,
                    "ConversionDirectory=" + ConversionDirectory,
                    "DownloadPreset=" + DownloadPreset,
                    "DownloadQuality=" + DownloadQuality,
                    "DownloadRateLimit=" + DownloadRateLimit,
                    "CookieBrowser=" + CookieBrowser,
                    "WebshareUserName=" + WebshareUserName,
                    "ConversionFormat=" + ConversionFormat,
                    "ConversionCodec=" + ConversionCodec,
                    "ConversionAudioCodec=" + ConversionAudioCodec,
                    "Theme=" + Theme,
                    "AdvancedMode=" + AdvancedMode,
                    "UseBrowserCookies=" + UseBrowserCookies,
                    "NoOverwrite=" + NoOverwrite,
                    "Subtitles=" + Subtitles,
                    "AutoUpdate=" + AutoUpdate
                };
                File.WriteAllLines(AppPaths.SettingsPath, lines, Encoding.UTF8);
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
            }
        }

        private static string Get(Dictionary<string, string> values, string key, string fallback)
        {
            return Get(values, key, fallback, false);
        }

        private static string Get(Dictionary<string, string> values, string key, string fallback, bool allowEmpty)
        {
            string value;
            if (!values.TryGetValue(key, out value))
                return fallback;
            return allowEmpty || !string.IsNullOrWhiteSpace(value) ? value : fallback;
        }

        private static bool GetBool(Dictionary<string, string> values, string key, bool fallback)
        {
            string value;
            bool parsed;
            return values.TryGetValue(key, out value) && bool.TryParse(value, out parsed) ? parsed : fallback;
        }
    }
}
