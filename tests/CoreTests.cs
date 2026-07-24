using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using MVMediaStudio.Core;
using MVMediaStudio.Services;

namespace MVMediaStudio.Tests
{
    internal static class CoreTests
    {
        private static int failures;

        public static int Main()
        {
            Equal("abc", ArgumentUtilities.Quote("abc"), "jednoduchý argument");
            Equal("\"a b\"", ArgumentUtilities.Quote("a b"), "argument s mezerou");
            TestDownloadUrlParser();
            TestScrollWheel();
            TestDownloadPreset();
            TestDirectPostProcessing();
            TestDownloadProviders();
            TestDiagnosticRedaction();
            TestJojResolver();
            TestConversion();
            TestUpdateMetadata();
            Console.WriteLine(failures == 0 ? "Všechny testy prošly." : "Počet chyb: " + failures);
            return failures == 0 ? 0 : 1;
        }

        private static void TestScrollWheel()
        {
            int remainder = 0;
            Equal("0", ScrollWheelTuning.ConsumeSteps(ref remainder, -30).ToString(), "jemný impuls ještě neposouvá");
            Equal("0", ScrollWheelTuning.ConsumeSteps(ref remainder, -30).ToString(), "druhý jemný impuls se sčítá");
            Equal("0", ScrollWheelTuning.ConsumeSteps(ref remainder, -30).ToString(), "třetí jemný impuls se sčítá");
            Equal("-1", ScrollWheelTuning.ConsumeSteps(ref remainder, -30).ToString(), "úplný krok posune jednou");
            Equal("0", remainder.ToString(), "po úplném kroku nezůstane přebytek");
            Equal("2", ScrollWheelTuning.ConsumeSteps(ref remainder, 240).ToString(), "dva kroky se zachovají");
        }

        private static void TestDownloadUrlParser()
        {
            string input =
                "01 https://play.joj.sk/player/qCI7gycOYCiUTiuYTrsQ?type=VIDEO\r\n" +
                "02 https://play.joj.sk/player/ht9XyGCjZbNft5DLH9LE?type=VIDEO\r\n" +
                "03 https://play.joj.sk/player/l5izvcWkaeL8hvWvrJCp?type=VIDEO\r\n" +
                "04 https://play.joj.sk/player/pRKJsFZRqasNtwNnlN08?type=VIDEO\r\n" +
                "05 https://play.joj.sk/player/N3UeKfrE95T38u0VMugT?type=VIDEO\r\n" +
                "06 https://play.joj.sk/player/vOKvCNsNxpzg6hDF6qJv?type=VIDEO\r\n" +
                "07 https://play.joj.sk/player/o8yTfAcOYkXYhWkp6pTn?type=VIDEO\r\n" +
                "08 https://play.joj.sk/player/6ypkBfoaCB8aG953SGwB?type=VIDEO\r\n" +
                "09 https://play.joj.sk/player/ftr4RUxdnYasRNVcGZc7?type=VIDEO\r\n" +
                "10 https://play.joj.sk/player/wLTarQi0pL0YXFadlYb0?type=VIDEO\r\n" +
                "11 https://play.joj.sk/player/kgrBgKMOPqgcon9aCO7q?type=VIDEO\r\n" +
                "12 https://play.joj.sk/player/moi2iLiNXs4pgDIivVaf?type=VIDEO\r\n" +
                "13 https://play.joj.sk/player/Ecv07zIjPc4lLdWeZgHT?type=VIDEO\r\n" +
                "14 https://play.joj.sk/player/NvgqTlpiQ1awh9qzyM7U?type=VIDEO\r\n" +
                "15 https://play.joj.sk/player/Sx42BisQ1iJU9Z2thlHY?type=VIDEO";

            List<string> urls = DownloadUrlParser.Parse(input);
            Equal("15", urls.Count.ToString(), "číslovaný seznam načte všech 15 odkazů");
            Equal("https://play.joj.sk/player/qCI7gycOYCiUTiuYTrsQ?type=VIDEO", urls[0], "JOJ odkaz zachová parametry");
            Equal("https://play.joj.sk/player/Sx42BisQ1iJU9Z2thlHY?type=VIDEO", urls[14], "poslední JOJ odkaz se neztratí");

            string slugInput = string.Join("\r\n", Enumerable.Range(1, 56).Select(index =>
                index.ToString("00") + " https://play.joj.sk/player/inkognito-s2-e" + index + "?type=VIDEO"));
            List<string> slugUrls = DownloadUrlParser.Parse(slugInput);
            Equal("56", slugUrls.Count.ToString(), "seznam slugů načte všech 56 odkazů");
            Equal("https://play.joj.sk/player/inkognito-s2-e56?type=VIDEO", slugUrls[55], "poslední slug JOJ se neztratí");

            string firstWebshare = "https://webshare.cz/#/file/Vzgv8t6d2P/prvni.mkv";
            string secondWebshare = "https://webshare.cz/#/file/gNQNhJMfu2/druhy.mkv";
            string markdown = "[**" + firstWebshare + "**](" + firstWebshare + ")\r\n" +
                "[**" + secondWebshare + "**](" + secondWebshare + ")";
            List<string> markdownUrls = DownloadUrlParser.Parse(markdown);
            Equal("2", markdownUrls.Count.ToString(), "Markdown seznam Webshare odstraní duplicitní odkazy");
            Equal(firstWebshare, markdownUrls[0], "Markdown zachová první Webshare odkaz");
            Equal(secondWebshare, markdownUrls[1], "Markdown zachová druhý Webshare odkaz");
        }

