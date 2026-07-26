using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MVMediaStudio.Core;
using MVMediaStudio.Services;
using MVMediaStudio.UI;
using Forms = System.Windows.Forms;

namespace MVMediaStudio
{
    internal partial class MainWindow
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
        private Button webshareLoginButton;
        private Border downloadLogCard;
        private TextBox downloadLogBox;
        private ProgressBar downloadProgress;
        private TextBlock downloadStatusTitle;
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
        private StackPanel downloadContent;
        private Grid downloadWorkspace;
        private Border downloadLinkCard;
        private Border downloadSettingsCard;
        private FrameworkElement downloadUrlInputHost;
        private readonly List<string> cachedDownloadUrls = new List<string>();
        private bool downloadWideLayout;

        private Grid BuildDownloadPage()
        {
            Grid page = new Grid();
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            ConfigurePageScroll(scroll);
            downloadContent = new StackPanel
            {
                Margin = new Thickness(32, 28, 32, 34),
                MaxWidth = 1560,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = downloadContent;
            page.Children.Add(scroll);

            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel title = new StackPanel();
            title.Children.Add(Heading("Stáhnout média", 27));
            TextBlock subtitle = Text("Vlož odkaz, zvol výsledek a zbytek vyřeší aplikace.", 13, Theme.Muted);
            subtitle.Margin = new Thickness(0, 5, 0, 0);
            title.Children.Add(subtitle);
            header.Children.Add(title);
            Border safePreset = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(11, 7, 11, 7), VerticalAlignment = VerticalAlignment.Center };
            Theme.Bind(safePreset, Border.BackgroundProperty, Theme.SurfaceAlt);
            TextBlock safeText = Text("H.264 · kompatibilní výchozí nastavení", 11.5, Theme.Success);
            safeText.FontWeight = FontWeights.SemiBold;
            safePreset.Child = safeText;
            Grid.SetColumn(safePreset, 1);
            header.Children.Add(safePreset);
            downloadContent.Children.Add(header);

            StackPanel linkPanel = new StackPanel();
            Grid linkHeader = new Grid { Margin = new Thickness(0, 0, 0, 13) };
            linkHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linkHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel linkTitle = new StackPanel();
            linkTitle.Children.Add(Heading("Odkazy ke stažení", 17));
            TextBlock linkHint = Text("Jeden odkaz na řádek. Před odkazem může být číslo, například 01 https://…", 11.5, Theme.Muted);
            linkHint.Margin = new Thickness(0, 3, 0, 0);
            linkTitle.Children.Add(linkHint);
            downloadSourceSummary = Text("Zdroj rozpoznám automaticky.", 11.5, Theme.Primary);
            downloadSourceSummary.Margin = new Thickness(0, 4, 0, 0);
            linkTitle.Children.Add(downloadSourceSummary);
            downloadSourceHint = Text("", 11, Theme.Warning);
            downloadSourceHint.Margin = new Thickness(0, 3, 0, 0);
            downloadSourceHint.TextWrapping = TextWrapping.Wrap;
            downloadSourceHint.Visibility = Visibility.Collapsed;
            linkTitle.Children.Add(downloadSourceHint);
            linkHeader.Children.Add(linkTitle);
            downloadUrlCount = Text("0 odkazů", 11.5, Theme.Muted);
            downloadUrlCount.FontWeight = FontWeights.SemiBold;
            downloadUrlCount.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(downloadUrlCount, 1);
            linkHeader.Children.Add(downloadUrlCount);
            linkPanel.Children.Add(linkHeader);

            Grid inputHost = new Grid { Height = 122 };
            downloadUrlInputHost = inputHost;
            downloadUrlBox = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(13, 12, 13, 12), AllowDrop = true };
            downloadUrlBox.TextChanged += delegate
            {
                downloadPlaceholder.Visibility = string.IsNullOrWhiteSpace(downloadUrlBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                RefreshDownloadInputAnalysis();
                UpdateDownloadButtons();
            };
            downloadUrlBox.PreviewDragOver += delegate(object sender, DragEventArgs eventArgs)
            {
                eventArgs.Effects = eventArgs.Data.GetDataPresent(DataFormats.Text) ? DragDropEffects.Copy : DragDropEffects.None;
                eventArgs.Handled = true;
            };
            downloadUrlBox.Drop += delegate(object sender, DragEventArgs eventArgs)
            {
                if (eventArgs.Data.GetDataPresent(DataFormats.Text))
                    downloadUrlBox.Text = Convert.ToString(eventArgs.Data.GetData(DataFormats.Text));
            };
            inputHost.Children.Add(downloadUrlBox);
            downloadPlaceholder = Text("YouTube, ČT, Nova, Stream.cz, Český rozhlas, TV Noe, JOJ nebo další web…", 13, Theme.Muted);
            downloadPlaceholder.Margin = new Thickness(14, 13, 0, 0);
            downloadPlaceholder.VerticalAlignment = VerticalAlignment.Top;
            downloadPlaceholder.IsHitTestVisible = false;
            inputHost.Children.Add(downloadPlaceholder);
            linkPanel.Children.Add(inputHost);

            Grid linkActions = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            linkActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linkActions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel primaryLinkActions = new StackPanel { Orientation = Orientation.Horizontal };
            Button paste = CreateActionButton("\uE77F", "Vložit ze schránky");
            Button clear = CreateActionButton("\uE74D", "Vymazat");
            clear.Margin = new Thickness(8, 0, 0, 0);
            paste.Click += delegate
            {
                try { if (Clipboard.ContainsText()) downloadUrlBox.Text = Clipboard.GetText(); } catch { }
            };
            clear.Click += delegate { downloadUrlBox.Clear(); downloadUrlBox.Focus(); };
            primaryLinkActions.Children.Add(paste);
            primaryLinkActions.Children.Add(clear);
            linkActions.Children.Add(primaryLinkActions);
            Button supportedSources = CreateActionButton("\uE946", "Podporované weby");
            supportedSources.Click += delegate { new SourceSupportDialog(this).ShowDialog(); };
            Grid.SetColumn(supportedSources, 1);
            linkActions.Children.Add(supportedSources);
            linkPanel.Children.Add(linkActions);

            downloadLinkCard = Card(linkPanel);

            StackPanel settingsPanel = new StackPanel();
            settingsPanel.Children.Add(Heading("Výsledek", 17));
            AdaptiveGrid choices = new AdaptiveGrid
            {
                Margin = new Thickness(0, 16, 0, 0),
                ItemMinWidth = 300,
                MaximumColumns = 4,
                ColumnSpacing = 12,
                RowSpacing = 14
            };

            downloadFormatCombo = Combo(
                new ComboItem("mp4-h264", "Video + zvuk · MP4 / H.264"),
                new ComboItem("mkv-best", "Video + zvuk · MKV / nejlepší"),
                new ComboItem("webm", "Video + zvuk · WebM"),
                new ComboItem("audio-m4a", "Pouze zvuk · M4A / AAC"),
                new ComboItem("audio-mp3", "Pouze zvuk · MP3"),
                new ComboItem("audio-opus", "Pouze zvuk · Opus"),
                new ComboItem("audio-flac", "Pouze zvuk · FLAC bezztrátový"),
                new ComboItem("video-only", "Pouze obraz · bez zvuku"));
            downloadFormatCombo.ToolTip = "U Webshare a přímých souborů se výsledek po stažení připraví přes FFmpeg.";
            SelectCombo(downloadFormatCombo, settings.DownloadPreset);
            choices.Children.Add(Labeled("Typ souboru", downloadFormatCombo));

            downloadQualityCombo = Combo(
                new ComboItem("auto", "Automaticky"),
                new ComboItem("2160", "Až 4K"),
                new ComboItem("1440", "Až 1440p"),
                new ComboItem("1080", "Až 1080p"),
                new ComboItem("720", "Až 720p"),
                new ComboItem("480", "Až 480p"));
            downloadQualityCombo.ToolTip = "Vyšší video zůstane zachované nebo se zmenší na zvolenou maximální výšku.";
            SelectCombo(downloadQualityCombo, settings.DownloadQuality);
            Border quality = Labeled("Kvalita", downloadQualityCombo);
            choices.Children.Add(quality);

            appliedDownloadRateLimit = settings.DownloadRateLimit ?? "";
            StackPanel ratePanel = new StackPanel();
            downloadLimitEnabledCheck = new CheckBox
            {
                Content = "Omezit rychlost",
                IsChecked = !string.IsNullOrWhiteSpace(settings.DownloadRateLimit),
                VerticalAlignment = VerticalAlignment.Center
            };
            ratePanel.Children.Add(downloadLimitEnabledCheck);
            Grid rateGrid = new Grid { Margin = new Thickness(0, 8, 0, 0), MinWidth = 190, HorizontalAlignment = HorizontalAlignment.Left };
            rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            downloadRateValueBox = new TextBox
            {
                Text = RateLimitKilobytes(settings.DownloadRateLimit),
                MinHeight = 38,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(12, 9, 49, 9),
                ToolTip = "3000 KB/s odpovídá přibližně 3 MiB/s"
            };
            downloadRateValueBox.PreviewTextInput += delegate(object sender, TextCompositionEventArgs eventArgs)
            {
                eventArgs.Handled = !Regex.IsMatch(eventArgs.Text, "^[0-9]+$");
            };
            downloadRateValueBox.TextChanged += delegate { UpdateDownloadButtons(); };
            downloadRateValueBox.PreviewKeyDown += delegate(object sender, KeyEventArgs eventArgs)
            {
                if (eventArgs.Key != Key.Enter)
                    return;
                ApplyDownloadRateNow();
                eventArgs.Handled = true;
            };
            Grid rateInputHost = new Grid();
            rateInputHost.Children.Add(downloadRateValueBox);
            TextBlock rateUnit = Text("KB/s", 11.5, Theme.Muted);
            rateUnit.HorizontalAlignment = HorizontalAlignment.Right;
            rateUnit.VerticalAlignment = VerticalAlignment.Center;
            rateUnit.Margin = new Thickness(0, 0, 10, 0);
            rateUnit.IsHitTestVisible = false;
            rateInputHost.Children.Add(rateUnit);
            rateGrid.Children.Add(rateInputHost);
            downloadApplyRateButton = new Button
            {
                Content = new TextBlock
                {
                    Text = "\u2713",
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = 17,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                MinHeight = 38,
                Padding = new Thickness(0)
            };
            downloadApplyRateButton.Width = 40;
            downloadApplyRateButton.MinWidth = 40;
            downloadApplyRateButton.Margin = new Thickness(6, 0, 0, 0);
            downloadApplyRateButton.ToolTip = "Potvrdit limit rychlosti (Enter)";
            downloadApplyRateButton.Click += delegate { ApplyDownloadRateNow(); };
            Grid.SetColumn(downloadApplyRateButton, 1);
            rateGrid.Children.Add(downloadApplyRateButton);
            downloadRateEditor = rateGrid;
            downloadRateEditor.Visibility = downloadLimitEnabledCheck.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
            ratePanel.Children.Add(downloadRateEditor);
            downloadLimitEnabledCheck.Checked += delegate
            {
                downloadRateEditor.Visibility = Visibility.Visible;
                downloadRateValueBox.Focus();
                downloadRateValueBox.SelectAll();
                UpdateDownloadButtons();
            };
            downloadLimitEnabledCheck.Unchecked += delegate
            {
                downloadRateEditor.Visibility = Visibility.Hidden;
                if (downloadStatusTitle != null)
                    ApplyDownloadRateNow();
                UpdateDownloadButtons();
            };
            Border rate = Labeled("Rychlost stahování", ratePanel);
            choices.Children.Add(rate);

            Grid folderGrid = new Grid();
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            downloadFolderBox = new TextBox { Text = settings.DownloadDirectory, IsReadOnly = true, MinHeight = 38, VerticalContentAlignment = VerticalAlignment.Center };
            folderGrid.Children.Add(downloadFolderBox);
            Button browse = CreateActionButton("\uE8B7", "Vybrat");
            browse.Margin = new Thickness(8, 0, 0, 0);
            browse.Click += delegate { BrowseDownloadFolder(); };
            Grid.SetColumn(browse, 1);
            folderGrid.Children.Add(browse);
            Button open = CreateIconButton("\uE838", "Otevřít výstupní složku");
            open.Margin = new Thickness(6, 0, 0, 0);
            open.Click += delegate { OpenDirectory(downloadFolderBox.Text); };
            Grid.SetColumn(open, 2);
            folderGrid.Children.Add(open);
            Border folder = Labeled("Cílová složka", folderGrid);
            choices.Children.Add(folder);
            settingsPanel.Children.Add(choices);

            AdaptiveGrid optionRow = new AdaptiveGrid
            {
                Margin = new Thickness(0, 18, 0, 0),
                ItemMinWidth = 430,
                MaximumColumns = 2,
                ColumnSpacing = 14,
                RowSpacing = 12
            };
            WrapPanel checks = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
            downloadSubtitlesCheck = new CheckBox { Content = "Titulky CS/EN", IsChecked = settings.Subtitles };
            downloadPlaylistCheck = new CheckBox { Content = "Celý playlist" };
            downloadNoOverwriteCheck = new CheckBox { Content = "Nepřepisovat existující", IsChecked = settings.NoOverwrite };
            checks.Children.Add(downloadSubtitlesCheck);
            checks.Children.Add(downloadPlaylistCheck);
            checks.Children.Add(downloadNoOverwriteCheck);
            StackPanel browserLogin = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 7, 18, 0) };
            downloadCookiesCheck = new CheckBox
            {
                Content = "Přihlášení z prohlížeče",
                IsChecked = settings.UseBrowserCookies,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            browserLogin.Children.Add(downloadCookiesCheck);
            downloadCookieBrowserCombo = Combo(
                new ComboItem("chrome", "Chrome"),
                new ComboItem("edge", "Edge"),
                new ComboItem("firefox", "Firefox"),
                new ComboItem("brave", "Brave"));
            downloadCookieBrowserCombo.MinWidth = 125;
            downloadCookieBrowserCombo.ToolTip = "Vyber prohlížeč, ve kterém jsi na daném webu přihlášený.";
            SelectCombo(downloadCookieBrowserCombo, settings.CookieBrowser);
            downloadCookieBrowserCombo.Visibility = settings.UseBrowserCookies ? Visibility.Visible : Visibility.Collapsed;
            browserLogin.Children.Add(downloadCookieBrowserCombo);
            downloadCookiesCheck.Checked += delegate { downloadCookieBrowserCombo.Visibility = Visibility.Visible; };
            downloadCookiesCheck.Unchecked += delegate { downloadCookieBrowserCombo.Visibility = Visibility.Collapsed; };
            checks.Children.Add(browserLogin);
            optionRow.Children.Add(checks);
            WrapPanel providerActions = new WrapPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            webshareLoginButton = CreateActionButton("\uE77B", WebshareService.HasSession ? "Webshare ✓" : "Přihlásit Webshare");
            webshareLoginButton.VerticalAlignment = VerticalAlignment.Center;
            webshareLoginButton.ToolTip = "Přihlášení přes oficiální Webshare API";
            webshareLoginButton.Click += async delegate { await OpenWebshareLoginAsync(); };
            providerActions.Children.Add(webshareLoginButton);
            Button jojLogin = CreateActionButton("\uE77B", "Přihlásit JOJ Play");
            jojLogin.Margin = new Thickness(12, 0, 0, 0);
            jojLogin.VerticalAlignment = VerticalAlignment.Center;
            jojLogin.ToolTip = "Otevře oddělený Chrome profil používaný pouze pro JOJ Play";
            jojLogin.Click += delegate { OpenJojPlayLogin(); };
            providerActions.Children.Add(jojLogin);
            optionRow.Children.Add(providerActions);
            settingsPanel.Children.Add(optionRow);
            downloadSettingsCard = Card(settingsPanel);

            downloadWorkspace = new Grid();
            downloadWorkspace.Children.Add(downloadLinkCard);
            downloadWorkspace.Children.Add(downloadSettingsCard);
            ArrangeDownloadWorkspace(false);
            downloadContent.Children.Add(downloadWorkspace);

            StackPanel advanced = new StackPanel();
            advanced.Children.Add(Heading("Pokročilé argumenty yt-dlp", 15));
            TextBlock advancedHint = Text("Volitelné. Používej jen parametry, kterým rozumíš.", 11.5, Theme.Muted);
            advancedHint.Margin = new Thickness(0, 3, 0, 10);
            advanced.Children.Add(advancedHint);
            downloadExtraArgsBox = new TextBox { MinHeight = 40 };
            advanced.Children.Add(downloadExtraArgsBox);
            Border advancedCard = Card(advanced);
            advancedCard.Margin = new Thickness(0, 14, 0, 0);
            downloadAdvancedPanel = advancedCard;
            downloadContent.Children.Add(advancedCard);

            Grid actions = new Grid { Margin = new Thickness(0, 18, 0, 0) };
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            downloadStartButton = CreatePrimaryButton("\uE896", "Stáhnout");
            downloadStartButton.MinWidth = 150;
            downloadStartButton.Click += async delegate { await StartDownloadAsync(); };
            actions.Children.Add(downloadStartButton);
            downloadCancelButton = CreateActionButton("\uE71A", "Zrušit");
            downloadCancelButton.Margin = new Thickness(8, 0, 0, 0);
            downloadCancelButton.Click += delegate { CancelActiveWork(); };
            Grid.SetColumn(downloadCancelButton, 1);
            actions.Children.Add(downloadCancelButton);
            downloadReportButton = CreateActionButton("\uE8BD", "Nahlásit chybu");
            downloadReportButton.Margin = new Thickness(0, 0, 8, 0);
            downloadReportButton.Visibility = Visibility.Collapsed;
            downloadReportButton.Click += delegate { SaveProblemReport("Stahování", downloadLog.ToString()); };
            Grid.SetColumn(downloadReportButton, 3);
            actions.Children.Add(downloadReportButton);
            downloadLogToggle = CreateActionButton("\uE756", "Zobrazit log");
            downloadLogToggle.Click += delegate { ToggleDownloadLog(); };
            Grid.SetColumn(downloadLogToggle, 4);
            actions.Children.Add(downloadLogToggle);
            downloadContent.Children.Add(actions);

            Grid progressPanel = new Grid();
            progressPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            progressPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            progressPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid progressHeader = new Grid();
            progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            downloadStatusTitle = Heading("Připraveno ke stažení", 15);
            progressHeader.Children.Add(downloadStatusTitle);
            downloadProgressPercent = Text("0 %", 17, Theme.Primary);
            downloadProgressPercent.FontWeight = FontWeights.SemiBold;
            downloadProgressPercent.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(downloadProgressPercent, 1);
            progressHeader.Children.Add(downloadProgressPercent);
            progressPanel.Children.Add(progressHeader);
            downloadStatusDetail = Text("Čekám na odkaz.", 11.5, Theme.Muted);
            downloadStatusDetail.Margin = new Thickness(0, 4, 0, 12);
            Grid.SetRow(downloadStatusDetail, 1);
            progressPanel.Children.Add(downloadStatusDetail);
            downloadProgress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0 };
            Grid.SetRow(downloadProgress, 2);
            progressPanel.Children.Add(downloadProgress);
            Border progressCard = Card(progressPanel);
            progressCard.Margin = new Thickness(0, 14, 0, 0);
            downloadContent.Children.Add(progressCard);

