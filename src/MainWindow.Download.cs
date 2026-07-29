using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using MVMediaStudio.Core;
using MVMediaStudio.Services;
using MVMediaStudio.UI;
using Forms = System.Windows.Forms;

namespace MVMediaStudio
{
    public partial class MainWindow
    {
        private TextBox downloadUrlBox;
        private TextBlock downloadPlaceholder;
        private TextBlock downloadUrlCount;
        private TextBlock downloadSourceSummary;
        private TextBlock downloadSourceHint;
        private ComboBox downloadFormatCombo;
        private ComboBox downloadQualityCombo;
        private ComboBox downloadCookieBrowserCombo;
        private TextBox downloadRateValueBox;
        private CheckBox downloadLimitEnabledCheck;
        private FrameworkElement downloadRateEditor;
        private TextBlock downloadRateStateText;
        private TextBox downloadFolderBox;
        private CheckBox downloadPlaylistCheck;
        private CheckBox downloadSubtitlesCheck;
        private CheckBox downloadCookiesCheck;
        private CheckBox downloadNoOverwriteCheck;
        private TextBox downloadExtraArgsBox;
        private FrameworkElement downloadAdvancedPanel;
        private Button downloadStartButton;
        private Button downloadCancelButton;
        private Button downloadApplyRateButton;
        private Button downloadReportButton;
        private Button downloadLogToggle;
        private Button downloadCopyLogButton;
        private Button webshareLoginButton;
        private Button jojLoginButton;
        private Border downloadLogCard;
        private TextBox downloadLogBox;
        private ProgressBar downloadProgress;
        private TextBlock downloadStatusTitle;
        private TextBlock downloadCurrentItem;
        private TextBlock downloadStatusDetail;
        private TextBlock downloadProgressPercent;
        private int downloadCompletedItems;
        private string downloadLiveLogLine = "";
        private readonly HashSet<string> downloadCompletedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool downloadRateRestartRequested;
        private bool downloadCanApplyRate;
        private bool downloadRateApplyPending;
        private string appliedDownloadRateLimit = "";
        private string activeDownloadEngine = "";
        private string activeDownloadItemName = "";
        private readonly DownloadRateControl directRateControl = new DownloadRateControl();
        private StackPanel downloadContent;
        private Grid downloadWorkspace;
        private Border downloadLinkCard;
        private Border downloadSettingsCard;
        private FrameworkElement downloadUrlInputHost;
        private readonly List<string> cachedDownloadUrls = new List<string>();
        private bool downloadWideLayout;