        private static void TestJojResolver()
        {
            string component = "<joj-video-player :data='&#123;&quot;videoEmbedUrl&quot;:&quot;https://media.joj.sk/embed/4opHLD1T5rt&quot;&#125;'></joj-video-player>";
            Equal("https://media.joj.sk/embed/4opHLD1T5rt", JojUrlResolver.ExtractPublicMediaUrl(component), "JOJ epizoda převede vložený přehrávač");

            string iframe = "<iframe src=\"//media.joj.sk/embed/a388ec4c-6019-4a4a-9312-b1bee194e932\"></iframe>";
            Equal("https://media.joj.sk/embed/a388ec4c-6019-4a4a-9312-b1bee194e932", JojUrlResolver.ExtractPublicMediaUrl(iframe), "JOJ podporuje starší iframe");
            Equal("", JojUrlResolver.ExtractPublicMediaUrl("<html>bez videa</html>"), "JOJ seznam epizod se nevydává za video");

            const string playUrl = "https://play.joj.sk/player/qCI7gycOYCiUTiuYTrsQ?type=VIDEO";
            DownloadUrlResolution play = JojUrlResolver.ResolveAsync(new[] { playUrl }).GetAwaiter().GetResult();
            Equal(playUrl, play.Urls[0], "JOJ Play odkaz předá specializovanému konektoru");
        }

