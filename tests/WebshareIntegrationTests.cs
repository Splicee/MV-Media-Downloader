using System;
using System.Threading.Tasks;
using MVMediaStudio.Core;
using MVMediaStudio.Services;

namespace MVMediaStudio.Tests
{
    internal static class WebshareIntegrationTests
    {
        public static int Main(string[] args)
        {
            try
            {
                return RunAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("CHYBA: " + error);
                return 1;
            }
        }

        private static async Task<int> RunAsync(string[] args)
        {
            if (args == null || args.Length != 1)
            {
                Console.Error.WriteLine("Použití: webshare-test.cmd \"https://webshare.cz/#/file/...\"");
                return 2;
            }

            DownloadRoute route = DownloadSourceRouter.Classify(args[0]);
            if (route.Kind != DownloadProviderKind.Webshare)
            {
                Console.Error.WriteLine("CHYBA: Odkaz nebyl rozpoznán jako Webshare.");
                return 3;
            }

            DirectDownloadItem item = await WebshareService.ResolveAsync(route.Url);
            Uri direct;
            bool secure = Uri.TryCreate(item.DownloadUrl, UriKind.Absolute, out direct) &&
                direct.Scheme == Uri.UriSchemeHttps;
            Console.WriteLine("OK: Webshare API vrátilo bezpečný odkaz.");
            Console.WriteLine("Soubor: " + item.FileName);
            Console.WriteLine("Velikost: " + item.ExpectedSize + " B");
            return secure && !string.IsNullOrWhiteSpace(item.FileName) ? 0 : 4;
        }
    }
}