        private void InitializeDownloadView()
        {
            downloadContent = DownloadViewControl.DownloadContent;
            downloadWorkspace = DownloadViewControl.DownloadWorkspace;
            downloadLinkCard = DownloadViewControl.DownloadLinkCard;
            downloadSettingsCard = DownloadViewControl.DownloadSettingsCard;
            downloadUrlInputHost = DownloadViewControl.DownloadUrlInputHost;
            downloadUrlBox = DownloadViewControl.DownloadUrlBox;
            downloadPlaceholder = DownloadViewControl.DownloadPlaceholder;
            downloadUrlCount = DownloadViewControl.DownloadUrlCount;
            downloadSourceSummary = DownloadViewControl.DownloadSourceSummary;
            downloadSourceHint = DownloadViewControl.DownloadSourceHint;
            downloadFormatCombo = DownloadViewControl.DownloadFormatCombo;
            downloadQualityCombo = DownloadViewControl.DownloadQualityCombo;
            downloadLimitEnabledCheck = DownloadViewControl.DownloadLimitEnabledCheck;
            downloadRateEditor = DownloadViewControl.DownloadRateEditor;
            downloadRateStateText = DownloadViewControl.DownloadRateStateText;
            downloadRateValueBox = DownloadViewControl.DownloadRateValueBox;
            downloadApplyRateButton = DownloadViewControl.DownloadApplyRateButton;
            downloadFolderBox = DownloadViewControl.DownloadFolderBox;
            downloadSubtitlesCheck = DownloadViewControl.DownloadSubtitlesCheck;
            downloadPlaylistCheck = DownloadViewControl.DownloadPlaylistCheck;
            downloadNoOverwriteCheck = DownloadViewControl.DownloadNoOverwriteCheck;
            downloadCookiesCheck = DownloadViewControl.DownloadCookiesCheck;
            downloadCookieBrowserCombo = DownloadViewControl.DownloadCookieBrowserCombo;
            webshareLoginButton = DownloadViewControl.WebshareLoginButton;
            jojLoginButton = DownloadViewControl.JojLoginButton;
            downloadAdvancedPanel = DownloadViewControl.DownloadAdvancedPanel;
            downloadExtraArgsBox = DownloadViewControl.DownloadExtraArgsBox;
            downloadStartButton = DownloadViewControl.DownloadStartButton;
            downloadCancelButton = DownloadViewControl.DownloadCancelButton;
            downloadReportButton = DownloadViewControl.DownloadReportButton;
            downloadLogToggle = DownloadViewControl.DownloadLogToggle;
            downloadCopyLogButton = DownloadViewControl.DownloadCopyLogButton;
            downloadStatusTitle = DownloadViewControl.DownloadStatusTitle;
            downloadCurrentItem = DownloadViewControl.DownloadCurrentItem;
            downloadStatusDetail = DownloadViewControl.DownloadStatusDetail;
            downloadProgressPercent = DownloadViewControl.DownloadProgressPercent;
            downloadProgress = DownloadViewControl.DownloadProgress;
            downloadLogCard = DownloadViewControl.DownloadLogCard;
            downloadLogBox = DownloadViewControl.DownloadLogBox;

            ConfigurePageScroll(DownloadViewControl.PageScroll);
            PopulateCombo(
                downloadFormatCombo,
                new ComboItem("mp4-h264", "Video + zvuk · MP4 / H.264"),
                new ComboItem("mkv-best", "Video + zvuk · MKV / nejlepší"),
                new ComboItem("webm", "Video + zvuk · WebM"),
                new ComboItem("audio-m4a", "Pouze zvuk · M4A / AAC"),
                new ComboItem("audio-mp3", "Pouze zvuk · MP3"),
                new ComboItem("audio-opus", "Pouze zvuk · Opus"),
                new ComboItem("audio-flac", "Pouze zvuk · FLAC bezztrátový"),
                new ComboItem("video-only", "Pouze obraz · bez zvuku"));
            SelectCombo(downloadFormatCombo, settings.DownloadPreset);
            PopulateCombo(
                downloadQualityCombo,
                new ComboItem("auto", "Automaticky"),
                new ComboItem("2160", "Až 4K"),
                new ComboItem("1440", "Až 1440p"),
                new ComboItem("1080", "Až 1080p"),
                new ComboItem("720", "Až 720p"),
                new ComboItem("480", "Až 480p"));
            SelectCombo(downloadQualityCombo, settings.DownloadQuality);
            PopulateCombo(
                downloadCookieBrowserCombo,
                new ComboItem("chrome", "Chrome"),
                new ComboItem("edge", "Edge"),
                new ComboItem("firefox", "Firefox"),
                new ComboItem("brave", "Brave"));
            SelectCombo(downloadCookieBrowserCombo, settings.CookieBrowser);

            appliedDownloadRateLimit = settings.DownloadRateLimit ?? "";
            directRateControl.Set(appliedDownloadRateLimit);
            downloadLimitEnabledCheck.IsChecked = !string.IsNullOrWhiteSpace(settings.DownloadRateLimit);
            downloadRateValueBox.Text = RateLimitKilobytes(settings.DownloadRateLimit);
            downloadRateEditor.Visibility = downloadLimitEnabledCheck.IsChecked == true
                ? Visibility.Visible
                : Visibility.Hidden;
            downloadRateStateText.Visibility = downloadLimitEnabledCheck.IsChecked == true
                ? Visibility.Visible
                : Visibility.Hidden;
            UpdateDownloadRateState(
                string.IsNullOrWhiteSpace(appliedDownloadRateLimit)
                    ? "Potvrď rychlost tlačítkem nebo klávesou Enter."
                    : "Uloženo: " + DownloadRateLabel(appliedDownloadRateLimit),
                Theme.Muted);
            downloadFolderBox.Text = settings.DownloadDirectory;
            downloadSubtitlesCheck.IsChecked = settings.Subtitles;
            downloadPlaylistCheck.IsChecked = settings.Playlist;
            downloadNoOverwriteCheck.IsChecked = settings.NoOverwrite;
            downloadCookiesCheck.IsChecked = settings.UseBrowserCookies;
            downloadCookieBrowserCombo.Visibility = settings.UseBrowserCookies
                ? Visibility.Visible
                : Visibility.Collapsed;
            webshareLoginButton.Content = IconText(
                "\uE77B",
                WebshareService.HasSession ? "Webshare ✓" : "Přihlásit Webshare");

            downloadUrlBox.TextChanged += delegate
            {
                downloadPlaceholder.Visibility = string.IsNullOrWhiteSpace(downloadUrlBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                RefreshDownloadInputAnalysis();
                UpdateDownloadButtons();
            };
            downloadUrlBox.PreviewDragOver += delegate (object sender, DragEventArgs eventArgs)
            {
                eventArgs.Effects = eventArgs.Data.GetDataPresent(DataFormats.Text)
                    ? DragDropEffects.Copy
                    : DragDropEffects.None;
                eventArgs.Handled = true;
            };
            downloadUrlBox.Drop += delegate (object sender, DragEventArgs eventArgs)
            {
                if (eventArgs.Data.GetDataPresent(DataFormats.Text))
                    downloadUrlBox.Text = Convert.ToString(eventArgs.Data.GetData(DataFormats.Text));
            };
            DownloadViewControl.PasteButton.Click += delegate
            {
                try
                {
                    if (Clipboard.ContainsText())
                        downloadUrlBox.Text = Clipboard.GetText();
                }
                catch { }
            };
            DownloadViewControl.ClearButton.Click += delegate
            {
                downloadUrlBox.Clear();
                downloadUrlBox.Focus();
            };
            DownloadViewControl.SupportedSourcesButton.Click += delegate
            {
                new SourceSupportDialog(this).ShowDialog();
            };
            downloadRateValueBox.PreviewTextInput += delegate (object sender, TextCompositionEventArgs eventArgs)
            {
                eventArgs.Handled = !Regex.IsMatch(eventArgs.Text, "^[0-9]+$");
            };
            downloadRateValueBox.TextChanged += delegate { UpdateDownloadButtons(); };
            downloadRateValueBox.PreviewKeyDown += delegate (object sender, KeyEventArgs eventArgs)
            {
                if (eventArgs.Key != Key.Enter)
                    return;
                ApplyDownloadRateNow();
                eventArgs.Handled = true;
            };
            downloadApplyRateButton.Click += delegate { ApplyDownloadRateNow(); };
            downloadLimitEnabledCheck.Checked += delegate
            {
                downloadRateEditor.Visibility = Visibility.Visible;
                downloadRateStateText.Visibility = Visibility.Visible;
                UpdateDownloadRateState(
                    string.IsNullOrWhiteSpace(appliedDownloadRateLimit)
                        ? "Potvrď rychlost tlačítkem nebo klávesou Enter."
                        : "Uloženo: " + DownloadRateLabel(appliedDownloadRateLimit),
                    Theme.Muted);
                downloadRateValueBox.Focus();
                downloadRateValueBox.SelectAll();
                UpdateDownloadButtons();
            };
            downloadLimitEnabledCheck.Unchecked += delegate
            {
                downloadRateEditor.Visibility = Visibility.Hidden;
                downloadRateStateText.Visibility = Visibility.Hidden;
                if (downloadStatusTitle != null)
                    ApplyDownloadRateNow();
                UpdateDownloadButtons();
            };
            DownloadViewControl.BrowseDownloadFolderButton.Click += delegate { BrowseDownloadFolder(); };
            DownloadViewControl.OpenDownloadFolderButton.Click += delegate
            {
                OpenDirectory(downloadFolderBox.Text);
            };
            downloadCookiesCheck.Checked += delegate
            {
                downloadCookieBrowserCombo.Visibility = Visibility.Visible;
            };
            downloadCookiesCheck.Unchecked += delegate
            {
                downloadCookieBrowserCombo.Visibility = Visibility.Collapsed;
            };
            webshareLoginButton.Click += async delegate { await OpenWebshareLoginAsync(); };
            jojLoginButton.Click += delegate { OpenJojPlayLogin(); };
            downloadStartButton.Click += async delegate { await StartDownloadAsync(); };
            downloadCancelButton.Click += delegate { CancelActiveWork(); };
            downloadReportButton.Click += delegate
            {
                SaveProblemReport("Stahování", downloadLog.ToString());
            };
            downloadLogToggle.Click += delegate { ToggleDownloadLog(); };
            downloadCopyLogButton.Click += delegate { CopyDownloadLog(); };

            ArrangeDownloadWorkspace(false);
            RefreshDownloadInputAnalysis();
            UpdateDownloadButtons();
        }


        private async Task StartDownloadAsync()
        {
            if (busy)
                return;

            List<string> inputUrls = ValidDownloadUrls();
            if (inputUrls.Count == 0)
            {
                SetDownloadStatus("Chybí platný odkaz", "Vlož adresu začínající http:// nebo https://.", Theme.Danger);
                return;
            }
            string selectedRateLimit;
            if (!TryGetDownloadRateLimit(out selectedRateLimit))
            {
                SetDownloadStatus("Neplatný limit rychlosti", "Zadej kladné celé číslo v KB/s, například 3000.", Theme.Danger);
                return;
            }
            List<string> ytDlpUrls = new List<string>();
            List<DownloadRoute> directRoutes = new List<DownloadRoute>();
            List<DownloadRoute> unsupportedRoutes = new List<DownloadRoute>();
            foreach (string url in inputUrls)
            {
                DownloadRoute route = DownloadSourceRouter.Classify(url);
                if (route.Kind == DownloadProviderKind.YtDlp)
                    ytDlpUrls.Add(url);
                else if (route.Kind == DownloadProviderKind.Unsupported)
                    unsupportedRoutes.Add(route);
                else
                    directRoutes.Add(route);
            }

            bool hasJojPlay = ytDlpUrls.Exists(IsJojPlayUrl);
            if (hasJojPlay && !JojLoginService.IsReady && !OpenJojPlayLogin())
                return;
            if (hasJojPlay)
                downloadCookiesCheck.IsChecked = true;
            if (ytDlpUrls.Count > 0 && !await EnsureYtDlpAsync())
                return;
            if (directRoutes.Exists(DirectRouteNeedsFfmpeg) && !await EnsureFfmpegAsync())
                return;

            CaptureDownloadSettings();
            settings.DownloadRateLimit = selectedRateLimit;
            settings.Save();
            directRateControl.Set(selectedRateLimit);
            appliedDownloadRateLimit = selectedRateLimit;

            DownloadOptions options = new DownloadOptions
            {
                Preset = settings.DownloadPreset,
                Quality = settings.DownloadQuality,
                RateLimit = settings.DownloadRateLimit,
                OutputDirectory = settings.DownloadDirectory,
                Playlist = downloadPlaylistCheck.IsChecked == true,
                Subtitles = downloadSubtitlesCheck.IsChecked == true,
                CookiesFromBrowser = downloadCookiesCheck.IsChecked == true,
                CookieBrowserSpec = ComboValue(downloadCookieBrowserCombo, "chrome"),
                NoOverwrite = downloadNoOverwriteCheck.IsChecked == true,
                ExtraArguments = downloadExtraArgsBox.Text
            };

            downloadLog.Clear();
            downloadLiveLogLine = "";
            downloadLogBox.Clear();
            downloadCompletedItems = 0;
            downloadCompletedPaths.Clear();
            downloadProgress.Value = 0;
            downloadProgressPercent.Text = "0 %";
            SetActiveDownloadItem("");
            downloadReportButton.Visibility = Visibility.Collapsed;
            downloadRateRestartRequested = false;
            downloadCanApplyRate = false;
            downloadRateApplyPending = false;
            BeginCancellableOperation("download");
            SetBusy(true, "Kontroluji odkazy");
            SetTaskbarProgress(0, TaskbarItemProgressState.Indeterminate);
            SetDownloadStatus("Kontroluji odkazy", "Ověřuji zdroj a dostupnost veřejného videa…", Theme.Primary);
            int exitCode = unsupportedRoutes.Count > 0 ? 1 : 0;
            string failureTitle = "";
            string failureDetail = "";
            Stopwatch elapsed = Stopwatch.StartNew();
            try
            {
                if (ytDlpUrls.Count > 0)
                {
                    try
                    {
                        DownloadUrlResolution resolution = await JojUrlResolver.ResolveAsync(
                            ytDlpUrls,
                            operationCancellation.Token);
                        ytDlpUrls = resolution.Urls;
                        foreach (string note in resolution.Notes)
                            AppendDownloadLog(note);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception error)
                    {
                        failureTitle = "Odkaz JOJ nelze stáhnout";
                        failureDetail = error.Message;
                        throw;
                    }
                }

                StorageService.EnsureWritableDirectory(settings.DownloadDirectory);
                SetBusy(true, "Probíhá stahování");
                SetDownloadStatus(
                    "Připravuji stahování",
                    inputUrls.Count == 1 ? "Zpracovávám odkaz…" : "Zpracovávám " + inputUrls.Count + " odkazů…",
                    Theme.Primary);

                foreach (DownloadRoute route in unsupportedRoutes)
                    AppendDownloadLog("! [" + route.Provider + "] " + route.Message);

                if (directRoutes.Count > 0)
                {
                    activeDownloadEngine = "direct";
                    downloadCanApplyRate = true;
                    foreach (DownloadRoute route in directRoutes)
                    {
                        operationCancellation.Token.ThrowIfCancellationRequested();
                        DirectDownloadItem item = null;
                        string downloadedPath = "";
                        bool sourceSkipped = false;
                        try
                        {
                            if (route.Kind == DownloadProviderKind.Webshare)
                                item = await WebshareService.ResolveAsync(route.Url, activeCancellation.Token);
                            else
                                item = new DirectDownloadItem
                                {
                                    Provider = route.Provider,
                                    SourceUrl = route.Url,
                                    DownloadUrl = route.Url,
                                    FileName = DownloadSourceRouter.FileNameFromUrl(route.Url)
                                };
                            SetActiveDownloadItem(DownloadOutputParser.DisplayNameFromPath(item.FileName));
                            AppendDownloadLog("[" + item.Provider + "] " + item.FileName);
                            downloadedPath = await DirectDownloadService.DownloadAsync(
                                item,
                                settings.DownloadDirectory,
                                options.NoOverwrite,
                                directRateControl.ReadBytesPerSecond,
                                delegate (DirectDownloadProgress progress)
                                {
                                    if (progress.Completed)
                                    {
                                        sourceSkipped = progress.Skipped;
                                        return;
                                    }
                                    HandleDirectDownloadProgress(progress);
                                },
                                activeCancellation.Token);
                            downloadCanApplyRate = false;
                            CommitDownloadLiveLog();
                            downloadProgress.Value = 0;
                            downloadProgressPercent.Text = "0 %";
                            SetTaskbarProgress(0, TaskbarItemProgressState.Indeterminate);
                            SetDownloadStatus("Připravuji výsledek", item.Provider + " · " + item.FileName, Theme.Primary);
                            DirectPostProcessResult processed = await DirectMediaPostProcessService.ProcessAsync(
                                tools.FfmpegPath,
                                tools.FfprobePath,
                                downloadedPath,
                                options.Preset,
                                options.Quality,
                                options.Subtitles,
                                options.NoOverwrite,
                                sourceSkipped,
                                delegate (DirectPostProcessProgress progress) { HandleDirectPostProcessProgress(item, progress); },
                                activeCancellation.Token);
                            MarkDirectDownloadCompleted(item, processed);
                        }
                        catch (OperationCanceledException)
                        {
                            if (!string.IsNullOrWhiteSpace(downloadedPath) && File.Exists(downloadedPath))
                                AppendDownloadLog("[Zachováno po zrušení] " + downloadedPath);
                            throw;
                        }
                        catch (Exception error)
                        {
                            exitCode = 1;
                            AppPaths.WriteError(error);
                            AppendDownloadLog("! [" + route.Provider + "] " + error.Message);
                            if (item != null && !string.IsNullOrWhiteSpace(downloadedPath) && File.Exists(downloadedPath))
                                MarkDirectDownloadRetained(item, downloadedPath);
                        }
                    }
                    downloadCanApplyRate = false;
                }

                if (ytDlpUrls.Count > 0)
                {
                    activeDownloadEngine = "ytdlp";
                    List<string> regularUrls = ytDlpUrls.FindAll(delegate (string value) { return !IsJojPlayUrl(value); });
                    List<string> jojPlayUrls = ytDlpUrls.FindAll(IsJojPlayUrl);
                    if (regularUrls.Count > 0)
                    {
                        int ytDlpExit = await RunYtDlpDownloadAsync(options, regularUrls);
                        if (ytDlpExit == -2)
                            throw new OperationCanceledException(operationCancellation.Token);
                        if (ytDlpExit != 0)
                            exitCode = ytDlpExit;
                    }
                    if (jojPlayUrls.Count > 0)
                    {
                        DownloadOptions jojOptions = CopyDownloadOptions(options);
                        jojOptions.CookiesFromBrowser = true;
                        jojOptions.CookieBrowserSpec = "chrome:" + JojLoginService.ProfileDirectory;
                        int jojExit = await RunYtDlpDownloadAsync(jojOptions, jojPlayUrls);
                        if (jojExit == -2)
                            throw new OperationCanceledException(operationCancellation.Token);
                        if (jojExit != 0)
                            exitCode = jojExit;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                exitCode = -2;
            }
            catch (Exception error)
            {
                exitCode = 1;
                AppPaths.WriteError(error);
                AppendDownloadLog("! " + error.Message);
                if (string.IsNullOrWhiteSpace(failureTitle))
                    failureTitle = "Stažení se nepovedlo";
                if (string.IsNullOrWhiteSpace(failureDetail))
                    failureDetail = error.Message;
            }
            finally
            {
                try
                {
                    await Dispatcher.InvokeAsync(
                        delegate { },
                        System.Windows.Threading.DispatcherPriority.Background);
                }
                catch
                {
                }

                elapsed.Stop();
                CommitDownloadLiveLog();
                activeDownloadEngine = "";
                SetActiveDownloadItem("");
                downloadCanApplyRate = false;
                downloadRateRestartRequested = false;
                downloadRateApplyPending = false;
                EndCancellableOperation();

                if (exitCode == 0)
                {
                    downloadProgress.Value = 100;
                    downloadProgressPercent.Text = "100 %";
                    SetBusy(false, "Stahování dokončeno");
                    string completed = downloadCompletedItems > 0
                        ? downloadCompletedItems + " souborů je připraveno"
                        : "Soubory jsou připravené";
                    SetDownloadStatus("Stažení dokončeno", completed + " · " + FormatElapsed(elapsed.Elapsed) + ".", Theme.Success);
                    SetTaskbarProgress(0, TaskbarItemProgressState.None);
                }
                else if (exitCode == -2)
                {
                    SetBusy(false, "Stahování zrušeno");
                    SetDownloadStatus("Stahování zrušeno", "Přenos byl zastaven a rozpracovaná data zůstala zachovaná.", Theme.Warning);
                    SetTaskbarProgress(downloadProgress.Value, TaskbarItemProgressState.Paused);
                }
                else if (downloadCompletedItems > 0)
                {
                    SetBusy(false, "Část souborů byla stažena");
                    SetDownloadStatus(
                        "Dokončeno s upozorněním",
                        downloadCompletedItems + " souborů je připraveno · " + FormatElapsed(elapsed.Elapsed) + ". Podrobnosti jsou v logu.",
                        Theme.Warning);
                    SetTaskbarProgress(downloadProgress.Value, TaskbarItemProgressState.Paused);
                    ShowDownloadLog();
                    downloadReportButton.Visibility = Visibility.Visible;
                }
                else
                {
                    SetBusy(false, "Chyba při stahování");
                    SetDownloadStatus(
                        string.IsNullOrWhiteSpace(failureTitle) ? "Stažení se nepovedlo" : failureTitle,
                        string.IsNullOrWhiteSpace(failureDetail) ? "Podrobnosti jsou v technickém logu." : failureDetail,
                        Theme.Danger);
                    SetTaskbarProgress(Math.Max(1, downloadProgress.Value), TaskbarItemProgressState.Error);
                    ShowDownloadLog();
                    downloadReportButton.Visibility = Visibility.Visible;
                }

                if (downloadLimitEnabledCheck.IsChecked == true)
                {
                    UpdateDownloadRateState(
                        string.IsNullOrWhiteSpace(settings.DownloadRateLimit)
                            ? "Limit je vypnutý."
                            : "Uloženo: " + DownloadRateLabel(settings.DownloadRateLimit),
                        Theme.Muted);
                }
                SaveLog(AppPaths.DownloadLogPath, downloadLog.ToString());
            }
        }

        private async Task<int> RunYtDlpDownloadAsync(DownloadOptions options, List<string> urls)
        {
            int exitCode = -1;
            bool firstRun = true;
            while (true)
            {
                if (operationCancellation != null && operationCancellation.IsCancellationRequested)
                    return -2;
                options.RateLimit = settings.DownloadRateLimit;
                if (!firstRun)
                    options.NoOverwrite = true;
                List<string> arguments;
                try
                {
                    arguments = DownloadArgumentBuilder.Build(options, urls, tools);
                }
                catch (Exception error)
                {
                    exitCode = -1;
                    AppendDownloadLog(error.Message);
                    break;
                }

                RenewActiveCancellation();
                appliedDownloadRateLimit = options.RateLimit ?? "";
                downloadRateRestartRequested = false;
                UpdateDownloadRateState(
                    string.IsNullOrWhiteSpace(appliedDownloadRateLimit)
                        ? "Aktivní: bez omezení"
                        : "Aktivní: " + DownloadRateLabel(appliedDownloadRateLimit),
                    Theme.Success);
                if (firstRun)
                    AppendDownloadLog("$ yt-dlp " + ArgumentUtilities.Join(arguments));
                try
                {
                    exitCode = await ProcessService.RunAsync(tools.YtDlpPath, arguments, HandleDownloadLine, activeCancellation.Token);
                }
                catch (Exception error)
                {
                    AppPaths.WriteError(error);
                    AppendDownloadLog(error.ToString());
                }

                if (exitCode == -2 &&
                    downloadRateRestartRequested &&
                    operationCancellation != null &&
                    !operationCancellation.IsCancellationRequested)
                {
                    downloadLiveLogLine = "";
                    RefreshDownloadLogBox();
                    downloadCanApplyRate = false;
                    firstRun = false;
                    SetDownloadStatus("Navazuji stahování", "Používám nový limit rychlosti…", Theme.Primary);
                    continue;
                }
                break;
            }
            return exitCode;
        }

        private static bool IsJojPlayUrl(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                string.Equals(uri.Host, "play.joj.sk", StringComparison.OrdinalIgnoreCase);
        }

        private static bool DirectRouteNeedsFfmpeg(DownloadRoute route)
        {
            if (route == null)
                return false;
            if (route.Kind == DownloadProviderKind.Webshare)
                return true;
            Uri uri;
            return Uri.TryCreate(route.Url, UriKind.Absolute, out uri) &&
                DirectMediaArgumentBuilder.IsMediaPath(uri.AbsolutePath);
        }

        private static DownloadOptions CopyDownloadOptions(DownloadOptions source)
        {
            return new DownloadOptions
            {
                Preset = source.Preset,
                Quality = source.Quality,
                RateLimit = source.RateLimit,
                OutputDirectory = source.OutputDirectory,
                Playlist = source.Playlist,
                Subtitles = source.Subtitles,
                CookiesFromBrowser = source.CookiesFromBrowser,
                CookieBrowserSpec = source.CookieBrowserSpec,
                NoOverwrite = source.NoOverwrite,
                ExtraArguments = source.ExtraArguments
            };
        }

        private void HandleDirectDownloadProgress(DirectDownloadProgress progress)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (activeCancellation != null && activeCancellation.IsCancellationRequested)
                    return;
                if (progress.Completed)
                {
                    if (downloadCompletedPaths.Add(progress.OutputPath))
                        downloadCompletedItems++;
                    downloadProgress.Value = 100;
                    downloadProgressPercent.Text = "100 %";
                    AppendDownloadLog(progress.Skipped ? "[Přeskočeno] " + progress.OutputPath : "[Hotovo] " + progress.OutputPath);
                    SetDownloadStatus(progress.Skipped ? "Soubor už existuje" : "Soubor dokončen", progress.Provider + " · " + progress.FileName, Theme.Success);
                    return;
                }

                downloadCanApplyRate = true;
                double percentage = progress.Percentage;
                downloadProgress.Value = percentage;
                downloadProgressPercent.Text = progress.TotalBytes > 0 ? percentage.ToString("0.#") + " %" : "…";
                SetTaskbarProgress(percentage, progress.TotalBytes > 0
                    ? TaskbarItemProgressState.Normal
                    : TaskbarItemProgressState.Indeterminate);
                string detail = progress.Provider + " · " + FormatTransferSpeed(progress.BytesPerSecond);
                if (!string.IsNullOrWhiteSpace(settings.DownloadRateLimit))
                    detail += " · limit " + DownloadRateLabel(settings.DownloadRateLimit);
                if (progress.TotalBytes > 0 && progress.BytesPerSecond > 0)
                {
                    double remaining = (progress.TotalBytes - progress.BytesReceived) / progress.BytesPerSecond;
                    detail += " · zbývá " + FormatDuration(remaining);
                }
                SetDownloadStatus("Stahování", detail, Theme.Primary);
                SetDownloadLiveLog("[" + progress.Provider + "] " +
                    (progress.TotalBytes > 0 ? percentage.ToString("0.#") + "% " : "") +
                    FormatByteSize(progress.BytesReceived) + " · " + FormatTransferSpeed(progress.BytesPerSecond));
            }));
        }