        private static void TestDownloadPreset()
        {
            string pluginDirectory = Path.Combine(Path.GetTempPath(), "mv-media-downloader-plugin-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pluginDirectory);
            List<string> args = new List<string>();
            DownloadArgumentBuilder.AddFormat(args, "mp4-h264", "1080", true);
            string selector = args.SkipWhile(value => value != "-f").Skip(1).FirstOrDefault() ?? "";
            True(selector.IndexOf("vcodec^=avc1", StringComparison.OrdinalIgnoreCase) >= 0, "MP4 preferuje H.264");
            True(selector.IndexOf("vcodec!*=av01", StringComparison.OrdinalIgnoreCase) >= 0, "MP4 se vyhýbá AV1");
            True(selector.EndsWith("/best", StringComparison.Ordinal), "MP4 má fallback pro zdroje bez údajů o rozlišení");
            True(args.Contains("--merge-output-format") && args.Contains("mp4"), "MP4 sloučení");

            DownloadOptions cookieOptions = new DownloadOptions
            {
                Preset = "mp4-h264",
                Quality = "720",
                RateLimit = "3000K",
                OutputDirectory = Path.GetTempPath(),
                CookiesFromBrowser = true,
                CookieBrowserSpec = "chrome:C:\\JOJ\\Default"
            };
            args = DownloadArgumentBuilder.Build(cookieOptions, new[] { "https://play.joj.sk/player/test" }, new ToolState { PluginDirectory = pluginDirectory });
            True(args.Contains("after_move:MV_DONE:%(filepath)s"), "dokončené soubory mají samostatný stav");
            True(args.Contains("--continue") && args.Contains("--part"), "rozpracovaný soubor lze bezpečně navázat");
            int progressDeltaIndex = args.IndexOf("--progress-delta");
            True(progressDeltaIndex >= 0 && args[progressDeltaIndex + 1] == "0.5", "průběh omezuje opakovaný výstup");
            int cookieIndex = args.IndexOf("--cookies-from-browser");
            True(cookieIndex >= 0 && args[cookieIndex + 1] == "chrome:C:\\JOJ\\Default", "JOJ používá oddělený Chrome profil");
            int rateIndex = args.IndexOf("--limit-rate");
            True(rateIndex >= 0 && args[rateIndex + 1] == "3000K", "vlastní omezení rychlosti se předá yt-dlp");
            int pluginIndex = args.IndexOf("--plugin-dirs");
            True(pluginIndex >= 0 && args[pluginIndex + 1] == pluginDirectory, "JOJ plugin se předá yt-dlp výslovně");

            args.Clear();
            DownloadArgumentBuilder.AddFormat(args, "audio-mp3", "auto", true);
            True(args.Contains("ba/bestaudio/best"), "audio má fallback na kombinovaný stream");

            args.Clear();
            DownloadArgumentBuilder.AddFormat(args, "audio-flac", "auto", true);
            True(args.Contains("--audio-format") && args.Contains("flac"), "FLAC audio se předá yt-dlp");

            args.Clear();
            DownloadArgumentBuilder.AddFormat(args, "mkv-best", "720", true);
            True(args.Contains("--remux-video") && args.Contains("mkv"), "MKV přebalí i přímý kombinovaný soubor");

            cookieOptions.RateLimit = "neplatne";
            bool rejectedRate = false;
            try { DownloadArgumentBuilder.Build(cookieOptions, new[] { "https://example.com/video" }, new ToolState()); }
            catch (ArgumentException) { rejectedRate = true; }
            True(rejectedRate, "neplatné omezení rychlosti se odmítne");
            try { Directory.Delete(pluginDirectory, true); } catch { }
        }

        private static void TestDirectPostProcessing()
        {
            string temp = Path.Combine(Path.GetTempPath(), "mv-media-direct-profile-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            string input = Path.Combine(temp, "video.mkv");
            File.WriteAllText(input, "test");
            try
            {
                MediaInfo media = new MediaInfo { Codec = "H.265 / HEVC", Width = 1920, Height = 1080, DurationSeconds = 60 };
                DirectPostProcessPlan mp4 = DirectMediaArgumentBuilder.Build(input, "mp4-h264", "720", true, false, false, media);
                True(mp4.Required, "Webshare MKV se převede podle zvoleného profilu");
                True(mp4.OutputPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase), "Webshare profil vytvoří MP4");
                True(mp4.Arguments.Contains("libx264"), "Webshare MP4 používá H.264");
                True(mp4.Arguments.Contains("scale=-2:720"), "Webshare respektuje maximální kvalitu");
                True(mp4.Arguments.Contains("0:s?") && mp4.Arguments.Contains("mov_text"), "Webshare umí převést vložené titulky");

                DirectPostProcessPlan originalMkv = DirectMediaArgumentBuilder.Build(input, "mkv-best", "auto", false, true, false, media);
                True(!originalMkv.Required && originalMkv.OutputPath == input, "MKV v původní kvalitě se zbytečně nepřevádí");

                DirectPostProcessPlan mp3 = DirectMediaArgumentBuilder.Build(input, "audio-mp3", "auto", false, false, false, media);
                True(mp3.Required && mp3.OutputPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase), "Webshare umí vyjmout MP3");
                True(mp3.Arguments.Contains("libmp3lame") && mp3.Arguments.Contains("0:a:0"), "MP3 použije první zvukovou stopu");

                string existingMp4 = Path.Combine(temp, "hotovo.mp4");
                string convertedMp4 = Path.Combine(temp, "hotovo - převedeno.mp4");
                File.WriteAllText(existingMp4, "source");
                File.WriteAllText(convertedMp4, "result");
                DirectPostProcessPlan existingResult = DirectMediaArgumentBuilder.Build(
                    existingMp4,
                    "mp4-h264",
                    "auto",
                    false,
                    true,
                    true,
                    media);
                True(existingResult.ExistingOutput && existingResult.OutputPath == convertedMp4, "Existující převod stejného typu se neduplikuje");
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }

        private static void TestDiagnosticRedaction()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string input = home + "\\video.mkv https://example.com/watch?id=123&token=abc " +
                "Authorization: Bearer tajne " + "AI" + "zaSyB02udgMkNLADkLJ_w5YNBMR2VR1WHfusI " +
                "eyJabcdefghijk.abcdefghijklmnop.qwertyuiop wst=webshare-tajne";
            string safe = DiagnosticRedactor.Redact(input);
            True(safe.IndexOf(home, StringComparison.OrdinalIgnoreCase) < 0, "report skryje uživatelskou cestu");
            True(safe.IndexOf("tajne", StringComparison.OrdinalIgnoreCase) < 0, "report skryje autorizační údaj");
            True(safe.IndexOf("AIza", StringComparison.Ordinal) < 0, "report skryje API klíč");
            True(safe.IndexOf("id=123", StringComparison.Ordinal) < 0, "report skryje parametry URL");
            True(safe.IndexOf("webshare-tajne", StringComparison.Ordinal) < 0, "report skryje Webshare relaci");
            True(DiagnosticReportService.BuildEmailUrl("Test", safe).StartsWith("mailto:?", StringComparison.Ordinal), "e-mailové hlášení otevře poštovní aplikaci");
        }

