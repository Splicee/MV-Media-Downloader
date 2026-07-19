using System;
using System.Diagnostics;
using System.IO;

namespace MVMediaStudio.Services
{
    internal static class JojLoginService
    {
        public static readonly string UserDataDirectory = Path.Combine(Core.AppPaths.DataDirectory, "joj-chrome-profile");
        public static readonly string ProfileDirectory = Path.Combine(UserDataDirectory, "Default");
        public static readonly string ReadyPath = Path.Combine(UserDataDirectory, ".login-ready");

        public static bool IsReady
        {
            get { return File.Exists(ReadyPath); }
        }

        public static bool OpenLogin()
        {
            string chromePath = FindChrome();
            if (string.IsNullOrWhiteSpace(chromePath))
                return false;

            Directory.CreateDirectory(UserDataDirectory);
            RemoveSavedPasswords();
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = chromePath,
                Arguments = "--user-data-dir=" + Quote(UserDataDirectory) +
                    " --profile-directory=Default --disable-save-password-bubble" +
                    " --disable-features=PasswordManagerOnboarding,PasswordManagerAccountStorage,PasswordManagerEnablePasskeys" +
                    " https://play.joj.sk/",
                UseShellExecute = true
            };
            Process.Start(start);
            return true;
        }

        public static bool MarkReady()
        {
            Directory.CreateDirectory(UserDataDirectory);
            if (!RemoveSavedPasswords())
                return false;
            File.WriteAllText(ReadyPath, DateTime.UtcNow.ToString("O"));
            return true;
        }

        public static bool RemoveSavedPasswords()
        {
            try
            {
                if (!Directory.Exists(ProfileDirectory))
                    return true;
                foreach (string path in Directory.GetFiles(ProfileDirectory, "Login Data*", SearchOption.TopDirectoryOnly))
                    File.Delete(path);
                return Directory.GetFiles(ProfileDirectory, "Login Data*", SearchOption.TopDirectoryOnly).Length == 0;
            }
            catch
            {
                return false;
            }
        }

        private static string FindChrome()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe")
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }
            return "";
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