        private void HandleDirectPostProcessProgress(DirectDownloadItem item, DirectPostProcessProgress progress)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (activeCancellation != null && activeCancellation.IsCancellationRequested)
                    return;
                downloadCanApplyRate = false;
                double percentage = Math.Max(0, Math.Min(100, progress.Percentage));
                downloadProgress.Value = percentage;
                downloadProgressPercent.Text = percentage.ToString("0.#") + " %";
                SetTaskbarProgress(percentage, TaskbarItemProgressState.Normal);
                SetDownloadStatus(
                    "Převádím stažený soubor",
                    item.Provider + " · " + progress.ProfileLabel + " · " + percentage.ToString("0.#") + " %",
                    Theme.Primary);
                SetDownloadLiveLog(
                    "[FFmpeg] " + item.FileName + " · " + progress.ProfileLabel + " · " + percentage.ToString("0.#") + " %");
            }));
        }

        private void MarkDirectDownloadCompleted(DirectDownloadItem item, DirectPostProcessResult result)
        {
            CommitDownloadLiveLog();
            if (downloadCompletedPaths.Add(result.OutputPath))
                downloadCompletedItems++;
            downloadProgress.Value = 100;
            downloadProgressPercent.Text = "100 %";
            SetTaskbarProgress(100, TaskbarItemProgressState.Normal);
            if (result.Skipped)
                AppendDownloadLog("[Přeskočeno] " + result.OutputPath);
            else if (result.Processed)
                AppendDownloadLog("[Převedeno] " + result.OutputPath);
            else
                AppendDownloadLog("[Hotovo] " + result.OutputPath);
            SetDownloadStatus(
                result.Skipped ? "Výsledek už existuje" : result.Processed ? "Soubor převeden" : "Soubor dokončen",
                item.Provider + " · " + result.ProfileLabel,
                result.Skipped ? Theme.Warning : Theme.Success);
        }

        private void MarkDirectDownloadRetained(DirectDownloadItem item, string path)
        {
            CommitDownloadLiveLog();
            if (downloadCompletedPaths.Add(path))
                downloadCompletedItems++;
            downloadProgress.Value = 100;
            downloadProgressPercent.Text = "100 %";
            AppendDownloadLog("[Originál zachován] " + path);
            SetDownloadStatus("Převod se nepovedl", item.Provider + " · původní soubor zůstal zachovaný", Theme.Warning);
        }

        private static string FormatTransferSpeed(double bytesPerSecond)
        {
            return bytesPerSecond <= 0 ? "zjišťuji rychlost" : FormatByteSize((long)bytesPerSecond) + "/s";
        }

        private static string FormatByteSize(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
                return (bytes / (1024d * 1024 * 1024)).ToString("0.00") + " GiB";
            if (bytes >= 1024L * 1024)
                return (bytes / (1024d * 1024)).ToString("0.0") + " MiB";
            return (bytes / 1024d).ToString("0") + " KiB";
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds < 0 || double.IsInfinity(seconds) || double.IsNaN(seconds))
                return "—";
            TimeSpan value = TimeSpan.FromSeconds(Math.Min(seconds, 359999));
            return value.TotalHours >= 1 ? value.ToString(@"h\:mm\:ss") : value.ToString(@"m\:ss");
        }

        private void HandleDownloadLine(string line, bool isError)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (activeCancellation != null && activeCancellation.IsCancellationRequested)
                    return;
                string itemName;
                bool explicitItem = line.StartsWith(DownloadOutputParser.CurrentItemPrefix, StringComparison.Ordinal);
                if (DownloadOutputParser.TryReadCurrentItem(line, out itemName) &&
                    (explicitItem || string.IsNullOrWhiteSpace(activeDownloadItemName)))
                {
                    if (SetActiveDownloadItem(itemName))
                        AppendDownloadLog("[Aktuálně] " + itemName);
                    SetDownloadStatus("Připravuji soubor", "Načítám dostupné datové proudy.", Theme.Primary);
                    UpdateDownloadButtons();
                    return;
                }
                if (line.StartsWith(DownloadOutputParser.CompletedPathPrefix, StringComparison.Ordinal))
                {
                    string completedPath = line.Substring(DownloadOutputParser.CompletedPathPrefix.Length);
                    if (downloadCompletedPaths.Add(completedPath))
                        downloadCompletedItems++;
                    downloadCanApplyRate = false;
                    downloadProgress.Value = 100;
                    downloadProgressPercent.Text = "100 %";
                    SetTaskbarProgress(100, TaskbarItemProgressState.Normal);
                    AppendDownloadLog("[Hotovo] " + completedPath);
                    SetDownloadStatus("Soubor dokončen", "Hotovo " + downloadCompletedItems + ". Pokračuji další položkou.", Theme.Success);
                    return;
                }

                bool isDownloadLine = line.IndexOf("[download]", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isDownloadLine)
                {
                    downloadCanApplyRate = true;
                    if (downloadRateApplyPending)
                    {
                        downloadRateApplyPending = false;
                        ApplyDownloadRateNow();
                        return;
                    }
                }

                Match percent = Regex.Match(line, "(?<![0-9])([0-9]{1,3}(?:\\.[0-9]+)?)%");
                double value = 0;
                bool isProgress = isDownloadLine &&
                    percent.Success &&
                    double.TryParse(percent.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
                if (isProgress)
                {
                    value = Math.Max(0, Math.Min(100, value));
                    downloadProgress.Value = value;
                    downloadProgressPercent.Text = value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " %";
                    SetTaskbarProgress(value, TaskbarItemProgressState.Normal);

                    Match eta = Regex.Match(line, "ETA\\s+([^\\s]+)", RegexOptions.IgnoreCase);
                    Match speed = Regex.Match(line, "at\\s+([^\\s]+/s)", RegexOptions.IgnoreCase);
                    Match fragment = Regex.Match(line, "(?:frag|fragment)\\s+(\\d+)\\s*/\\s*(\\d+)", RegexOptions.IgnoreCase);
                    string detail = speed.Success ? speed.Groups[1].Value : "Stahuji data";
                    if (eta.Success) detail += " · zbývá " + eta.Groups[1].Value;
                    if (fragment.Success) detail += " · fragment " + fragment.Groups[1].Value + " / " + fragment.Groups[2].Value;
                    if (!string.IsNullOrWhiteSpace(settings.DownloadRateLimit))
                        detail += " · limit " + DownloadRateLabel(settings.DownloadRateLimit);
                    SetDownloadStatus("Stahování", detail, Theme.Primary);
                    SetDownloadLiveLog(line);
                    UpdateDownloadButtons();
                    return;
                }

                AppendDownloadLog((isError ? "! " : "") + line);
                if (isDownloadLine)
                    SetDownloadStatus("Připravuji soubor", "Zjišťuji velikost a dostupné datové proudy.", Theme.Primary);
                else if (line.IndexOf("[Merger]", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    downloadCanApplyRate = false;
                    SetDownloadStatus("Dokončuji soubor", "Spojuji obraz a zvuk do výsledného formátu.", Theme.Primary);
                }
                else if (line.IndexOf("[ExtractAudio]", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    downloadCanApplyRate = false;
                    SetDownloadStatus("Zpracovávám zvuk", "Připravuji výsledný zvukový soubor.", Theme.Primary);
                }
                UpdateDownloadButtons();
            }));
        }

        private void AppendDownloadLog(string line)
        {
            downloadLiveLogLine = "";
            downloadLog.AppendLine(line);
            if (downloadLog.Length > 180000)
                downloadLog.Remove(0, downloadLog.Length - 150000);
            RefreshDownloadLogBox();
        }

        private void SetDownloadLiveLog(string line)
        {
            downloadLiveLogLine = line ?? "";
            RefreshDownloadLogBox();
        }

        private void CommitDownloadLiveLog()
        {
            if (string.IsNullOrWhiteSpace(downloadLiveLogLine))
                return;
            string line = downloadLiveLogLine;
            downloadLiveLogLine = "";
            AppendDownloadLog(line);
        }

        private void RefreshDownloadLogBox()
        {
            double offset = downloadLogBox.VerticalOffset;
            bool followEnd = downloadLogBox.ExtentHeight <= offset + downloadLogBox.ViewportHeight + 2;
            downloadLogBox.Text = downloadLog.ToString() + downloadLiveLogLine;
            if (followEnd)
                downloadLogBox.ScrollToEnd();
            else
                downloadLogBox.ScrollToVerticalOffset(offset);
        }

        private void ToggleDownloadLog()
        {
            if (downloadLogCard.Visibility == Visibility.Visible)
            {
                downloadLogCard.Visibility = Visibility.Collapsed;
                downloadLogToggle.Content = IconText("\uE756", "Zobrazit log");
            }
            else
            {
                ShowDownloadLog();
            }
        }

        private void ShowDownloadLog()
        {
            downloadLogCard.Visibility = Visibility.Visible;
            downloadLogToggle.Content = IconText("\uE70D", "Skrýt log");
        }

        private void CopyDownloadLog()
        {
            string text = downloadLog.ToString() + downloadLiveLogLine;
            if (string.IsNullOrWhiteSpace(text))
            {
                footerStatus.Text = "Log je zatím prázdný";
                return;
            }
            try
            {
                Clipboard.SetText(text);
                footerStatus.Text = "Technický log byl zkopírován";
            }
            catch
            {
                footerStatus.Text = "Log se nepodařilo zkopírovat";
            }
        }

        private void UpdateDownloadUrlCount()
        {
            int count = cachedDownloadUrls.Count;
            downloadUrlCount.Text = count == 1 ? "1 odkaz" : count + " odkazů";
            if (count > 0)
            {
                downloadStatusTitle.Text = "Připraveno ke stažení";
                downloadStatusDetail.Text = count == 1 ? "Odkaz je připravený." : count + " odkazů je připravených.";
            }
            else if (!busy)
            {
                downloadStatusTitle.Text = "Připraveno ke stažení";
                downloadStatusDetail.Text = "Čekám na odkaz.";
            }
        }

        private List<string> ValidDownloadUrls()
        {
            return new List<string>(cachedDownloadUrls);
        }

        private void RefreshDownloadInputAnalysis()
        {
            cachedDownloadUrls.Clear();
            if (downloadUrlBox != null)
                cachedDownloadUrls.AddRange(DownloadUrlParser.Parse(downloadUrlBox.Text));
            UpdateDownloadUrlCount();

            if (downloadSourceSummary == null)
                return;
            if (cachedDownloadUrls.Count == 0)
            {
                downloadSourceSummary.Text = "Zdroj rozpoznám automaticky.";
                Theme.Bind(downloadSourceSummary, TextBlock.ForegroundProperty, Theme.Muted);
                downloadSourceHint.Text = "";
                downloadSourceHint.ToolTip = null;
                downloadSourceHint.Visibility = Visibility.Collapsed;
                return;
            }

            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<string> order = new List<string>();
            List<string> guidance = new List<string>();
            bool unsupported = false;
            foreach (string url in cachedDownloadUrls)
            {
                DownloadRoute route = DownloadSourceRouter.Classify(url);
                string label = route.Provider;
                if (route.Kind == DownloadProviderKind.Unsupported)
                {
                    label += " · nepodporováno";
                    unsupported = true;
                }
                if (!counts.ContainsKey(label))
                {
                    counts[label] = 0;
                    order.Add(label);
                }
                counts[label]++;
                if (!string.IsNullOrWhiteSpace(route.Message) && !guidance.Contains(route.Message))
                    guidance.Add(route.Message);
            }

            List<string> summary = new List<string>();
            int visible = Math.Min(4, order.Count);
            for (int index = 0; index < visible; index++)
            {
                string label = order[index];
                summary.Add(counts[label] > 1 ? label + " " + counts[label] + "×" : label);
            }
            if (order.Count > visible)
                summary.Add("+" + (order.Count - visible) + " další");
            downloadSourceSummary.Text = string.Join("  ·  ", summary.ToArray());
            Theme.Bind(downloadSourceSummary, TextBlock.ForegroundProperty, unsupported ? Theme.Warning : Theme.Success);
            if (guidance.Count == 0)
            {
                downloadSourceHint.Text = "";
                downloadSourceHint.ToolTip = null;
                downloadSourceHint.Visibility = Visibility.Collapsed;
            }
            else
            {
                int shown = Math.Min(2, guidance.Count);
                downloadSourceHint.Text = string.Join("  •  ", guidance.GetRange(0, shown).ToArray()) +
                    (guidance.Count > shown ? "  +" + (guidance.Count - shown) + " další upozornění" : "");
                downloadSourceHint.ToolTip = string.Join(Environment.NewLine, guidance.ToArray());
                downloadSourceHint.Visibility = Visibility.Visible;
            }
        }

        private void SetDownloadStatus(string title, string detail, string colorKey)
        {
            downloadStatusTitle.Text = title;
            Theme.Bind(downloadStatusTitle, TextBlock.ForegroundProperty, colorKey);
            downloadStatusDetail.Text = detail;
        }

        private bool SetActiveDownloadItem(string value)
        {
            string itemName = (value ?? "").Trim();
            bool changed = !string.Equals(activeDownloadItemName, itemName, StringComparison.Ordinal);
            activeDownloadItemName = itemName;
            if (downloadCurrentItem == null)
                return changed;

            if (itemName.Length == 0)
            {
                downloadCurrentItem.Text = "";
                downloadCurrentItem.ToolTip = null;
                downloadCurrentItem.Visibility = Visibility.Collapsed;
                Title = "MV Media Downloader";
            }
            else
            {
                downloadCurrentItem.Text = "Aktuálně: " + itemName;
                downloadCurrentItem.ToolTip = itemName;
                downloadCurrentItem.Visibility = Visibility.Visible;
                Title = itemName + " · MV Media Downloader";
            }
            return changed;
        }

        private void BrowseDownloadFolder()
        {
            using (Forms.FolderBrowserDialog dialog = new Forms.FolderBrowserDialog())
            {
                dialog.Description = "Vyber cílovou složku pro stažené soubory";
                dialog.SelectedPath = downloadFolderBox.Text;
                if (dialog.ShowDialog() == Forms.DialogResult.OK)
                    downloadFolderBox.Text = dialog.SelectedPath;
            }
        }

        private void CaptureDownloadSettings()
        {
            if (downloadFormatCombo == null)
                return;
            settings.DownloadPreset = ComboValue(downloadFormatCombo, "mp4-h264");
            settings.DownloadQuality = ComboValue(downloadQualityCombo, "1080");
            settings.DownloadDirectory = string.IsNullOrWhiteSpace(downloadFolderBox.Text) ? AppPaths.DefaultDownloadDirectory : downloadFolderBox.Text;
            settings.UseBrowserCookies = downloadCookiesCheck.IsChecked == true;
            settings.CookieBrowser = ComboValue(downloadCookieBrowserCombo, "chrome");
            settings.NoOverwrite = downloadNoOverwriteCheck.IsChecked == true;
            settings.Subtitles = downloadSubtitlesCheck.IsChecked == true;
            settings.Playlist = downloadPlaylistCheck.IsChecked == true;
        }

        private bool OpenJojPlayLogin()
        {
            if (!JojLoginService.OpenLogin())
            {
                SetDownloadStatus("Chrome nebyl nalezen", "Nainstaluj Google Chrome a zkus přihlášení JOJ Play znovu.", Theme.Danger);
                return false;
            }

            MessageBoxResult result = MessageBox.Show(
                "V otevřeném odděleném okně Chrome se přihlas na JOJ Play. Heslo nenechávej uložit. Potom toto okno Chrome úplně zavři a klikni na Pokračovat.\n\nAplikace odstraní databázi uložených hesel; zachová pouze lokální přihlašovací relaci potřebnou pro stahování.",
                "Přihlášení JOJ Play",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information,
                MessageBoxResult.OK);
            if (result != MessageBoxResult.OK)
                return false;

            if (!JojLoginService.MarkReady())
            {
                SetDownloadStatus("Chrome je stále otevřený", "Úplně zavři oddělené okno Chrome a přihlášení spusť znovu. Heslo nebylo přijato jako uložené.", Theme.Danger);
                return false;
            }
            downloadCookiesCheck.IsChecked = true;
            SetDownloadStatus("JOJ Play je připraven", "Přihlášení se ověří při zahájení stahování.", Theme.Success);
            return true;
        }

        private async Task OpenWebshareLoginAsync()
        {
            if (WebshareService.HasSession)
            {
                MessageBoxResult logout = MessageBox.Show(
                    this,
                    "Webshare relace je aktivní. Chceš ji z tohoto počítače odstranit?",
                    "Webshare",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);
                if (logout == MessageBoxResult.Yes)
                {
                    WebshareService.Logout();
                    webshareLoginButton.Content = IconText("\uE77B", "Přihlásit Webshare");
                    SetDownloadStatus("Webshare odhlášeno", "Uložená relace byla odstraněna.", Theme.Success);
                }
                return;
            }

            WebshareLoginDialog dialog = new WebshareLoginDialog(this, settings.WebshareUserName);
            if (dialog.ShowDialog() != true)
                return;

            string userName = dialog.UserName;
            string password = dialog.Password;
            webshareLoginButton.IsEnabled = false;
            SetDownloadStatus("Přihlašuji Webshare", "Ověřuji účet přes oficiální API…", Theme.Primary);
            try
            {
                WebshareLoginResult result = await WebshareService.LoginAsync(userName, password, dialog.Remember);
                settings.WebshareUserName = result.UserName;
                settings.Save();
                webshareLoginButton.Content = IconText("\uE77B", "Webshare ✓");
                SetDownloadStatus("Webshare je připravené", "Relace účtu byla úspěšně ověřena.", Theme.Success);
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                SetDownloadStatus("Přihlášení Webshare selhalo", error.Message, Theme.Danger);
            }
            finally
            {
                password = null;
                webshareLoginButton.IsEnabled = true;
            }
        }

        private bool TryGetDownloadRateLimit(out string rateLimit)
        {
            rateLimit = "";
            if (downloadLimitEnabledCheck == null || downloadLimitEnabledCheck.IsChecked != true)
                return true;
            int kilobytes;
            if (!int.TryParse((downloadRateValueBox.Text ?? "").Trim(), out kilobytes) || kilobytes < 1 || kilobytes > 1000000)
                return false;
            rateLimit = kilobytes + "K";
            return true;
        }

        private void ApplyDownloadRateNow()
        {
            string rateLimit;
            if (!TryGetDownloadRateLimit(out rateLimit))
            {
                SetDownloadStatus("Neplatný limit rychlosti", "Zadej kladné celé číslo v KB/s, například 3000.", Theme.Danger);
                return;
            }
            directRateControl.Set(rateLimit);

            if (!busy)
            {
                settings.DownloadRateLimit = rateLimit;
                settings.Save();
                appliedDownloadRateLimit = rateLimit;
                SetDownloadStatus("Limit rychlosti uložen", DownloadRateLabel(rateLimit) + " · použije se při příštím stahování.", Theme.Success);
                UpdateDownloadRateState(
                    string.IsNullOrWhiteSpace(rateLimit) ? "Limit je vypnutý." : "Uloženo: " + DownloadRateLabel(rateLimit),
                    Theme.Success);
                UpdateDownloadButtons();
                return;
            }

            if (activeOperation != "download")
                return;
            if (activeCancellation == null)
            {
                settings.DownloadRateLimit = rateLimit;
                settings.Save();
                downloadRateApplyPending = true;
                SetDownloadStatus("Změna rychlosti připravena", DownloadRateLabel(rateLimit) + " · použije se pro nejbližší přenos.", Theme.Warning);
                UpdateDownloadRateState("Čeká na další přenos: " + DownloadRateLabel(rateLimit), Theme.Warning);
                UpdateDownloadButtons();
                return;
            }
            if (activeDownloadEngine == "direct" && downloadCanApplyRate)
            {
                settings.DownloadRateLimit = rateLimit;
                settings.Save();
                appliedDownloadRateLimit = rateLimit;
                downloadRateApplyPending = false;
                SetDownloadStatus("Limit rychlosti změněn", DownloadRateLabel(rateLimit) + " · aktivní bez přerušení přenosu.", Theme.Success);
                UpdateDownloadRateState("Aktivní: " + DownloadRateLabel(rateLimit), Theme.Success);
                UpdateDownloadButtons();
                return;
            }
            if (!downloadCanApplyRate)
            {
                settings.DownloadRateLimit = rateLimit;
                settings.Save();
                downloadRateApplyPending = true;
                SetDownloadStatus("Změna rychlosti připravena", "Použije se při nejbližším přenosu dat.", Theme.Warning);
                UpdateDownloadRateState("Čeká na další přenos: " + DownloadRateLabel(rateLimit), Theme.Warning);
                UpdateDownloadButtons();
                return;
            }
            downloadRateApplyPending = false;
            if (string.Equals(rateLimit, appliedDownloadRateLimit, StringComparison.OrdinalIgnoreCase))
            {
                SetDownloadStatus("Limit už je aktivní", DownloadRateLabel(rateLimit), Theme.Success);
                UpdateDownloadRateState("Aktivní: " + DownloadRateLabel(rateLimit), Theme.Success);
                UpdateDownloadButtons();
                return;
            }

            settings.DownloadRateLimit = rateLimit;
            settings.Save();
            downloadRateRestartRequested = true;
            downloadCanApplyRate = false;
            AppendDownloadLog("[Rychlost] Nový limit: " + DownloadRateLabel(rateLimit) + ". Navazuji rozpracovaný soubor.");
            SetDownloadStatus("Měním rychlost", "Navazuji rozpracovaný soubor bez ztráty stažených dat…", Theme.Primary);
            UpdateDownloadRateState("Měním na: " + DownloadRateLabel(rateLimit), Theme.Primary);
            UpdateDownloadButtons();
            activeCancellation.Cancel();
        }

        private void UpdateDownloadRateState(string text, string colorKey)
        {
            if (downloadRateStateText == null)
                return;
            downloadRateStateText.Text = text;
            Theme.Bind(downloadRateStateText, TextBlock.ForegroundProperty, colorKey);
        }

        private static string DownloadRateLabel(string rateLimit)
        {
            if (string.IsNullOrWhiteSpace(rateLimit))
                return "bez omezení";
            return RateLimitKilobytes(rateLimit) + " KB/s";
        }

        private static string RateLimitKilobytes(string rateLimit)
        {
            string value = (rateLimit ?? "").Trim().ToUpperInvariant();
            Match match = Regex.Match(value, "^([0-9]+)([KM])$");
            long amount;
            if (!match.Success || !long.TryParse(match.Groups[1].Value, out amount))
                return "3000";
            if (match.Groups[2].Value == "M")
                amount *= 1024;
            return Math.Max(1, Math.Min(1000000, amount)).ToString();
        }

        private void ApplyDownloadAdvancedMode()
        {
            if (downloadAdvancedPanel != null)
                downloadAdvancedPanel.Visibility = settings.AdvancedMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateDownloadButtons()
        {
            if (downloadStartButton == null)
                return;
            downloadStartButton.IsEnabled = !busy && cachedDownloadUrls.Count > 0;
            downloadCancelButton.IsEnabled = busy && activeOperation == "download" &&
                operationCancellation != null && !operationCancellation.IsCancellationRequested;
            if (downloadApplyRateButton != null)
            {
                string selected;
                bool valid = TryGetDownloadRateLimit(out selected);
                bool changed = valid && !string.Equals(selected, appliedDownloadRateLimit, StringComparison.OrdinalIgnoreCase);
                bool cancellationRequested = IsOperationCancellationRequested;
                downloadApplyRateButton.IsEnabled = DownloadRateControl.CanApply(
                    busy,
                    activeOperation == "download",
                    cancellationRequested,
                    downloadRateRestartRequested,
                    valid,
                    changed);
            }
        }

        private void UpdateDownloadControlState()
        {
            if (downloadUrlBox == null)
                return;

            bool editable = !busy;
            bool rateEditable = !busy || activeOperation == "download";
            downloadUrlBox.IsEnabled = editable;
            DownloadViewControl.PasteButton.IsEnabled = editable;
            DownloadViewControl.ClearButton.IsEnabled = editable;
            downloadFormatCombo.IsEnabled = editable;
            downloadQualityCombo.IsEnabled = editable;
            downloadFolderBox.IsEnabled = editable;
            DownloadViewControl.BrowseDownloadFolderButton.IsEnabled = editable;
            downloadSubtitlesCheck.IsEnabled = editable;
            downloadPlaylistCheck.IsEnabled = editable;
            downloadNoOverwriteCheck.IsEnabled = editable;
            downloadCookiesCheck.IsEnabled = editable;
            downloadCookieBrowserCombo.IsEnabled = editable;
            downloadExtraArgsBox.IsEnabled = editable;
            webshareLoginButton.IsEnabled = editable;
            jojLoginButton.IsEnabled = editable;
            downloadLimitEnabledCheck.IsEnabled = rateEditable;
            downloadRateValueBox.IsEnabled = rateEditable;
        }

        private void FocusDownloadInput()
        {
            downloadUrlBox.Focus();
            downloadUrlBox.SelectAll();
        }

        private static void SaveLog(string path, string text)
        {
            try
            {
                AppPaths.EnsureDirectories();
                File.AppendAllText(
                    path,
                    "==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ====" + Environment.NewLine + text + Environment.NewLine,
                    new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        private void ArrangeDownloadWorkspace(bool wide)
        {
            if (downloadWorkspace == null || downloadLinkCard == null || downloadSettingsCard == null)
                return;

            downloadWorkspace.RowDefinitions.Clear();
            downloadWorkspace.ColumnDefinitions.Clear();
            downloadWideLayout = wide;
            if (wide)
            {
                downloadWorkspace.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                downloadWorkspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.02, GridUnitType.Star) });
                downloadWorkspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.18, GridUnitType.Star) });
                Grid.SetRow(downloadLinkCard, 0);
                Grid.SetColumn(downloadLinkCard, 0);
                downloadLinkCard.Margin = new Thickness(0);
                Grid.SetRow(downloadSettingsCard, 0);
                Grid.SetColumn(downloadSettingsCard, 1);
                downloadSettingsCard.Margin = new Thickness(14, 0, 0, 0);
            }
            else
            {
                downloadWorkspace.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                downloadWorkspace.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                downloadWorkspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetRow(downloadLinkCard, 0);
                Grid.SetColumn(downloadLinkCard, 0);
                downloadLinkCard.Margin = new Thickness(0);
                Grid.SetRow(downloadSettingsCard, 1);
                Grid.SetColumn(downloadSettingsCard, 0);
                downloadSettingsCard.Margin = new Thickness(0, 14, 0, 0);
            }
        }

        private void UpdateDownloadResponsiveLayout(double windowWidth, double windowHeight)
        {
            if (downloadContent == null)
                return;

            double horizontalMargin = windowWidth >= 1700 ? 44 : windowWidth >= 1200 ? 32 : 20;
            downloadContent.Margin = new Thickness(horizontalMargin, 26, horizontalMargin, 34);
            bool wide = windowWidth >= 1280;
            if (wide != downloadWideLayout)
                ArrangeDownloadWorkspace(wide);

            if (downloadUrlInputHost != null)
                downloadUrlInputHost.Height = Math.Max(122, Math.Min(170, windowHeight * 0.16));
            if (downloadLogBox != null)
                downloadLogBox.MaxHeight = Math.Max(260, Math.Min(390, windowHeight * 0.42));
        }
    }
}
