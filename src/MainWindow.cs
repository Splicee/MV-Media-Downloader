using System;
using System.Collections.ObjectModel;
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
    internal partial class MainWindow : Window
    {
        private readonly AppSettings settings;
        private readonly ToolService toolService;
        private readonly ObservableCollection<ConversionJob> conversionJobs;
        private readonly StringBuilder downloadLog;
        private readonly StringBuilder conversionLog;

        private ToolState tools;
        private CancellationTokenSource activeCancellation;
        private bool busy;
        private string currentPage = "download";
        private string activeOperation = "";

        private Grid root;
        private Grid downloadPage;
        private Grid conversionPage;
        private Button downloadNavButton;
        private Button conversionNavButton;
        private Button repairButton;
        private TextBlock footerStatus;
        private StackPanel toolStatusPanel;
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

            Title = "MV Media Downloader";
            Width = 1280;
            Height = 820;
            MinWidth = 1020;
            MinHeight = 680;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            FontFamily = new FontFamily("Segoe UI");
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 62,
                ResizeBorderThickness = new Thickness(7),
                CornerRadius = new CornerRadius(8),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            });

            Theme.Apply(this, IsDark);
            BuildShell();
            bool startWithConversion = false;
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, "--conversion", StringComparison.OrdinalIgnoreCase))
                    startWithConversion = true;
                else if (string.Equals(argument, "--advanced", StringComparison.OrdinalIgnoreCase))
                    settings.AdvancedMode = true;
            }
            ApplyAdvancedMode();
            if (startWithConversion)
                Navigate("conversion");

            Loaded += async delegate
            {
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
            Closing += delegate { SaveSettings(); CancelActiveWork(); };
            PreviewKeyDown += HandleShortcuts;
        }

        private bool IsDark
        {
            get { return !string.Equals(settings.Theme, "light", StringComparison.OrdinalIgnoreCase); }
        }

        private void BuildShell()
        {
            root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(62) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            Theme.Bind(root, Panel.BackgroundProperty, Theme.WindowBackground);
            Content = root;

            Grid titleBar = BuildTitleBar();
            Grid.SetRow(titleBar, 0);
            root.Children.Add(titleBar);

            Grid pages = new Grid();
            downloadPage = BuildDownloadPage();
            conversionPage = BuildConversionPage();
            pages.Children.Add(downloadPage);
            pages.Children.Add(conversionPage);
            Grid.SetRow(pages, 1);
            root.Children.Add(pages);

            Grid footer = new Grid { Margin = new Thickness(24, 0, 20, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            footerStatus = Text("Připraveno", 11.5, Theme.Muted);
            footerStatus.VerticalAlignment = VerticalAlignment.Center;
            footer.Children.Add(footerStatus);
            TextBlock version = Text("MV Media Downloader 3.0.1", 11.5, Theme.Muted);
            version.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(version, 1);
            footer.Children.Add(version);
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Navigate("download");
        }

        private Grid BuildTitleBar()
        {
            Grid bar = new Grid { Background = Brush("#0D1217") };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(138) });

            StackPanel brand = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            Border mark = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(7), Background = Brush("#20A4F3") };
            mark.Child = new TextBlock { Text = "MV", Foreground = Brushes.White, FontSize = 13, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            brand.Children.Add(mark);
            StackPanel brandText = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            brandText.Children.Add(new TextBlock { Text = "MV Media Downloader", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, FontSize = 15 });
            brandText.Children.Add(new TextBlock { Text = "download & convert", Foreground = Brush("#82909B"), FontSize = 10.5 });
            brand.Children.Add(brandText);
            bar.Children.Add(brand);

            Border navigation = new Border { Background = Brush("#171E24"), CornerRadius = new CornerRadius(7), Padding = new Thickness(4), VerticalAlignment = VerticalAlignment.Center, Height = 42 };
            Grid navGrid = new Grid();
            navGrid.ColumnDefinitions.Add(new ColumnDefinition());
            navGrid.ColumnDefinitions.Add(new ColumnDefinition());
            downloadNavButton = CreateNavButton("\uE896", "Stahování");
            conversionNavButton = CreateNavButton("\uE895", "Konverze");
            downloadNavButton.Click += delegate { Navigate("download"); };
            conversionNavButton.Click += delegate { Navigate("conversion"); };
            navGrid.Children.Add(downloadNavButton);
            Grid.SetColumn(conversionNavButton, 1);
            navGrid.Children.Add(conversionNavButton);
            navigation.Child = navGrid;
            Grid.SetColumn(navigation, 1);
            bar.Children.Add(navigation);

            toolStatusPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 12, 0) };
            Grid.SetColumn(toolStatusPanel, 3);
            bar.Children.Add(toolStatusPanel);
            RenderToolStatus();

            repairButton = CreateTitleButton("\uE90F", "Nástroje");
            repairButton.Margin = new Thickness(0, 11, 10, 11);
            repairMenu = BuildRepairMenu();
            repairButton.Click += delegate
            {
                repairMenu.PlacementTarget = repairButton;
                repairMenu.IsOpen = true;
            };
            Grid.SetColumn(repairButton, 4);
            bar.Children.Add(repairButton);

            Grid windowButtons = new Grid();
            windowButtons.ColumnDefinitions.Add(new ColumnDefinition());
            windowButtons.ColumnDefinitions.Add(new ColumnDefinition());
            windowButtons.ColumnDefinitions.Add(new ColumnDefinition());
            Button minimize = CreateWindowButton("—", false);
            Button maximize = CreateWindowButton("□", false);
            Button close = CreateWindowButton("×", true);
            minimize.Click += delegate { WindowState = WindowState.Minimized; };
            maximize.Click += delegate { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; };
            close.Click += delegate { Close(); };
            windowButtons.Children.Add(minimize);
            Grid.SetColumn(maximize, 1);
            windowButtons.Children.Add(maximize);
            Grid.SetColumn(close, 2);
            windowButtons.Children.Add(close);
            Grid.SetColumn(windowButtons, 5);
            bar.Children.Add(windowButtons);
            return bar;
        }

        private void Navigate(string page)
        {
            currentPage = page;
            bool download = page == "download";
            downloadPage.Visibility = download ? Visibility.Visible : Visibility.Collapsed;
            conversionPage.Visibility = download ? Visibility.Collapsed : Visibility.Visible;
            SetNavSelected(downloadNavButton, download);
            SetNavSelected(conversionNavButton, !download);
            footerStatus.Text = download ? "Stahování připraveno" : "Konverze připravena";
        }

        private void SetNavSelected(Button button, bool selected)
        {
            button.Background = selected ? Brush("#20A4F3") : Brushes.Transparent;
            button.Foreground = selected ? Brushes.White : Brush("#A9B4BD");
        }

        private void ToggleTheme()
        {
            settings.Theme = IsDark ? "light" : "dark";
            Theme.Apply(this, IsDark);
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
            ApplyDownloadAdvancedMode();
        }

        private void SaveSettings()
        {
            CaptureDownloadSettings();
            CaptureConversionSettings();
            settings.Save();
        }

        private async void HandleShortcuts(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.Key == Key.F5)
            {
                await RefreshToolsAsync(true);
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
            if (activeCancellation != null && !activeCancellation.IsCancellationRequested)
                activeCancellation.Cancel();
        }

        private void SetBusy(bool value, string message)
        {
            busy = value;
            footerStatus.Text = message;
            if (repairButton != null)
                repairButton.IsEnabled = !value;
            UpdateDownloadButtons();
            UpdateConversionButtons();
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

        private Border Card(UIElement content)
        {
            Border card = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(22), Child = content };
            Theme.Bind(card, Border.BackgroundProperty, Theme.Surface);
            Theme.Bind(card, Border.BorderBrushProperty, Theme.Border);
            return card;
        }

        private TextBlock Text(string value, double size, string colorKey)
        {
            TextBlock text = new TextBlock { Text = value, FontSize = size, TextWrapping = TextWrapping.Wrap };
            Theme.Bind(text, TextBlock.ForegroundProperty, colorKey);
            return text;
        }

        private TextBlock Heading(string value, double size)
        {
            TextBlock text = Text(value, size, Theme.Text);
            text.FontWeight = FontWeights.SemiBold;
            return text;
        }

        private Button CreatePrimaryButton(string glyph, string label)
        {
            Button button = CreateActionButton(glyph, label);
            Theme.Bind(button, Control.BackgroundProperty, Theme.Primary);
            button.Foreground = Brushes.White;
            button.BorderThickness = new Thickness(0);
            return button;
        }

        private Button CreateActionButton(string glyph, string label)
        {
            Button button = new Button { Content = IconText(glyph, label), MinHeight = 40 };
            return button;
        }

        private StackPanel IconText(string glyph, string label)
        {
            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            panel.Children.Add(new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            return panel;
        }

        private Button CreateNavButton(string glyph, string label)
        {
            Button button = new Button { Content = IconText(glyph, label), Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0) };
            WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            return button;
        }

        private Button CreateTitleButton(string glyph, string label)
        {
            Button button = new Button { Content = IconText(glyph, label), Background = Brush("#171E24"), Foreground = Brushes.White, BorderBrush = Brush("#313B44"), Padding = new Thickness(13, 7, 13, 7) };
            WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            return button;
        }

        private Button CreateWindowButton(string glyph, bool danger)
        {
            Button button = new Button { Content = new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe UI"), FontSize = glyph == "×" ? 18 : 13, FontWeight = FontWeights.SemiBold }, Background = Brushes.Transparent, Foreground = Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(0) };
            if (danger)
                button.MouseEnter += delegate { button.Background = Brush("#C42B3A"); };
            button.MouseLeave += delegate { button.Background = Brushes.Transparent; };
            WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            return button;
        }

        private ComboBox Combo(params ComboItem[] items)
        {
            ComboBox combo = new ComboBox { MinHeight = 38 };
            foreach (ComboItem item in items)
                combo.Items.Add(item);
            return combo;
        }

        private Border Labeled(string label, FrameworkElement control)
        {
            StackPanel panel = new StackPanel();
            TextBlock caption = Text(label.ToUpperInvariant(), 10.5, Theme.Muted);
            caption.FontWeight = FontWeights.SemiBold;
            caption.Margin = new Thickness(0, 0, 0, 7);
            panel.Children.Add(caption);
            panel.Children.Add(control);
            return new Border { Child = panel };
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
    }
}