        private static void TestDownloadProviders()
        {
            DownloadRoute webshare = DownloadSourceRouter.Classify("https://webshare.cz/#/file/7Iz5S8A4nib/nazev-souboru.mkv");
            Equal(DownloadProviderKind.Webshare.ToString(), webshare.Kind.ToString(), "Webshare používá vlastní poskytovatel");
            Equal("7Iz5S8A4nib", DownloadSourceRouter.ExtractWebshareIdent(webshare.Url), "Webshare načte identifikátor z hash odkazu");

            DownloadRoute direct = DownloadSourceRouter.Classify("https://pf-storage4.premiumcdn.net/video.mp4?token=abc");
            Equal(DownloadProviderKind.Direct.ToString(), direct.Kind.ToString(), "oficiální CDN odkaz používá přímé stažení");

            DownloadRoute prehrajto = DownloadSourceRouter.Classify("https://prehraj.to/video/test-123");
            Equal(DownloadProviderKind.Unsupported.ToString(), prehrajto.Kind.ToString(), "Přehraj.to stránka se nescrapuje bez veřejného API");

            DownloadRoute youtube = DownloadSourceRouter.Classify("https://www.youtube.com/watch?v=abc");
            Equal(DownloadProviderKind.YtDlp.ToString(), youtube.Kind.ToString(), "běžné video zůstává v yt-dlp");

            string firstHash = WebsharePasswordHash.Create("heslo", "test1234");
            string secondHash = WebsharePasswordHash.Create("heslo", "test1234");
            True(firstHash.Length == 40 && firstHash == secondHash, "Webshare heslo se převádí na stabilní SHA-1 hash");
            True(firstHash != WebsharePasswordHash.Create("heslo", "jinaSul1"), "Webshare hash používá sůl účtu");

            using (WebClient client = new WebClient())
            {
                WebshareService.ConfigureApiClient(client);
                True(string.IsNullOrWhiteSpace(client.Headers[HttpRequestHeader.ContentType]), "Webshare nepřepisuje Content-Type spravovaný UploadValues");
                True(client.Headers[HttpRequestHeader.Accept] == "text/xml; charset=UTF-8", "Webshare očekává XML odpověď");
            }
        }

