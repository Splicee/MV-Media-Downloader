using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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

namespace MVMediaStudio
{
    public partial class MainWindow : Window
    {
        private readonly AppSettings settings;
        private readonly ToolService toolService;
        private readonly ObservableCollection<ConversionJob> conversionJobs;
        private readonly StringBuilder downloadLog;
        private readonly StringBuilder conversionLog;

        private ToolState tools;
        private CancellationTokenSource operationCancellation;
        private CancellationTokenSource activeCancellation;
        private bool busy;
        private bool allowWindowClose;
        private bool closingInProgress;
        private bool powerRequestActive;
        private string currentPage = "download";
        private string activeOperation = "";

        private Grid downloadPage;
        private Grid conversionPage;
        private Button downloadNavButton;
        private Button conversionNavButton;
        private Button repairButton;
        private Button maximizeWindowButton;
        private TextBlock footerStatus;
        private StackPanel toolStatusPanel;
        private TextBlock brandSubtitle;
        private StackPanel brandTextPanel;
        private ColumnDefinition titleBrandColumn;
        private ColumnDefinition titleNavigationColumn;
        private ContextMenu repairMenu;
        private MenuItem advancedMenuItem;
        private MenuItem themeMenuItem;
        private MenuItem autoUpdateMenuItem;
        private FrameworkElement conversionAdvancedPanel;
        private DataGridColumn conversionCodecColumn;

