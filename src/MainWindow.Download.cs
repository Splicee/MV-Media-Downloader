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
        private ComboBox downloadFormatCombo;
        private ComboBox downloadQualityCombo;
        private TextBox downloadRateValueBox;
        private CheckBox downloadUnlimitedCheck;
        private TextBox downloadFolderBox;
        private CheckBox downloadPlaylistCheck;
        private CheckBox downloadSubtitlesCheck;
        private CheckBox downloadCookiesCheck;
        private CheckBox downloadNoOverwriteCheck;
        private TextBox downloadExtraArgsBox;
        private FrameworkElement downloadAdvancedPanel;
        private Button downloadStartButton;
        private Button downloadCancelButton;
        private Button downloadLogToggle;
        private Border downloadLogCard;
        private TextBox downloadLogBox;
        private ProgressBar downloadProgress;
        private TextBlock downloadStatusTitle;
        private TextBlock downloadStatusDetail;
        private TextBlock downloadProgressPercent;
        private int downloadCompletedItems;
        private string downloadLiveLogLine = "";

        private Grid BuildDownloadPage()
        {
            Grid page = new Grid();
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            ConfigurePageScroll(scroll);
            StackPanel content = new StackPanel { Margin = new Thickness(32, 28, 32, 34), MaxWidth = 1160, HorizontalAlignment = HorizontalAlignment.Center };
            scroll.Content = content;
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
            content.Children.Add(header);

            StackPanel linkPanel = new StackPanel();
            Grid linkHeader = new Grid { Margin = new Thickness(0, 0, 0, 13) };
            linkHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linkHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel linkTitle = new StackPanel();
            linkTitle.Children.Add(Heading("Odkazy ke stažení", 17));
            TextBlock linkHint = Text("Jeden odkaz na řádek. Před odkazem může být číslo, například 01 https://…", 11.5, Theme.Muted);
            linkHint.Margin = new Thickness(0, 3, 0, 0);
            linkTitle.Children.Add(linkHint);
            linkHeader.Children.Add(linkTitle);
            downloadUrlCount = Text("0 odkazů", 11.5, Theme.Muted);
            downloadUrlCount.FontWeight = FontWeights.SemiBold;
            downloadUrlCount.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(downloadUrlCount, 1);
            linkHeader.Children.Add(downloadUrlCount);
            linkPanel.Children.Add(linkHeader);

            Grid inputHost = new Grid { Height = 122 };
            downloadUrlBox = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(13, 12, 13, 12), AllowDrop = true };
            downloadUrlBox.TextChanged += delegate
            {
                downloadPlaceholder.Visibility = string.IsNullOrWhiteSpace(downloadUrlBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                UpdateDownloadUrlCount();
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
            downloadPlaceholder = Text("https://www.youtube.com/watch?v=…  nebo  https://www.joj.sk/relacia/…/epizoda/…", 13, Theme.Muted);
            downloadPlaceholder.Margin = new Thickness(14, 13, 0, 0);
            downloadPlaceholder.VerticalAlignment = VerticalAlignment.Top;
            downloadPlaceholder.IsHitTestVisible = false;
            inputHost.Children.Add(downloadPlaceholder);
            linkPanel.Children.Add(inputHost);

            StackPanel linkActions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            Button paste = CreateActionButton("\uE77F", "Vložit ze schránky");
            Button clear = CreateActionButton("\uE74D", "Vymazat");
            clear.Margin = new Thickness(8, 0, 0, 0);
            paste.Click += delegate
            {
                try { if (Clipboard.ContainsText()) downloadUrlBox.Text = Clipboard.GetText(); } catch { }
            };
            clear.Click += delegate { downloadUrlBox.Clear(); downloadUrlBox.Focus(); };
            linkActions.Children.Add(paste);
            linkActions.Children.Add(clear);
            linkPanel.Children.Add(linkActions);

            Border linkCard = Card(linkPanel);
            content.Children.Add(linkCard);

            StackPanel settingsPanel = new StackPanel();
            settingsPanel.Children.Add(Heading("Výsledek", 17));
            Grid choices = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.65, GridUnitType.Star) });
            choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.85, GridUnitType.Star) });
            choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
            choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) });

            downloadFormatCombo = Combo(
                new ComboItem("mp4-h264", "Video + zvuk · MP4 / H.264"),
                new ComboItem("mkv-best", "Video + zvuk · MKV / nejlepší"),
                new ComboItem("webm", "Video + zvuk · WebM"),
                new ComboItem("audio-m4a", "Pouze zvuk · M4A"),
                new ComboItem("audio-mp3", "Pouze zvuk · MP3"),
                new ComboItem("audio-opus", "Pouze zvuk · Opus"),
                new ComboItem("video-only", "Pouze obraz · bez zvuku"));
            SelectCombo(downloadFormatCombo, settings.DownloadPreset);
            choices.Children.Add(Labeled("Typ souboru", downloadFormatCombo));

            downloadQualityCombo = Combo(
                new ComboItem("auto", "Automaticky"),
                new ComboItem("2160", "Až 4K"),
                new ComboItem("1440", "Až 1440p"),
                new ComboItem("1080", "Až 1080p"),
                new ComboItem("720", "Až 720p"),
                new ComboItem("480", "Až 480p"));
            SelectCombo(downloadQualityCombo, settings.DownloadQuality);
            Border quality = Labeled("Kvalita", downloadQualityCombo);
            quality.Margin = new Thickness(12, 0, 0, 0);
            Grid.SetColumn(quality, 1);
            choices.Children.Add(quality);

            Grid rateGrid = new Grid();
            rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            downloadRateValueBox = new TextBox
            {
                Text = RateLimitKilobytes(settings.DownloadRateLimit),
                MinHeight = 38,
                VerticalContentAlignment = VerticalAlignment.Center,
                ToolTip = "3000 KB/s odpovídá přibližně 3 MiB/s"
            };
            downloadRateValueBox.PreviewTextInput += delegate(object sender, TextCompositionEventArgs eventArgs)
            {
                eventArgs.Handled = !Regex.IsMatch(eventArgs.Text, "^[0-9]+$");
            };
            rateGrid.Children.Add(downloadRateValueBox);
            downloadUnlimitedCheck = new CheckBox
            {
                Content = "Bez omezení",
                IsChecked = string.IsNullOrWhiteSpace(settings.DownloadRateLimit),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(9, 0, 0, 0)
            };
            downloadUnlimitedCheck.Checked += delegate { downloadRateValueBox.IsEnabled = false; };
            downloadUnlimitedCheck.Unchecked += delegate { downloadRateValueBox.IsEnabled = true; };
            downloadRateValueBox.IsEnabled = downloadUnlimitedCheck.IsChecked != true;
            Grid.SetColumn(downloadUnlimitedCheck, 1);
            rateGrid.Children.Add(downloadUnlimitedCheck);
            Border rate = Labeled("Limit (KB/s)", rateGrid);
            rate.Margin = new Thickness(12, 0, 0, 0);
            Grid.SetColumn(rate, 2);
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
            Button open = CreateActionButton("\uE8B7", "");
            open.Width = 42;
            open.Margin = new Thickness(6, 0, 0, 0);
            open.ToolTip = "Otevřít výstupní složku";
            open.Click += delegate { OpenDirectory(downloadFolderBox.Text); };
            Grid.SetColumn(open, 2);
            folderGrid.Children.Add(open);
            Border folder = Labeled("Cílová složka", folderGrid);
            folder.Margin = new Thickness(12, 0, 0, 0);
            Grid.SetColumn(folder, 3);
            choices.Children.Add(folder);
            settingsPanel.Children.Add(choices);

            WrapPanel checks = new WrapPanel { Margin = new Thickness(0, 18, 0, 0) };
            downloadSubtitlesCheck = new CheckBox { Content = "Titulky CS/EN", IsChecked = settings.Subtitles };
            downloadPlaylistCheck = new CheckBox { Content = "Celý playlist" };
            downloadCookiesCheck = new CheckBox { Content = "Přihlášení z Chrome" };
            Button jojLogin = CreateActionButton("\uE77B", "Přihlásit JOJ Play");
            jojLogin.Margin = new Thickness(8, 0, 14, 6);
            jojLogin.ToolTip = "Otevře oddělený Chrome profil používaný pouze pro JOJ Play";
            jojLogin.Click += delegate { OpenJojPlayLogin(); };
            downloadNoOverwriteCheck = new CheckBox { Content = "Nepřepisovat existující", IsChecked = settings.NoOverwrite };
            checks.Children.Add(downloadSubtitlesCheck);
            checks.Children.Add(downloadPlaylistCheck);
            checks.Children.Add(downloadCookiesCheck);
            checks.Children.Add(jojLogin);
            checks.Children.Add(downloadNoOverwriteCheck);
            settingsPanel.Children.Add(checks);
            Border settingsCard = Card(settingsPanel);
            settingsCard.Margin = new Thickness(0, 14, 0, 0);
            content.Children.Add(settingsCard);

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
            content.Children.Add(advancedCard);

            Grid actions = new Grid { Margin = new Thickness(0, 18, 0, 0) };
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
            downloadLogToggle = CreateActionButton("\uE756", "Zobrazit log");
            downloadLogToggle.Click += delegate { ToggleDownloadLog(); };
            Grid.SetColumn(downloadLogToggle, 3);
            actions.Children.Add(downloadLogToggle);
            content.Children.Add(actions);

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
            content.Children.Add(progressCard);

            downloadLogBox = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, MinHeight = 180, MaxHeight = 260, FontFamily = new FontFamily("Consolas"), FontSize = 11.5 };
            Theme.Bind(downloadLogBox, Control.BackgroundProperty, Theme.Console);
            Theme.Bind(downloadLogBox, Control.ForegroundProperty, Theme.ConsoleText);
            downloadLogCard = Card(downloadLogBox);
            downloadLogCard.Margin = new Thickness(0, 14, 0, 0);
            downloadLogCard.Visibility = Visibility.Collapsed;
            content.Children.Add(downloadLogCard);

            UpdateDownloadButtons();
            return page;
        }

        private async Task StartDownloadAsync()
        {
            if (busy)
                return;
            List<string> urls = ValidDownloadUrls();
            if (urls.Count == 0)
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
            bool hasJojPlay = urls.Exists(delegate(string value)
            {
                Uri uri;
                return Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                    string.Equals(uri.Host, "play.joj.sk", StringComparison.OrdinalIgnoreCase);
            });
            if (hasJojPlay && !JojLoginService.IsReady && !OpenJojPlayLogin())
                return;
            if (hasJojPlay)
                downloadCookiesCheck.IsChecked = true;
            if (!await EnsureYtDlpAsync())
                return;

            downloadLog.Clear();
            downloadLiveLogLine = "";
            downloadLogBox.Clear();
            downloadCompletedItems = 0;
            downloadProgress.Value = 0;
            downloadProgressPercent.Text = "0 %";
            SetBusy(true, "Kontroluji odkazy");
            SetDownloadStatus("Kontroluji odkazy", "Ověřuji zdroj a dostupnost veřejného videa…", Theme.Primary);

            DownloadUrlResolution resolution;
            try
            {
                resolution = await JojUrlResolver.ResolveAsync(urls);
                urls = resolution.Urls;
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
                CookieBrowserSpec = hasJojPlay ? "chrome:" + JojLoginService.ProfileDirectory : "chrome",
                NoOverwrite = downloadNoOverwriteCheck.IsChecked == true,
                ExtraArguments = downloadExtraArgsBox.Text
            };

            List<string> arguments;
            try
            {
                arguments = DownloadArgumentBuilder.Build(options, urls, tools);
            }
            catch (Exception error)
            {
                SetBusy(false, "Neplatné nastavení");
                SetDownloadStatus("Nastavení není platné", error.Message, Theme.Danger);
                return;
            }

            activeCancellation = new CancellationTokenSource();
            activeOperation = "download";
            AppendDownloadLog("$ yt-dlp " + ArgumentUtilities.Join(arguments));
            SetBusy(true, "Probíhá stahování");
            SetDownloadStatus("Připravuji stahování", urls.Count == 1 ? "Zpracovávám odkaz…" : "Zpracovávám " + urls.Count + " odkazů…", Theme.Primary);

            int exitCode = -1;
            try
            {
                exitCode = await ProcessService.RunAsync(tools.YtDlpPath, arguments, HandleDownloadLine, activeCancellation.Token);
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                AppendDownloadLog(error.ToString());
            }

            CommitDownloadLiveLog();

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
                    SetDownloadStatus("Dokončeno s upozorněním", downloadCompletedItems + " z " + urls.Count + " souborů je připraveno. Podrobnosti jsou v logu.", Theme.Warning);
                    SetBusy(false, "Část souborů byla stažena");
                }
                else
                {
                    SetDownloadStatus("Stažení se nepovedlo", "Podrobnosti jsou v technickém logu.", Theme.Danger);
                    SetBusy(false, "Chyba při stahování");
                }
                ShowDownloadLog();
            }
            SaveLog(AppPaths.DownloadLogPath, downloadLog.ToString());
            activeCancellation = null;
            activeOperation = "";
        }

        private void HandleDownloadLine(string line, bool isError)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (line.StartsWith("MV_DONE:", StringComparison.Ordinal))
                {
                    downloadCompletedItems++;
                    downloadProgress.Value = 100;
                    downloadProgressPercent.Text = "100 %";
                    AppendDownloadLog("[Hotovo] " + line.Substring("MV_DONE:".Length));
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
                    return;
                }

                AppendDownloadLog((isError ? "! " : "") + line);
                if (line.IndexOf("[download]", StringComparison.OrdinalIgnoreCase) >= 0)
                    SetDownloadStatus("Připravuji soubor", "Zjišťuji velikost a dostupné datové proudy.", Theme.Primary);
                else if (line.IndexOf("[Merger]", StringComparison.OrdinalIgnoreCase) >= 0)
                    SetDownloadStatus("Dokončuji soubor", "Spojuji obraz a zvuk do výsledného formátu.", Theme.Primary);
                else if (line.IndexOf("[ExtractAudio]", StringComparison.OrdinalIgnoreCase) >= 0)
                    SetDownloadStatus("Zpracovávám zvuk", "Připravuji výsledný zvukový soubor.", Theme.Primary);
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
            int count = ValidDownloadUrls().Count;
            downloadUrlCount.Text = count == 1 ? "1 odkaz" : count + " odkazů";
            if (count > 0)
            {
                downloadStatusTitle.Text = "Připraveno ke stažení";
                downloadStatusDetail.Text = count == 1 ? "Odkaz je připravený." : count + " odkazů je připravených.";
            }
        }

        private List<string> ValidDownloadUrls()
        {
            if (downloadUrlBox == null)
                return new List<string>();
            return DownloadUrlParser.Parse(downloadUrlBox.Text);
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
            string rateLimit;
            if (TryGetDownloadRateLimit(out rateLimit))
                settings.DownloadRateLimit = rateLimit;
            settings.DownloadDirectory = string.IsNullOrWhiteSpace(downloadFolderBox.Text) ? AppPaths.DefaultDownloadDirectory : downloadFolderBox.Text;
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

        private bool TryGetDownloadRateLimit(out string rateLimit)
        {
            rateLimit = "";
            if (downloadUnlimitedCheck == null || downloadUnlimitedCheck.IsChecked == true)
                return true;
            int kilobytes;
            if (!int.TryParse((downloadRateValueBox.Text ?? "").Trim(), out kilobytes) || kilobytes < 1 || kilobytes > 1000000)
                return false;
            rateLimit = kilobytes + "K";
            return true;
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
            downloadStartButton.IsEnabled = !busy && ValidDownloadUrls().Count > 0;
            downloadCancelButton.IsEnabled = busy && activeOperation == "download";
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
    }
}