            downloadLogBox = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, MinHeight = 180, MaxHeight = 260, FontFamily = new FontFamily("Consolas"), FontSize = 11.5 };
            Theme.Bind(downloadLogBox, Control.BackgroundProperty, Theme.Console);
            Theme.Bind(downloadLogBox, Control.ForegroundProperty, Theme.ConsoleText);
            downloadLogCard = Card(downloadLogBox);
            downloadLogCard.Margin = new Thickness(0, 14, 0, 0);
            downloadLogCard.Visibility = Visibility.Collapsed;
            downloadContent.Children.Add(downloadLogCard);

            RefreshDownloadInputAnalysis();
            UpdateDownloadButtons();
            return page;
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
            if (directRoutes.Count > 0 && !await EnsureFfmpegAsync())
                return;

            downloadLog.Clear();
            downloadLiveLogLine = "";
            downloadLogBox.Clear();
            downloadCompletedItems = 0;
            downloadCompletedPaths.Clear();
            downloadProgress.Value = 0;
            downloadProgressPercent.Text = "0 %";
            downloadReportButton.Visibility = Visibility.Collapsed;
            downloadRateRestartRequested = false;
            downloadCanApplyRate = false;
            downloadRateApplyPending = false;
            SetBusy(true, "Kontroluji odkazy");
            SetDownloadStatus("Kontroluji odkazy", "Ověřuji zdroj a dostupnost veřejného videa…", Theme.Primary);

            if (ytDlpUrls.Count > 0)
            {
                try
                {
                    DownloadUrlResolution resolution = await JojUrlResolver.ResolveAsync(ytDlpUrls);
                    ytDlpUrls = resolution.Urls;
                    foreach (string note in resolution.Notes)
                        AppendDownloadLog(note);
                }
                catch (Exception error)
                {
                    AppPaths.WriteError(error);
                    AppendDownloadLog(error.Message);
                    SetBusy(false, "Odkaz není dostupný");
                    SetDownloadStatus("Odkaz JOJ nelze stáhnout", error.Message, Theme.Danger);
                    ShowDownloadLog();
                    return;
                }
            }

            CaptureDownloadSettings();
            settings.DownloadRateLimit = selectedRateLimit;
            Directory.CreateDirectory(settings.DownloadDirectory);
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

            activeOperation = "download";
            SetBusy(true, "Probíhá stahování");
            SetDownloadStatus("Připravuji stahování", inputUrls.Count == 1 ? "Zpracovávám odkaz…" : "Zpracovávám " + inputUrls.Count + " odkazů…", Theme.Primary);

            int exitCode = unsupportedRoutes.Count > 0 ? 1 : 0;
            foreach (DownloadRoute route in unsupportedRoutes)
            {
                AppendDownloadLog("! [" + route.Provider + "] " + route.Message);
            }

            if (directRoutes.Count > 0)
            {
                activeDownloadEngine = "direct";
                activeCancellation = new CancellationTokenSource();
                downloadCanApplyRate = true;
                foreach (DownloadRoute route in directRoutes)
                {
                    if (activeCancellation.IsCancellationRequested)
                    {
                        exitCode = -2;
                        break;
                    }

                    DirectDownloadItem item = null;
                    string downloadedPath = "";
                    bool sourceSkipped = false;
                    try
                    {
                        if (route.Kind == DownloadProviderKind.Webshare)
                            item = await WebshareService.ResolveAsync(route.Url);
                        else
                            item = new DirectDownloadItem
                            {
                                Provider = route.Provider,
                                SourceUrl = route.Url,
                                DownloadUrl = route.Url,
                                FileName = DownloadSourceRouter.FileNameFromUrl(route.Url)
                            };
                        AppendDownloadLog("[" + item.Provider + "] " + item.FileName);
                        downloadedPath = await DirectDownloadService.DownloadAsync(
                            item,
                            settings.DownloadDirectory,
                            options.NoOverwrite,
                            CurrentDirectRateLimitBytes,
                            delegate(DirectDownloadProgress progress)
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
                            delegate(DirectPostProcessProgress progress) { HandleDirectPostProcessProgress(item, progress); },
                            activeCancellation.Token);
                        MarkDirectDownloadCompleted(item, processed);
                    }
                    catch (OperationCanceledException)
                    {
                        if (!string.IsNullOrWhiteSpace(downloadedPath) && File.Exists(downloadedPath))
                            AppendDownloadLog("[Zachováno po zrušení] " + downloadedPath);
                        exitCode = -2;
                        break;
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
                activeCancellation = null;
                downloadCanApplyRate = false;
            }

            if (exitCode != -2 && ytDlpUrls.Count > 0)
            {
                activeDownloadEngine = "ytdlp";
                List<string> regularUrls = ytDlpUrls.FindAll(delegate(string value) { return !IsJojPlayUrl(value); });
                List<string> jojPlayUrls = ytDlpUrls.FindAll(IsJojPlayUrl);
                if (regularUrls.Count > 0)
                {
                    int ytDlpExit = await RunYtDlpDownloadAsync(options, regularUrls);
                    if (ytDlpExit == -2)
                        exitCode = -2;
                    else if (ytDlpExit != 0)
                        exitCode = ytDlpExit;
                }
                if (exitCode != -2 && jojPlayUrls.Count > 0)
                {
                    DownloadOptions jojOptions = CopyDownloadOptions(options);
                    jojOptions.CookiesFromBrowser = true;
                    jojOptions.CookieBrowserSpec = "chrome:" + JojLoginService.ProfileDirectory;
                    int jojExit = await RunYtDlpDownloadAsync(jojOptions, jojPlayUrls);
                    if (jojExit == -2)
                        exitCode = -2;
                    else if (jojExit != 0)
                        exitCode = jojExit;
                }
            }

            CommitDownloadLiveLog();
            activeCancellation = null;
            activeOperation = "";
            activeDownloadEngine = "";
            downloadCanApplyRate = false;
            downloadRateRestartRequested = false;

            if (exitCode == 0)
            {
                downloadProgress.Value = 100;
                downloadProgressPercent.Text = "100 %";
                SetDownloadStatus("Stažení dokončeno", "Soubory jsou připravené v cílové složce.", Theme.Success);
                SetBusy(false, "Stahování dokončeno");
            }
            else if (exitCode == -2)
            {
                SetDownloadStatus("Stahování zrušeno", "Rozpracovaná operace byla zastavena.", Theme.Warning);
                SetBusy(false, "Stahování zrušeno");
            }
            else
            {
                if (downloadCompletedItems > 0)
                {
                    SetDownloadStatus("Dokončeno s upozorněním", downloadCompletedItems + " z " + inputUrls.Count + " souborů je připraveno. Podrobnosti jsou v logu.", Theme.Warning);
                    SetBusy(false, "Část souborů byla stažena");
                }
                else
                {
                    SetDownloadStatus("Stažení se nepovedlo", "Podrobnosti jsou v technickém logu.", Theme.Danger);
                    SetBusy(false, "Chyba při stahování");
                }
                ShowDownloadLog();
                downloadReportButton.Visibility = Visibility.Visible;
            }
            SaveLog(AppPaths.DownloadLogPath, downloadLog.ToString());
        }

        private async Task<int> RunYtDlpDownloadAsync(DownloadOptions options, List<string> urls)
        {
            int exitCode = -1;
            bool firstRun = true;
            while (true)
            {
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

                activeCancellation = new CancellationTokenSource();
                appliedDownloadRateLimit = options.RateLimit ?? "";
                downloadRateRestartRequested = false;
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

                if (exitCode == -2 && downloadRateRestartRequested)
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
                string detail = progress.Provider + " · " + FormatTransferSpeed(progress.BytesPerSecond);
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
                downloadCanApplyRate = false;
                double percentage = Math.Max(0, Math.Min(100, progress.Percentage));
                downloadProgress.Value = percentage;
                downloadProgressPercent.Text = percentage.ToString("0.#") + " %";
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

        private long CurrentDirectRateLimitBytes()
        {
            string value = settings.DownloadRateLimit;
            if (string.IsNullOrWhiteSpace(value))
                return 0;
            long amount;
            string normalized = value.Trim().ToUpperInvariant();
            if (normalized.EndsWith("K") && long.TryParse(normalized.TrimEnd('K'), out amount))
                return amount * 1024;
            if (normalized.EndsWith("M") && long.TryParse(normalized.TrimEnd('M'), out amount))
                return amount * 1024 * 1024;
            return 0;
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
                if (line.StartsWith("MV_DONE:", StringComparison.Ordinal))
                {
                    string completedPath = line.Substring("MV_DONE:".Length);
                    if (downloadCompletedPaths.Add(completedPath))
                        downloadCompletedItems++;
                    downloadCanApplyRate = false;
                    downloadProgress.Value = 100;
                    downloadProgressPercent.Text = "100 %";
                    AppendDownloadLog("[Hotovo] " + completedPath);
                    SetDownloadStatus("Soubor dokončen", "Hotovo " + downloadCompletedItems + ". Pokračuji další položkou.", Theme.Success);
                    return;
                }

                Match percent = Regex.Match(line, "(?<![0-9])([0-9]{1,3}(?:\\.[0-9]+)?)%");
                double value = 0;
                bool isProgress = line.IndexOf("[download]", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    percent.Success &&
                    double.TryParse(percent.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
                if (isProgress)
                {
                    downloadCanApplyRate = true;
                    if (downloadRateApplyPending)
                    {
                        downloadRateApplyPending = false;
                        ApplyDownloadRateNow();
                        return;
                    }
                    value = Math.Max(0, Math.Min(100, value));
                    downloadProgress.Value = value;
                    downloadProgressPercent.Text = value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " %";

                    Match eta = Regex.Match(line, "ETA\\s+([^\\s]+)", RegexOptions.IgnoreCase);
                    Match speed = Regex.Match(line, "at\\s+([^\\s]+/s)", RegexOptions.IgnoreCase);
                    Match fragment = Regex.Match(line, "(?:frag|fragment)\\s+(\\d+)\\s*/\\s*(\\d+)", RegexOptions.IgnoreCase);
                    string detail = speed.Success ? speed.Groups[1].Value : "Stahuji data";
                    if (eta.Success) detail += " · zbývá " + eta.Groups[1].Value;
                    if (fragment.Success) detail += " · fragment " + fragment.Groups[1].Value + " / " + fragment.Groups[2].Value;
                    SetDownloadStatus("Stahování", detail, Theme.Primary);
                    SetDownloadLiveLog(line);
                    UpdateDownloadButtons();
                    return;
                }

                AppendDownloadLog((isError ? "! " : "") + line);
                if (line.IndexOf("[download]", StringComparison.OrdinalIgnoreCase) >= 0)
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

            if (!busy)
            {
                settings.DownloadRateLimit = rateLimit;
                settings.Save();
                appliedDownloadRateLimit = rateLimit;
                SetDownloadStatus("Limit rychlosti uložen", DownloadRateLabel(rateLimit) + " · použije se při příštím stahování.", Theme.Success);
                UpdateDownloadButtons();
                return;
            }

            if (activeOperation != "download" || activeCancellation == null)
                return;
            if (activeDownloadEngine == "direct")
            {
                settings.DownloadRateLimit = rateLimit;
                settings.Save();
                appliedDownloadRateLimit = rateLimit;
                downloadRateApplyPending = false;
                SetDownloadStatus("Limit rychlosti změněn", DownloadRateLabel(rateLimit) + " · aktivní bez přerušení přenosu.", Theme.Success);
                UpdateDownloadButtons();
                return;
            }
            if (!downloadCanApplyRate)
            {
                settings.DownloadRateLimit = rateLimit;
                settings.Save();
                downloadRateApplyPending = true;
                SetDownloadStatus("Změna rychlosti připravena", "Použije se při nejbližším přenosu dat.", Theme.Warning);
                UpdateDownloadButtons();
                return;
            }
            if (string.Equals(rateLimit, appliedDownloadRateLimit, StringComparison.OrdinalIgnoreCase))
            {
                SetDownloadStatus("Limit už je aktivní", DownloadRateLabel(rateLimit), Theme.Success);
                return;
            }

            settings.DownloadRateLimit = rateLimit;
            settings.Save();
            downloadRateRestartRequested = true;
            downloadCanApplyRate = false;
            AppendDownloadLog("[Rychlost] Nový limit: " + DownloadRateLabel(rateLimit) + ". Navazuji rozpracovaný soubor.");
            SetDownloadStatus("Měním rychlost", "Navazuji rozpracovaný soubor bez ztráty stažených dat…", Theme.Primary);
            UpdateDownloadButtons();
            activeCancellation.Cancel();
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
            downloadCancelButton.IsEnabled = busy && activeOperation == "download";
            if (downloadApplyRateButton != null)
            {
                string selected;
                bool valid = TryGetDownloadRateLimit(out selected);
                bool changed = valid && !string.Equals(selected, appliedDownloadRateLimit, StringComparison.OrdinalIgnoreCase);
                downloadApplyRateButton.IsEnabled = !busy ? valid : activeOperation == "download" && downloadCanApplyRate &&
                    !downloadRateRestartRequested && changed;
            }
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
                File.AppendAllText(path, "==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ====" + Environment.NewLine + text + Environment.NewLine);
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