        public MainWindow()
        {
            AppPaths.EnsureDirectories();
            settings = AppSettings.Load();
            toolService = new ToolService();
            tools = new ToolState();
            conversionJobs = new ObservableCollection<ConversionJob>();
            downloadLog = new StringBuilder();
            conversionLog = new StringBuilder();

            InitializeComponent();
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
            if (settings.WindowMaximized)
                WindowState = WindowState.Maximized;
            Theme.Apply(this, IsDark);
            InitializeShell();
            InitializeDownloadView();
            InitializeConversionView();
            InitializeRepairMenu();
            Navigate(settings.LastPage);
            bool startWithConversion = false;
            bool startWithDownload = false;
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, "--conversion", StringComparison.OrdinalIgnoreCase))
                    startWithConversion = true;
                else if (string.Equals(argument, "--download", StringComparison.OrdinalIgnoreCase))
                    startWithDownload = true;
                else if (string.Equals(argument, "--advanced", StringComparison.OrdinalIgnoreCase))
                    settings.AdvancedMode = true;
            }
            ApplyAdvancedMode();
            if (startWithConversion)
                Navigate("conversion");
            else if (startWithDownload)
                Navigate("download");

            SizeChanged += delegate { UpdateResponsiveLayout(); };
            StateChanged += delegate { UpdateMaximizeGlyph(); };
            Loaded += async delegate
            {
                UpdateResponsiveLayout();
                UpdateService.SignalHealthy(Environment.GetCommandLineArgs());
                await RefreshToolsAsync(false);
                string updatedVersion = UpdateService.ArgumentValue(Environment.GetCommandLineArgs(), "--updated");
                string failedUpdate = UpdateService.ArgumentValue(Environment.GetCommandLineArgs(), "--update-failed");
                if (!string.IsNullOrWhiteSpace(updatedVersion))
                    footerStatus.Text = "Aktualizováno na verzi " + updatedVersion;
                else if (!string.IsNullOrWhiteSpace(failedUpdate))
                    footerStatus.Text = "Aktualizace byla vrácena zpět: " + failedUpdate;
                else if (settings.AutoUpdate)
                    await CheckForUpdatesAsync(false);
            };
            Closing += WindowClosing;
            PreviewKeyDown += HandleShortcuts;
        }

        private bool IsDark
        {
            get { return !string.Equals(settings.Theme, "light", StringComparison.OrdinalIgnoreCase); }
        }

        private void InitializeShell()
        {
            downloadPage = DownloadPageHost;
            conversionPage = ConversionPageHost;
            downloadNavButton = DownloadNavButton;
            conversionNavButton = ConversionNavButton;
            repairButton = RepairButton;
            maximizeWindowButton = MaximizeWindowButton;
            footerStatus = FooterStatus;
            toolStatusPanel = ToolStatusPanel;
            brandSubtitle = BrandSubtitle;
            brandTextPanel = BrandTextPanel;
            titleBrandColumn = BrandColumn;
            titleNavigationColumn = NavigationColumn;
            repairMenu = RepairContextMenu;
            VersionText.Text = "MV Media Downloader " + ProductVersion();

            downloadNavButton.Click += delegate { Navigate("download"); };
            conversionNavButton.Click += delegate { Navigate("conversion"); };
            RenderToolStatus();
            repairButton.Click += delegate
            {
                repairMenu.PlacementTarget = repairButton;
                repairMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                repairMenu.IsOpen = true;
            };
            MinimizeWindowButton.Click += delegate { WindowState = WindowState.Minimized; };
            maximizeWindowButton.Click += delegate
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            };
            CloseWindowButton.Click += delegate { Close(); };
        }

        private void UpdateResponsiveLayout()
        {
            double width = ActualWidth;
            if (width <= 0)
                return;

            bool compact = width < 1160;
            bool iconBrand = width < 1040;
            if (titleBrandColumn != null)
                titleBrandColumn.Width = new GridLength(iconBrand ? 70 : compact ? 220 : 270);
            if (titleNavigationColumn != null)
                titleNavigationColumn.Width = new GridLength(compact ? 280 : 320);
            if (brandTextPanel != null)
                brandTextPanel.Visibility = iconBrand ? Visibility.Collapsed : Visibility.Visible;
            if (brandSubtitle != null)
                brandSubtitle.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            if (toolStatusPanel != null)
                toolStatusPanel.Visibility = width >= 1320 ? Visibility.Visible : Visibility.Collapsed;
            if (repairButton != null)
            {
                bool iconOnly = iconBrand;
                repairButton.Content = iconOnly
                    ? (object)new TextBlock
                    {
                        Text = "\uE90F",
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                        FontSize = 15,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                    : IconText("\uE90F", "Nástroje");
                repairButton.MinWidth = iconOnly ? 42 : 0;
                repairButton.Padding = iconOnly ? new Thickness(0) : new Thickness(13, 7, 13, 7);
                repairButton.ToolTip = iconOnly ? "Nástroje a nastavení" : null;
            }

            UpdateDownloadResponsiveLayout(width, ActualHeight);
            UpdateConversionResponsiveLayout(width, ActualHeight);
        }

        private void UpdateMaximizeGlyph()
        {
            if (maximizeWindowButton == null)
                return;
            TextBlock glyph = maximizeWindowButton.Content as TextBlock;
            if (glyph != null)
                glyph.Text = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
            maximizeWindowButton.ToolTip = WindowState == WindowState.Maximized ? "Obnovit okno" : "Maximalizovat";
        }

        private void Navigate(string page)
        {
            bool download = !string.Equals(page, "conversion", StringComparison.OrdinalIgnoreCase);
            currentPage = download ? "download" : "conversion";
            settings.LastPage = currentPage;
            downloadPage.Visibility = download ? Visibility.Visible : Visibility.Collapsed;
            conversionPage.Visibility = download ? Visibility.Collapsed : Visibility.Visible;
            SetNavSelected(downloadNavButton, download);
            SetNavSelected(conversionNavButton, !download);
            footerStatus.Text = download ? "Stahování připraveno" : "Konverze připravena";
            Dispatcher.BeginInvoke(
                new Action(UpdateResponsiveLayout),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static void ConfigurePageScroll(ScrollViewer scroll)
        {
            int wheelRemainder = 0;
            scroll.CanContentScroll = false;
            scroll.PreviewMouseWheel += delegate (object sender, MouseWheelEventArgs eventArgs)
            {
                if (HasNestedScrollViewer(eventArgs.OriginalSource as DependencyObject, scroll))
                    return;

                int steps = ScrollWheelTuning.ConsumeSteps(ref wheelRemainder, eventArgs.Delta);
                if (steps != 0)
                    scroll.ScrollToVerticalOffset(scroll.VerticalOffset - steps * ScrollWheelTuning.PixelStep);
                eventArgs.Handled = true;
            };
        }

        private static bool HasNestedScrollViewer(DependencyObject source, ScrollViewer pageScroll)
        {
            DependencyObject current = source;
            while (current != null && current != pageScroll)
            {
                if (current is ScrollViewer)
                    return true;
                try
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                catch (InvalidOperationException)
                {
                    current = LogicalTreeHelper.GetParent(current);
                }
            }
            return false;
        }

        private void SetNavSelected(Button button, bool selected)
        {
            if (selected)
            {
                Theme.Bind(button, Button.BackgroundProperty, Theme.Primary);
                button.Foreground = Brushes.White;
            }
            else
            {
                button.Background = Brushes.Transparent;
                Theme.Bind(button, Button.ForegroundProperty, Theme.TitleBarMuted);
            }
        }

        private void ToggleTheme()
        {
            settings.Theme = IsDark ? "light" : "dark";
            Theme.Apply(this, IsDark);
            SetNavSelected(downloadNavButton, currentPage == "download");
            SetNavSelected(conversionNavButton, currentPage == "conversion");
            themeMenuItem.Header = IsDark ? "Světlý režim" : "Tmavý režim";
            settings.Save();
        }

        private void ToggleAdvanced()
        {
            settings.AdvancedMode = !settings.AdvancedMode;
            ApplyAdvancedMode();
            settings.Save();
        }

        private void ApplyAdvancedMode()
        {
            if (advancedMenuItem != null)
                advancedMenuItem.IsChecked = settings.AdvancedMode;
            if (conversionAdvancedPanel != null)
                conversionAdvancedPanel.Visibility = settings.AdvancedMode ? Visibility.Visible : Visibility.Collapsed;
            if (conversionCodecColumn != null)
                conversionCodecColumn.Visibility = settings.AdvancedMode ? Visibility.Visible : Visibility.Collapsed;
            if (conversionCodecField != null)
                conversionCodecField.Visibility = settings.AdvancedMode ? Visibility.Visible : Visibility.Collapsed;
            if (conversionCodecNoticePanel != null)
                conversionCodecNoticePanel.Visibility = settings.AdvancedMode ? Visibility.Visible : Visibility.Collapsed;
            if (!settings.AdvancedMode && conversionCodecCombo != null)
                SelectCombo(conversionCodecCombo, "h264");
            ApplyDownloadAdvancedMode();
        }

        private void SaveSettings()
        {
            CaptureDownloadSettings();
            CaptureConversionSettings();
            Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
            if (bounds.Width >= MinWidth && bounds.Width <= 7680)
                settings.WindowWidth = bounds.Width;
            if (bounds.Height >= MinHeight && bounds.Height <= 4320)
                settings.WindowHeight = bounds.Height;
            settings.WindowMaximized = WindowState == WindowState.Maximized;
            settings.Save();
        }

        private async void WindowClosing(object sender, CancelEventArgs eventArgs)
        {
            SaveSettings();
            if (allowWindowClose || !busy || activeOperation == "update")
                return;

            eventArgs.Cancel = true;
            if (closingInProgress)
                return;

            MessageBoxResult answer = MessageBox.Show(
                this,
                "Právě probíhá " + ActiveOperationLabel() + ". Chceš ji ukončit a zavřít aplikaci?\n\nRozpracovaná stažená data zůstanou zachovaná pro pozdější navázání.",
                "Ukončit probíhající úlohu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes)
                return;

            closingInProgress = true;
            CancelActiveWork();
            Stopwatch wait = Stopwatch.StartNew();
            while (busy && wait.Elapsed < TimeSpan.FromSeconds(6))
                await Task.Delay(100);

            allowWindowClose = true;
            closingInProgress = false;
            Close();
        }

        private string ActiveOperationLabel()
        {
            if (activeOperation == "download")
                return "stahování";
            if (activeOperation == "conversion")
                return "konverze";
            if (activeOperation == "tools")
                return "příprava nástroje";
            return "úloha";
        }

        private async void HandleShortcuts(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.Key == Key.F5)
            {
                await RefreshToolsAsync(true);
                eventArgs.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control &&
                (eventArgs.Key == Key.D1 || eventArgs.Key == Key.NumPad1))
            {
                Navigate("download");
                eventArgs.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control &&
                (eventArgs.Key == Key.D2 || eventArgs.Key == Key.NumPad2))
            {
                Navigate("conversion");
                eventArgs.Handled = true;
            }
            else if (eventArgs.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (currentPage == "download")
                    await StartDownloadAsync();
                else
                    await StartConversionAsync();
                eventArgs.Handled = true;
            }
            else if (eventArgs.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OpenDirectory(currentPage == "download" ? settings.DownloadDirectory : settings.ConversionDirectory);
                eventArgs.Handled = true;
            }
            else if (eventArgs.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control && currentPage == "download")
            {
                FocusDownloadInput();
                eventArgs.Handled = true;
            }
            else if (eventArgs.Key == Key.Escape && busy)
            {
                CancelActiveWork();
                eventArgs.Handled = true;
            }
        }

        private void CancelActiveWork()
        {
            downloadRateRestartRequested = false;
            downloadRateApplyPending = false;
            bool canCancel = (operationCancellation != null && !operationCancellation.IsCancellationRequested) ||
                (activeCancellation != null && !activeCancellation.IsCancellationRequested);
            if (canCancel)
            {
                if (activeOperation == "download")
                {
                    downloadCanApplyRate = false;
                    SetDownloadStatus("Ruším stahování", "Ukončuji aktivní přenos a zachovávám rozpracovaná data…", Theme.Warning);
                    footerStatus.Text = "Ruším stahování";
                }
                else if (activeOperation == "conversion")
                {
                    SetConversionStatus("Ruším konverzi", "Ukončuji aktivní převod…", Theme.Warning);
                    footerStatus.Text = "Ruším konverzi";
                }
                if (operationCancellation != null && !operationCancellation.IsCancellationRequested)
                    operationCancellation.Cancel();
                if (activeCancellation != null && !activeCancellation.IsCancellationRequested)
                    activeCancellation.Cancel();
                UpdateDownloadButtons();
                UpdateConversionButtons();
            }
        }

        private void SetBusy(bool value, string message)
        {
            busy = value;
            footerStatus.Text = message;
            if (value && !powerRequestActive &&
                (activeOperation == "download" || activeOperation == "conversion"))
            {
                powerRequestActive = SystemPowerService.PreventSleep();
            }
            else if (!value && powerRequestActive)
            {
                SystemPowerService.AllowSleep();
                powerRequestActive = false;
            }
            if (repairButton != null)
                repairButton.IsEnabled = !value;
            UpdateDownloadControlState();
            UpdateConversionControlState();
            UpdateDownloadButtons();
            UpdateConversionButtons();
        }

        private void BeginCancellableOperation(string operation)
        {
            EndCancellableOperation();
            operationCancellation = new CancellationTokenSource();
            activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(operationCancellation.Token);
            activeOperation = operation;
        }

        private void RenewActiveCancellation()
        {
            if (activeCancellation != null)
                activeCancellation.Dispose();
            activeCancellation = operationCancellation == null
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(operationCancellation.Token);
        }

        private void EndCancellableOperation()
        {
            if (activeCancellation != null)
            {
                activeCancellation.Dispose();
                activeCancellation = null;
            }
            if (operationCancellation != null)
            {
                operationCancellation.Dispose();
                operationCancellation = null;
            }
            activeOperation = "";
        }

        private bool IsOperationCancellationRequested
        {
            get
            {
                return (operationCancellation != null && operationCancellation.IsCancellationRequested) ||
                    (activeCancellation != null && activeCancellation.IsCancellationRequested);
            }
        }

        private void SetTaskbarProgress(double percentage, TaskbarItemProgressState state)
        {
            if (TaskbarItemInfo == null)
                return;
            TaskbarItemInfo.ProgressState = state;
            TaskbarItemInfo.ProgressValue = Math.Max(0, Math.Min(1, percentage / 100d));
        }

        private void OpenDirectory(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;
                Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
            }
        }

        private void RevealFile(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return;
                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    UseShellExecute = true
                };
                start.ArgumentList.Add("/select,");
                start.ArgumentList.Add(Path.GetFullPath(path));
                Process.Start(start);
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                OpenDirectory(Path.GetDirectoryName(path));
            }
        }

        private StackPanel IconText(string glyph, string label)
        {
            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            panel.Children.Add(new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            return panel;
        }

        private static void PopulateCombo(ComboBox combo, params ComboItem[] items)
        {
            combo.Items.Clear();
            foreach (ComboItem item in items)
                combo.Items.Add(item);
        }

        private static SolidColorBrush Brush(string value)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        }

        private static string ComboValue(ComboBox combo, string fallback)
        {
            ComboItem item = combo == null ? null : combo.SelectedItem as ComboItem;
            return item == null ? fallback : item.Value;
        }

        private static void SelectCombo(ComboBox combo, string value)
        {
            if (combo == null)
                return;
            foreach (object rawItem in combo.Items)
            {
                ComboItem item = rawItem as ComboItem;
                if (item != null && string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private sealed class ComboItem
        {
            public readonly string Value;
            public readonly string Label;

            public ComboItem(string value, string label)
            {
                Value = value;
                Label = label;
            }

            public override string ToString()
            {
                return Label;
            }
        }

        private static string ProductVersion()
        {
            Version version = typeof(MainWindow).Assembly.GetName().Version;
            return version.Major + "." + version.Minor + "." + version.Build;
        }

        private static string FormatElapsed(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1)
                return elapsed.ToString(@"h\:mm\:ss");
            if (elapsed.TotalMinutes >= 1)
                return elapsed.ToString(@"m\:ss");
            return Math.Max(1, (int)Math.Round(elapsed.TotalSeconds)) + " s";
        }
    }
}