        private static void TestConversion()
        {
            string temp = Path.Combine(Path.GetTempPath(), "mv-media-downloader-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            string input = Path.Combine(temp, "video.mkv");
            File.WriteAllText(input, "test");
            try
            {
                ConversionOptions options = new ConversionOptions
                {
                    InputPath = input,
                    OutputDirectory = temp,
                    Format = "mp4",
                    Codec = "h264",
                    RateControl = "crf",
                    Crf = "23",
                    AudioCodec = "aac",
                    AudioBitrate = "192k"
                };
                string output;
                List<string> args = ConversionArgumentBuilder.Build(options, out output);
                True(args.Contains("libx264"), "výchozí kodek H.264");
                True(args.Contains("-crf") && args.Contains("23"), "výchozí CRF");
                True(args.Contains("+faststart"), "MP4 faststart");
                True(output.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase), "výstup MP4");

                options.Format = "mkv";
                args = ConversionArgumentBuilder.Build(options, out output);
                True(!args.Contains("+faststart"), "MKV nemá MOV přepínač");

                options.RateControl = "bitrate";
                options.VideoBitrate = "8000k";
                args = ConversionArgumentBuilder.Build(options, out output);
                True(args.Contains("-b:v") && args.Contains("8000k"), "pevný video bitrate");

                options.Format = "mkv";
                options.Codec = "h264";
                options.AudioCodec = "flac";
                args = ConversionArgumentBuilder.Build(options, out output);
                True(args.Contains("flac") && !args.Contains("-b:a"), "FLAC nepoužívá ztrátový audio bitrate");

                options.Codec = "h265";
                options.AudioCodec = "mp3";
                args = ConversionArgumentBuilder.Build(options, out output);
                True(args.Contains("libx265") && args.Contains("libmp3lame"), "H.265 s MP3 používá běžné enkodéry");

                options.Codec = "av1";
                options.AudioCodec = "opus";
                args = ConversionArgumentBuilder.Build(options, out output);
                True(args.Contains("libaom-av1") && args.Contains("libopus"), "AV1 s Opus používá běžné enkodéry");

                options.Format = "webm";
                options.AudioCodec = "aac";
                args = ConversionArgumentBuilder.Build(options, out output);
                True(args.Contains("libopus") && !args.Contains("aac"), "WebM automaticky použije kompatibilní Opus");

            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }

        private static void TestUpdateMetadata()
        {
            string hash = new string('a', 64);
            string json = "{" +
                "\"tag_name\":\"v3.1.1\"," +
                "\"html_url\":\"https://github.com/mv/MV-Media-Downloader/releases/tag/v3.1.1\"," +
                "\"draft\":false,\"prerelease\":false," +
                "\"assets\":[" +
                "{\"name\":\"MV-Media-Downloader-win-x64.zip\",\"browser_download_url\":\"https://github.com/mv/app.zip\",\"digest\":\"sha256:" + hash + "\"}," +
                "{\"name\":\"MV-Media-Downloader-win-x64.zip.sha256\",\"browser_download_url\":\"https://github.com/mv/app.sha256\"}]}";

            UpdateReleaseInfo release = UpdateMetadata.ParseRelease(json);
            Equal("3.1.1", release.Version.ToString(3), "verze aktualizace z GitHub Release");
            Equal(hash, release.Sha256, "SHA-256 z GitHub metadat");
            Equal(hash, UpdateMetadata.ParseChecksum(hash.ToUpperInvariant() + "  MV-Media-Downloader-win-x64.zip\r\n"), "SHA-256 ze souboru");
            Equal("", UpdateMetadata.ParseChecksum("neplatny soucet"), "neplatný SHA-256 se odmítne");
            True(UpdateMetadata.ParseVersion("v3.2.1-beta") > new Version(3, 2, 0), "porovnání verzí aktualizace");
        }

        private static void Equal(string expected, string actual, string name)
        {
            True(expected == actual, name + " (očekáváno: " + expected + ", získáno: " + actual + ")");
        }

        private static void True(bool value, string name)
        {
            if (value)
                Console.WriteLine("OK: " + name);
            else
            {
                Console.WriteLine("CHYBA: " + name);
                failures++;
            }
        }
    }
}
