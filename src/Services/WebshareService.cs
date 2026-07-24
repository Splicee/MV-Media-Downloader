using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MVMediaStudio.Core;

namespace MVMediaStudio.Services
{
    internal sealed class WebshareLoginResult
    {
        public string UserName;
        public string Token;
    }

    internal static class WebshareService
    {
        private const string ApiBase = "https://webshare.cz/api/";
        private static string sessionToken;

        public static bool HasSession
        {
            get { return !string.IsNullOrWhiteSpace(GetSessionToken()); }
        }

        public static async Task<WebshareLoginResult> LoginAsync(string userName, string password, bool remember)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
                throw new InvalidOperationException("Zadej uživatelské jméno a heslo Webshare.");
            XmlDocument saltResponse = await PostAsync("salt/", new Dictionary<string, string>
            {
                { "username_or_email", userName.Trim() }
            });
            EnsureOk(saltResponse);
            string salt = Value(saltResponse, "salt");
            if (string.IsNullOrWhiteSpace(salt))
                throw new InvalidOperationException("Webshare nevrátilo přihlašovací sůl.");

            string passwordHash = WebsharePasswordHash.Create(password, salt);
            XmlDocument loginResponse = await PostAsync("login/", new Dictionary<string, string>
            {
                { "username_or_email", userName.Trim() },
                { "password", passwordHash },
                { "keep_logged_in", remember ? "1" : "0" }
            });
            EnsureOk(loginResponse);
            string token = Value(loginResponse, "token");
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Webshare nevytvořilo přihlašovací relaci.");

            sessionToken = token;
            if (remember)
                SaveSession(token);
            else
                DeleteSavedSession();
            return new WebshareLoginResult { UserName = userName.Trim(), Token = token };
        }

        public static void Logout()
        {
            sessionToken = "";
            DeleteSavedSession();
        }

        public static async Task<DirectDownloadItem> ResolveAsync(string sourceUrl)
        {
            string ident = DownloadSourceRouter.ExtractWebshareIdent(sourceUrl);
            if (string.IsNullOrWhiteSpace(ident))
                throw new InvalidOperationException("Webshare odkaz neobsahuje platný identifikátor souboru.");

            XmlDocument info = await PostAsync("file_info/", FormWithSession(new Dictionary<string, string>
            {
                { "ident", ident },
                { "password", "" },
                { "maybe_removed", "0" }
            }));
            EnsureOk(info);
            string name = Value(info, "name");
            long size;
            long.TryParse(Value(info, "size"), out size);
            if (Value(info, "password") == "1")
                throw new InvalidOperationException("Soubor Webshare je chráněný heslem. Tato verze podporuje pouze soubory přístupné přes účet.");
            if (Value(info, "available") == "0")
                throw new InvalidOperationException("Soubor Webshare momentálně není dostupný ke stažení.");

            XmlDocument link = await PostAsync("file_link/", FormWithSession(new Dictionary<string, string>
            {
                { "ident", ident },
                { "password", "" },
                { "download_type", "file_download" },
                { "device_uuid", WebshareDeviceId.Value },
                { "device_vendor", "MV" },
                { "device_model", "Media Downloader" },
                { "device_res_x", "1920" },
                { "device_res_y", "1080" },
                { "force_https", "1" }
            }));
            EnsureOk(link);
            string directUrl = Value(link, "link");
            Uri parsed;
            if (!Uri.TryCreate(directUrl, UriKind.Absolute, out parsed) || parsed.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Webshare nevrátilo bezpečný odkaz ke stažení.");

            return new DirectDownloadItem
            {
                Provider = "Webshare",
                SourceUrl = sourceUrl,
                DownloadUrl = directUrl,
                FileName = string.IsNullOrWhiteSpace(name) ? ident + ".bin" : name,
                ExpectedSize = size
            };
        }

        private static Dictionary<string, string> FormWithSession(Dictionary<string, string> values)
        {
            string token = GetSessionToken();
            if (!string.IsNullOrWhiteSpace(token))
                values["wst"] = token;
            return values;
        }

        private static Task<XmlDocument> PostAsync(string endpoint, Dictionary<string, string> values)
        {
            return Task.Run(delegate
            {
                using (WebshareWebClient client = new WebshareWebClient())
                {
                    ConfigureApiClient(client);
                    byte[] response = client.UploadValues(ApiBase + endpoint, "POST", ToNameValueCollection(values));
                    XmlDocument document = new XmlDocument();
                    document.LoadXml(Encoding.UTF8.GetString(response));
                    return document;
                }
            });
        }

        internal static void ConfigureApiClient(WebClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            client.Encoding = Encoding.UTF8;
            client.Headers[HttpRequestHeader.Accept] = "text/xml; charset=UTF-8";
        }

        private static System.Collections.Specialized.NameValueCollection ToNameValueCollection(Dictionary<string, string> values)
        {
            System.Collections.Specialized.NameValueCollection result = new System.Collections.Specialized.NameValueCollection();
            foreach (KeyValuePair<string, string> pair in values)
                result[pair.Key] = pair.Value ?? "";
            return result;
        }

        private static void EnsureOk(XmlDocument document)
        {
            if (string.Equals(Value(document, "status"), "OK", StringComparison.OrdinalIgnoreCase))
                return;
            string code = Value(document, "code");
            string message = Value(document, "message");
            if (code == "FILE_INFO_FATAL_2" || code == "FILE_LINK_FATAL_3")
                message = "Soubor Webshare je chráněný samostatným heslem souboru. Přihlášení k účtu toto heslo nenahrazuje.";
            else if (code == "FILE_INFO_FATAL_3")
                message = "Webshare tento soubor neposkytlo přes veřejné API. Přihlas se účtem, který k němu má oprávněný přístup.";
            else if (code == "FILE_LINK_FATAL_4")
                message = "Soubor je dočasně nedostupný.";
            else if (code == "FILE_LINK_FATAL_5")
                message = "Účet Webshare má příliš mnoho současných stahování.";
            else if (code == "FILE_LINK_FATAL_6")
                message = "Webshare tento soubor neposkytlo přes veřejné API. Přihlas se účtem, který k němu má oprávněný přístup.";
            else if (code == "LOGIN_FATAL_1")
                message = "Nesprávné přihlašovací údaje Webshare.";
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "Webshare API vrátilo chybu " + code + "." : message);
        }

        private static string Value(XmlDocument document, string name)
        {
            XmlNode node = document == null ? null : document.SelectSingleNode("/response/" + name);
            return node == null ? "" : node.InnerText.Trim();
        }

        private static string GetSessionToken()
        {
            if (!string.IsNullOrWhiteSpace(sessionToken))
                return sessionToken;
            try
            {
                if (!File.Exists(AppPaths.WebshareSessionPath))
                    return "";
                byte[] encrypted = File.ReadAllBytes(AppPaths.WebshareSessionPath);
                byte[] plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                sessionToken = Encoding.UTF8.GetString(plain);
            }
            catch
            {
                DeleteSavedSession();
                sessionToken = "";
            }
            return sessionToken ?? "";
        }

        private static void SaveSession(string token)
        {
            AppPaths.EnsureDirectories();
            byte[] plain = Encoding.UTF8.GetBytes(token);
            byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(AppPaths.WebshareSessionPath, encrypted);
        }

        private static void DeleteSavedSession()
        {
            try
            {
                if (File.Exists(AppPaths.WebshareSessionPath))
                    File.Delete(AppPaths.WebshareSessionPath);
            }
            catch
            {
            }
        }

        private sealed class WebshareWebClient : WebClient
        {
            public WebshareWebClient()
            {
                Encoding = Encoding.UTF8;
                Headers[HttpRequestHeader.UserAgent] = "MV-Media-Downloader/3.1.1";
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                HttpWebRequest http = request as HttpWebRequest;
                if (http != null)
                {
                    http.Referer = "https://webshare.cz/";
                    http.Timeout = 30000;
                }
                return request;
            }
        }
    }

    internal static class WebshareDeviceId
    {
        public static string Value
        {
            get
            {
                string seed = Environment.UserName + "|" + Environment.MachineName + "|MVMediaDownloader";
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
                    byte[] guid = new byte[16];
                    Array.Copy(hash, guid, guid.Length);
                    return new Guid(guid).ToString();
                }
            }
        }
    }

    internal static class WebsharePasswordHash
    {
        private const string Alphabet = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        public static string Create(string password, string salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? "");
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt ?? "");
            byte[] prefix = Encoding.ASCII.GetBytes("$1$");
            List<byte> context = new List<byte>();
            context.AddRange(passwordBytes);
            context.AddRange(prefix);
            context.AddRange(saltBytes);
            byte[] alternate = Md5(Combine(passwordBytes, saltBytes, passwordBytes));
            for (int remaining = passwordBytes.Length; remaining > 0; remaining -= 16)
                context.AddRange(Slice(alternate, 0, Math.Min(16, remaining)));
            for (int value = passwordBytes.Length; value != 0; value >>= 1)
                context.Add((value & 1) == 1 ? (byte)0 : passwordBytes[0]);
            byte[] digest = Md5(context.ToArray());

            for (int round = 0; round < 1000; round++)
            {
                context.Clear();
                context.AddRange((round & 1) == 1 ? passwordBytes : digest);
                if (round % 3 != 0)
                    context.AddRange(saltBytes);
                if (round % 7 != 0)
                    context.AddRange(passwordBytes);
                context.AddRange((round & 1) == 1 ? digest : passwordBytes);
                digest = Md5(context.ToArray());
            }

            string md5Crypt = "$1$" + salt + "$" +
                Encode(digest, 0, 6, 12, 4) +
                Encode(digest, 1, 7, 13, 4) +
                Encode(digest, 2, 8, 14, 4) +
                Encode(digest, 3, 9, 15, 4) +
                Encode(digest, 4, 10, 5, 4) +
                EncodeSingle(digest[11], 2);
            using (SHA1 sha = SHA1.Create())
            {
                byte[] result = sha.ComputeHash(Encoding.ASCII.GetBytes(md5Crypt));
                StringBuilder output = new StringBuilder(result.Length * 2);
                foreach (byte value in result)
                    output.Append(value.ToString("x2"));
                return output.ToString();
            }
        }

        private static byte[] Md5(byte[] value)
        {
            using (MD5 md5 = MD5.Create())
                return md5.ComputeHash(value);
        }

        private static byte[] Combine(params byte[][] parts)
        {
            List<byte> result = new List<byte>();
            foreach (byte[] part in parts)
                result.AddRange(part);
            return result.ToArray();
        }

        private static byte[] Slice(byte[] value, int start, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(value, start, result, 0, length);
            return result;
        }

        private static string Encode(byte[] value, int first, int second, int third, int count)
        {
            int number = (value[first] << 16) | (value[second] << 8) | value[third];
            return EncodeSingle(number, count);
        }

        private static string EncodeSingle(int value, int count)
        {
            StringBuilder result = new StringBuilder();
            while (count-- > 0)
            {
                result.Append(Alphabet[value & 0x3f]);
                value >>= 6;
            }
            return result.ToString();
        }
    }
}
