using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using MVMediaStudio.Core;
using MVMediaStudio.Services;
using MVMediaStudio.UI;
using Forms = System.Windows.Forms;

namespace MVMediaStudio
{
    internal partial class MainWindow
    {
        private DataGrid conversionGrid;
        private FrameworkElement conversionEmptyPanel;
        private TextBlock conversionCount;
        private ComboBox conversionFormatCombo;
        private ComboBox conversionCodecCombo;
        private ComboBox conversionRateCombo;
        private ComboBox conversionCrfCombo;
        private ComboBox conversionVideoBitrateCombo;
        private ComboBox conversionAudioCodecCombo;
        private ComboBox conversionAudioBitrateCombo;
        private TextBox conversionFolderBox;
        private Button conversionStartButton;
        private Button conversionCancelButton;
        private Button conversionRemoveButton;
        private Button conversionClearButton;
        private Button conversionReportButton;
        private Button conversionLogToggle;
        private Border conversionLogCard;
        private TextBox conversionLogBox;
        private ProgressBar conversionOverallProgress;
        private TextBlock conversionStatusTitle;
        private TextBlock conversionStatusDetail;
        private StackPanel conversionContent;
        private FrameworkElement conversionCodecField;
        private FrameworkElement conversionCodecNoticePanel;

        private Grid BuildConversionPage()
        {
            Grid page = new Grid();
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            ConfigurePageScroll(scroll);
            conversionContent = new StackPanel
            {
                Margin = new Thickness(32, 28, 32, 34),
                MaxWidth = 1560,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = conversionContent;
            page.Children.Add(scroll);

            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel title = new StackPanel();
            title.Children.Add(Heading("Převést soubory", 27));
            TextBlock subtitle = Text("Sjednoť až 20 videí do jednoho formátu a kodeku.", 13, Theme.Muted);
            subtitle.Margin = new Thickness(0, 5, 0, 0);
            title.Children.Add(subtitle);
            header.Children.Add(title);
            Button recommended = CreateActionButton("\uE73E", "Doporučené nastavení");
            recommended.VerticalAlignment = VerticalAlignment.Center;
            recommended.Click += delegate { ResetConversionChoices(); };
            Grid.SetColumn(recommended, 1);
            header.Children.Add(recommended);
            conversionContent.Children.Add(header);

            StackPanel filesPanel = new StackPanel();
            Grid filesHeader = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            filesHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            filesHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            filesHeader.Children.Add(Heading("Fronta souborů", 17));
            conversionCount = Text("0 / 20", 11.5, Theme.Muted);
            conversionCount.FontWeight = FontWeights.SemiBold;
            conversionCount.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(conversionCount, 1);
            filesHeader.Children.Add(conversionCount);
            filesPanel.Children.Add(filesHeader);

            Grid listHost = new Grid { MinHeight = 230, AllowDrop = true };
            listHost.PreviewDragOver += ConversionDragOver;
            listHost.Drop += async delegate(object sender, DragEventArgs eventArgs) { await ConversionDropAsync(eventArgs); };
            conversionGrid = new DataGrid
            {
                ItemsSource = conversionJobs,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserResizeRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                RowHeight = 46,
                MinHeight = 230,
                MaxHeight = 520,
                BorderThickness = new Thickness(1),
                IsReadOnly = true,
                EnableRowVirtualization = true,
                EnableColumnVirtualization = true
            };
            conversionGrid.AlternationCount = 2;
            Theme.Bind(conversionGrid, Control.BackgroundProperty, Theme.Input);
            Theme.Bind(conversionGrid, Control.ForegroundProperty, Theme.Text);
            Theme.Bind(conversionGrid, Control.BorderBrushProperty, Theme.Border);
            conversionGrid.ColumnHeaderStyle = CreateDataGridHeaderStyle();
            conversionGrid.RowStyle = CreateDataGridRowStyle();
            conversionGrid.CellStyle = CreateDataGridCellStyle();
            conversionGrid.SelectionChanged += delegate { UpdateConversionButtons(); };
            conversionGrid.Columns.Add(new DataGridTextColumn { Header = "Soubor", Binding = new Binding("FileName"), Width = new DataGridLength(2.2, DataGridLengthUnitType.Star) });
            conversionCodecColumn = new DataGridTextColumn { Header = "Zdrojový kodek", Binding = new Binding("CodecDetails"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) };
            conversionGrid.Columns.Add(conversionCodecColumn);
            conversionGrid.Columns.Add(new DataGridTextColumn { Header = "Stav", Binding = new Binding("Status"), Width = new DataGridLength(1.1, DataGridLengthUnitType.Star) });
            conversionGrid.Columns.Add(CreateProgressColumn());
            listHost.Children.Add(conversionGrid);

            StackPanel empty = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
            empty.Children.Add(new TextBlock { Text = "\uE710", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brush("#60707E") });
            TextBlock emptyTitle = Heading("Přetáhni sem videa", 15);
            emptyTitle.HorizontalAlignment = HorizontalAlignment.Center;
            emptyTitle.Margin = new Thickness(0, 9, 0, 3);
            empty.Children.Add(emptyTitle);
            TextBlock emptyHint = Text("nebo použij tlačítko Přidat soubory", 11.5, Theme.Muted);
            emptyHint.HorizontalAlignment = HorizontalAlignment.Center;
            empty.Children.Add(emptyHint);
            conversionEmptyPanel = empty;
            listHost.Children.Add(empty);
            filesPanel.Children.Add(listHost);

            StackPanel fileActions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            Button add = CreateActionButton("\uE710", "Přidat soubory");
            add.Click += async delegate { await BrowseConversionFilesAsync(); };
            conversionRemoveButton = CreateActionButton("\uE74D", "Odebrat vybraný");
            conversionRemoveButton.Margin = new Thickness(8, 0, 0, 0);
            conversionRemoveButton.Click += delegate { RemoveSelectedConversionJob(); };
            conversionClearButton = CreateActionButton("\uE894", "Vyčistit frontu");
            conversionClearButton.Margin = new Thickness(8, 0, 0, 0);
            conversionClearButton.Click += delegate { conversionJobs.Clear(); UpdateConversionQueue(); };
            fileActions.Children.Add(add);
            fileActions.Children.Add(conversionRemoveButton);
            fileActions.Children.Add(conversionClearButton);
            filesPanel.Children.Add(fileActions);
            conversionContent.Children.Add(Card(filesPanel));

            StackPanel outputPanel = new StackPanel();
            outputPanel.Children.Add(Heading("Výstup", 17));
            AdaptiveGrid outputChoices = new AdaptiveGrid
            {
                Margin = new Thickness(0, 16, 0, 0),
                ItemMinWidth = 285,
                MaximumColumns = 3,
                ColumnSpacing = 12,
                RowSpacing = 14
            };

            conversionFormatCombo = Combo(
                new ComboItem("mp4", "MP4 · nejběžnější"),
                new ComboItem("mkv", "MKV · flexibilní"),
                new ComboItem("webm", "WebM · web"),
                new ComboItem("mov", "MOV · editace"),
                new ComboItem("avi", "AVI · starší zařízení"));
            SelectCombo(conversionFormatCombo, settings.ConversionFormat);
            conversionFormatCombo.SelectionChanged += delegate { EnsureCompatibleConversionChoice(); };
            outputChoices.Children.Add(Labeled("Formát", conversionFormatCombo));

            conversionCodecCombo = Combo(
                new ComboItem("h264", "H.264 · kompatibilní"),
                new ComboItem("h265", "H.265 / HEVC · menší soubor"),
                new ComboItem("av1", "AV1 · moderní, pomalejší"));
            SelectCombo(conversionCodecCombo, settings.ConversionCodec);
            conversionCodecCombo.SelectionChanged += delegate { EnsureCompatibleConversionChoice(); };
            Border codec = Labeled("Video kodek", conversionCodecCombo);
            conversionCodecField = codec;
            outputChoices.Children.Add(codec);

            Grid folderGrid = new Grid();
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            conversionFolderBox = new TextBox { Text = settings.ConversionDirectory, IsReadOnly = true, MinHeight = 38, VerticalContentAlignment = VerticalAlignment.Center };
            folderGrid.Children.Add(conversionFolderBox);
            Button browseFolder = CreateActionButton("\uE8B7", "Vybrat");
            browseFolder.Margin = new Thickness(8, 0, 0, 0);
            browseFolder.Click += delegate { BrowseConversionFolder(); };
            Grid.SetColumn(browseFolder, 1);
            folderGrid.Children.Add(browseFolder);
            Button openFolder = CreateIconButton("\uE838", "Otevřít výstupní složku");
            openFolder.Margin = new Thickness(6, 0, 0, 0);
            openFolder.Click += delegate { OpenDirectory(conversionFolderBox.Text); };
            Grid.SetColumn(openFolder, 2);
            folderGrid.Children.Add(openFolder);
            Border folder = Labeled("Cílová složka", folderGrid);
            outputChoices.Children.Add(folder);
            outputPanel.Children.Add(outputChoices);

            AdaptiveGrid codecNotice = new AdaptiveGrid
            {
                Margin = new Thickness(0, 18, 0, 0),
                ItemMinWidth = 245,
                MaximumColumns = 3,
                ColumnSpacing = 8,
                RowSpacing = 8
            };
            codecNotice.Children.Add(CodecNotice("H.264", "Nejkompatibilnější s většinou zařízení.", Theme.Success));
            Border h265 = CodecNotice("H.265", "Některé starší televize ho nepřehrají.", Theme.Warning);
            codecNotice.Children.Add(h265);
            Border av1 = CodecNotice("AV1", "Na velké části starších zařízení nepůjde.", Theme.Danger);
            codecNotice.Children.Add(av1);
            outputPanel.Children.Add(codecNotice);
            conversionCodecNoticePanel = codecNotice;

            Border outputCard = Card(outputPanel);
            outputCard.Margin = new Thickness(0, 14, 0, 0);
            conversionContent.Children.Add(outputCard);

            StackPanel advancedPanel = new StackPanel();
            advancedPanel.Children.Add(Heading("Pokročilé řízení kvality", 15));
            AdaptiveGrid advancedChoices = new AdaptiveGrid
            {
                Margin = new Thickness(0, 14, 0, 0),
                ItemMinWidth = 190,
                MaximumColumns = 5,
                ColumnSpacing = 10,
                RowSpacing = 12
            };
            conversionRateCombo = Combo(new ComboItem("crf", "CRF · stálá kvalita"), new ComboItem("bitrate", "Pevný bitrate"));
            conversionRateCombo.SelectedIndex = 0;
            conversionRateCombo.SelectionChanged += delegate { UpdateRateControlVisibility(); };
            advancedChoices.Children.Add(Labeled("Řízení kvality", conversionRateCombo));
            conversionCrfCombo = Combo(new ComboItem("18", "18 · vysoká"), new ComboItem("20", "20 · velmi dobrá"), new ComboItem("23", "23 · doporučená"), new ComboItem("28", "28 · menší soubor"));
            SelectCombo(conversionCrfCombo, "23");
            Border crf = Labeled("CRF", conversionCrfCombo);
            advancedChoices.Children.Add(crf);
            conversionVideoBitrateCombo = Combo(new ComboItem("2500k", "2,5 Mb/s"), new ComboItem("4000k", "4 Mb/s"), new ComboItem("6000k", "6 Mb/s"), new ComboItem("8000k", "8 Mb/s"), new ComboItem("12000k", "12 Mb/s"), new ComboItem("20000k", "20 Mb/s"));
            SelectCombo(conversionVideoBitrateCombo, "6000k");
            Border videoRate = Labeled("Video bitrate", conversionVideoBitrateCombo);
            advancedChoices.Children.Add(videoRate);
            conversionAudioBitrateCombo = Combo(new ComboItem("128k", "128 kb/s"), new ComboItem("192k", "192 kb/s"), new ComboItem("256k", "256 kb/s"), new ComboItem("320k", "320 kb/s"));
            SelectCombo(conversionAudioBitrateCombo, "192k");
            conversionAudioCodecCombo = Combo(
                new ComboItem("aac", "AAC · kompatibilní"),
                new ComboItem("mp3", "MP3 · univerzální"),
                new ComboItem("opus", "Opus · efektivní"),
                new ComboItem("flac", "FLAC · bezztrátový"));
            SelectCombo(conversionAudioCodecCombo, settings.ConversionAudioCodec);
            conversionAudioCodecCombo.SelectionChanged += delegate { EnsureCompatibleConversionChoice(); UpdateRateControlVisibility(); };
            Border audioCodec = Labeled("Zvuk kodek", conversionAudioCodecCombo);
            advancedChoices.Children.Add(audioCodec);
            Border audioRate = Labeled("Zvuk bitrate", conversionAudioBitrateCombo);
            advancedChoices.Children.Add(audioRate);
            advancedPanel.Children.Add(advancedChoices);
            Border advancedCard = Card(advancedPanel);
            advancedCard.Margin = new Thickness(0, 14, 0, 0);
            conversionAdvancedPanel = advancedCard;
            conversionContent.Children.Add(advancedCard);
            UpdateRateControlVisibility();

            Grid actions = new Grid { Margin = new Thickness(0, 18, 0, 0) };
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            conversionStartButton = CreatePrimaryButton("\uE768", "Spustit konverzi");
            conversionStartButton.MinWidth = 170;
            conversionStartButton.Click += async delegate { await StartConversionAsync(); };
            actions.Children.Add(conversionStartButton);
            conversionCancelButton = CreateActionButton("\uE71A", "Zrušit");
            conversionCancelButton.Margin = new Thickness(8, 0, 0, 0);
            conversionCancelButton.Click += delegate { CancelActiveWork(); };
            Grid.SetColumn(conversionCancelButton, 1);
            actions.Children.Add(conversionCancelButton);
            conversionReportButton = CreateActionButton("\uE8BD", "Nahlásit chybu");
            conversionReportButton.Margin = new Thickness(0, 0, 8, 0);
            conversionReportButton.Visibility = Visibility.Collapsed;
            conversionReportButton.Click += delegate { SaveProblemReport("Konverze", conversionLog.ToString()); };
            Grid.SetColumn(conversionReportButton, 3);
            actions.Children.Add(conversionReportButton);
            conversionLogToggle = CreateActionButton("\uE756", "Zobrazit log");
            conversionLogToggle.Click += delegate { ToggleConversionLog(); };
            Grid.SetColumn(conversionLogToggle, 4);
            actions.Children.Add(conversionLogToggle);
            conversionContent.Children.Add(actions);

            Grid progressPanel = new Grid();
            progressPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            progressPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            progressPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            conversionStatusTitle = Heading("Fronta je prázdná", 15);
            progressPanel.Children.Add(conversionStatusTitle);
            conversionStatusDetail = Text("Přidej jeden nebo více souborů.", 11.5, Theme.Muted);
            conversionStatusDetail.Margin = new Thickness(0, 4, 0, 12);
            Grid.SetRow(conversionStatusDetail, 1);
            progressPanel.Children.Add(conversionStatusDetail);
            conversionOverallProgress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0 };
            Grid.SetRow(conversionOverallProgress, 2);
            progressPanel.Children.Add(conversionOverallProgress);
            Border progressCard = Card(progressPanel);
            progressCard.Margin = new Thickness(0, 14, 0, 0);
            conversionContent.Children.Add(progressCard);

            conversionLogBox = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, MinHeight = 180, MaxHeight = 260, FontFamily = new FontFamily("Consolas"), FontSize = 11.5 };
            Theme.Bind(conversionLogBox, Control.BackgroundProperty, Theme.Console);
            Theme.Bind(conversionLogBox, Control.ForegroundProperty, Theme.ConsoleText);
            conversionLogCard = Card(conversionLogBox);
            conversionLogCard.Margin = new Thickness(0, 14, 0, 0);
            conversionLogCard.Visibility = Visibility.Collapsed;
            conversionContent.Children.Add(conversionLogCard);

            UpdateConversionQueue();
            EnsureCompatibleConversionChoice();
            return page;
        }

        private DataGridTemplateColumn CreateProgressColumn()
        {
            DataTemplate template = new DataTemplate();
            FrameworkElementFactory grid = new FrameworkElementFactory(typeof(Grid));
            FrameworkElementFactory bar = new FrameworkElementFactory(typeof(ProgressBar));
            bar.SetValue(ProgressBar.MinimumProperty, 0d);
            bar.SetValue(ProgressBar.MaximumProperty, 100d);
            bar.SetValue(FrameworkElement.HeightProperty, 6d);
            bar.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bar.SetBinding(ProgressBar.ValueProperty, new Binding("Progress"));
            grid.AppendChild(bar);
            FrameworkElementFactory label = new FrameworkElementFactory(typeof(TextBlock));
            label.SetBinding(TextBlock.TextProperty, new Binding("ProgressText"));
            label.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            label.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top);
            label.SetValue(TextBlock.FontSizeProperty, 10d);
            label.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 16));
            grid.AppendChild(label);
            template.VisualTree = grid;
            return new DataGridTemplateColumn { Header = "Průběh", CellTemplate = template, Width = new DataGridLength(1.2, DataGridLengthUnitType.Star) };
        }

        private Style CreateDataGridHeaderStyle()
        {
            Style style = new Style(typeof(DataGridColumnHeader));
            style.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension(Theme.SurfaceAlt)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension(Theme.Muted)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, new DynamicResourceExtension(Theme.Border)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 9, 12, 9)));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 10.5d));
            style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            return style;
        }

        private Style CreateDataGridRowStyle()
        {
            Style style = new Style(typeof(DataGridRow));
            style.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension(Theme.Input)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension(Theme.Text)));
            Trigger alternate = new Trigger { Property = ItemsControl.AlternationIndexProperty, Value = 1 };
            alternate.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension(Theme.SurfaceAlt)));
            style.Triggers.Add(alternate);
            Trigger selected = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            selected.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension(Theme.Primary)));
            selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            style.Triggers.Add(selected);
            return style;
        }

        private Style CreateDataGridCellStyle()
        {
            Style style = new Style(typeof(DataGridCell));
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, new DynamicResourceExtension(Theme.Border)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 6, 12, 6)));
            Trigger focused = new Trigger { Property = DataGridCell.IsKeyboardFocusWithinProperty, Value = true };
            focused.Setters.Add(new Setter(Control.BorderBrushProperty, new DynamicResourceExtension(Theme.Primary)));
            style.Triggers.Add(focused);
            return style;
        }

        private Border CodecNotice(string title, string detail, string colorKey)
        {
            StackPanel panel = new StackPanel();
            TextBlock heading = Text(title, 11.5, colorKey);
            heading.FontWeight = FontWeights.Bold;
            panel.Children.Add(heading);
            TextBlock description = Text(detail, 10.5, Theme.Muted);
            description.Margin = new Thickness(0, 3, 0, 0);
            panel.Children.Add(description);
            Border border = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Child = panel };
            Theme.Bind(border, Border.BackgroundProperty, Theme.SurfaceAlt);
            return border;
        }

        private async Task BrowseConversionFilesAsync()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Vyber soubory ke konverzi",
                Filter = MediaFileSupport.VideoDialogFilter
            };
            if (dialog.ShowDialog(this) == true)
                await AddConversionFilesAsync(dialog.FileNames);
        }

        private async Task AddConversionFilesAsync(IEnumerable<string> paths)
        {
            List<ConversionJob> added = new List<ConversionJob>();
            int unsupportedCount = 0;
            foreach (string path in paths ?? Enumerable.Empty<string>())
            {
                if (conversionJobs.Count >= 20)
                    break;
                if (!File.Exists(path))
                    continue;
                if (!MediaFileSupport.IsSupportedVideo(path))
                {
                    unsupportedCount++;
                    continue;
                }
                if (conversionJobs.Any(job => string.Equals(job.SourcePath, path, StringComparison.OrdinalIgnoreCase)))
                    continue;
                ConversionJob item = new ConversionJob(path);
                conversionJobs.Add(item);
                added.Add(item);
            }
            UpdateConversionQueue();

            if (!busy && unsupportedCount > 0)
            {
                conversionStatusTitle.Text = added.Count > 0 ? "Videa byla přidána" : "Soubor nelze přidat";
                conversionStatusDetail.Text = unsupportedCount == 1
                    ? "Konverze nyní přijímá běžné video soubory, nikoli samostatný zvuk."
                    : unsupportedCount + " souborů bylo přeskočeno, protože nejde o podporovaná videa.";
            }

            if (added.Count == 0)
                return;
            if (!tools.HasFfprobe)
                await RefreshToolsAsync(false);

            foreach (ConversionJob item in added)
            {
                if (!tools.HasFfprobe)
                {
                    item.CodecDetails = "FFprobe není dostupný";
                    continue;
                }
                item.CodecDetails = "Analyzuji…";
                try
                {
                    MediaInfo media = await Task.Run(delegate { return MediaProbeService.Probe(tools.FfprobePath, item.SourcePath); });
                    item.Media = media;
                    item.CodecDetails = media.TechnicalSummary;
                }
                catch (Exception error)
                {
                    item.CodecDetails = "Analýza se nepovedla";
                    AppPaths.WriteError(error);
                }
            }
        }

        private void ConversionDragOver(object sender, DragEventArgs eventArgs)
        {
            eventArgs.Effects = eventArgs.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            eventArgs.Handled = true;
        }

        private async Task ConversionDropAsync(DragEventArgs eventArgs)
        {
            if (!eventArgs.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            string[] paths = eventArgs.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths != null)
                await AddConversionFilesAsync(paths);
        }

        private void RemoveSelectedConversionJob()
        {
            ConversionJob item = conversionGrid.SelectedItem as ConversionJob;
            if (item != null)
                conversionJobs.Remove(item);
            UpdateConversionQueue();
        }

        private void UpdateConversionQueue()
        {
            if (conversionCount == null)
                return;
            conversionCount.Text = conversionJobs.Count + " / 20";
            conversionEmptyPanel.Visibility = conversionJobs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (!busy)
            {
                conversionStatusTitle.Text = conversionJobs.Count == 0 ? "Fronta je prázdná" : "Připraveno ke konverzi";
                conversionStatusDetail.Text = conversionJobs.Count == 0 ? "Přidej jeden nebo více souborů." : conversionJobs.Count + " souborů čeká ve frontě.";
            }
            UpdateConversionButtons();
        }

        private async Task StartConversionAsync()
        {
            if (busy || conversionJobs.Count == 0)
                return;
            if (!await EnsureFfmpegAsync())
                return;

            CaptureConversionSettings();
            Directory.CreateDirectory(settings.ConversionDirectory);
            activeCancellation = new CancellationTokenSource();
            activeOperation = "conversion";
            conversionLog.Clear();
            conversionLogBox.Clear();
            conversionReportButton.Visibility = Visibility.Collapsed;
            conversionOverallProgress.Value = 0;
            SetBusy(true, "Probíhá konverze");
            SetConversionStatus("Konverze spuštěna", "Zpracovávám frontu souborů…", Theme.Primary);
            int errors = 0;

            for (int index = 0; index < conversionJobs.Count; index++)
            {
                ConversionJob item = conversionJobs[index];
                if (activeCancellation.IsCancellationRequested)
                {
                    item.Status = "Zrušeno";
                    continue;
                }

                item.Status = "Konvertuji";
                item.Progress = 0;
                ConversionOptions options = CurrentConversionOptions(item.SourcePath);
                string outputPath;
                List<string> arguments;
                try
                {
                    arguments = ConversionArgumentBuilder.Build(options, out outputPath);
                }
                catch (Exception error)
                {
                    item.Status = "Chyba";
                    errors++;
                    AppendConversionLog(error.Message);
                    continue;
                }

                AppendConversionLog("$ ffmpeg " + ArgumentUtilities.Join(arguments));
                int itemIndex = index;
                int exitCode = -1;
                try
                {
                    exitCode = await ProcessService.RunAsync(
                        tools.FfmpegPath,
                        arguments,
                        delegate(string line, bool isError) { HandleConversionLine(item, itemIndex, line, isError); },
                        activeCancellation.Token);
                }
                catch (Exception error)
                {
                    AppPaths.WriteError(error);
                    AppendConversionLog(error.ToString());
                }

                if (exitCode == 0)
                {
                    item.Progress = 100;
                    item.Status = "Hotovo";
                    AppendConversionLog("[Hotovo] " + item.FileName);
                }
                else if (exitCode == -2)
                {
                    item.Status = "Zrušeno";
                    break;
                }
                else
                {
                    item.Status = "Chyba";
                    errors++;
                    AppendConversionLog("[Chyba] " + item.FileName);
                }
                conversionOverallProgress.Value = ((index + 1d) / conversionJobs.Count) * 100d;
            }

            bool cancelled = activeCancellation.IsCancellationRequested;
            activeCancellation = null;
            activeOperation = "";
            if (cancelled)
            {
                SetConversionStatus("Konverze zrušena", "Rozpracovaná operace byla zastavena.", Theme.Warning);
                SetBusy(false, "Konverze zrušena");
            }
            else if (errors > 0)
            {
                SetConversionStatus("Dokončeno s chybami", errors + " souborů se nepodařilo převést. Podrobnosti jsou v logu.", Theme.Danger);
                SetBusy(false, "Konverze dokončena s chybami");
                ShowConversionLog();
                conversionReportButton.Visibility = Visibility.Visible;
            }
            else
            {
                conversionOverallProgress.Value = 100;
                SetConversionStatus("Konverze dokončena", "Všechny soubory jsou připravené ve výstupní složce.", Theme.Success);
                SetBusy(false, "Konverze dokončena");
            }
            SaveLog(AppPaths.ConversionLogPath, conversionLog.ToString());
        }

        private void HandleConversionLine(ConversionJob item, int itemIndex, string line, bool isError)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (!IsConversionProgressLine(line))
                    AppendConversionLog((isError ? "! " : "") + line);
                if (line.StartsWith("out_time_ms=", StringComparison.OrdinalIgnoreCase) || line.StartsWith("out_time_us=", StringComparison.OrdinalIgnoreCase))
                {
                    int split = line.IndexOf('=');
                    double microseconds;
                    if (split > 0 && double.TryParse(line.Substring(split + 1), out microseconds) && item.Media != null && item.Media.DurationSeconds > 0)
                    {
                        item.Progress = Math.Max(0, Math.Min(99, (microseconds / 1000000d) / item.Media.DurationSeconds * 100d));
                        conversionOverallProgress.Value = ((itemIndex + item.Progress / 100d) / conversionJobs.Count) * 100d;
                        SetConversionStatus("Konvertuji " + (itemIndex + 1) + " z " + conversionJobs.Count, item.FileName + " · " + item.Progress.ToString("0") + " %", Theme.Primary);
                    }
                }
            }));
        }

        private static bool IsConversionProgressLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return true;
            int split = line.IndexOf('=');
            if (split <= 0)
                return false;
            for (int index = 0; index < split; index++)
            {
                char value = line[index];
                if (!(char.IsLetterOrDigit(value) || value == '_'))
                    return false;
            }
            return true;
        }

        private ConversionOptions CurrentConversionOptions(string inputPath)
        {
            return new ConversionOptions
            {
                InputPath = inputPath,
                OutputDirectory = settings.ConversionDirectory,
                Format = ComboValue(conversionFormatCombo, "mp4"),
                Codec = ComboValue(conversionCodecCombo, "h264"),
                RateControl = ComboValue(conversionRateCombo, "crf"),
                Crf = ComboValue(conversionCrfCombo, "23"),
                VideoBitrate = ComboValue(conversionVideoBitrateCombo, "6000k"),
                AudioCodec = ComboValue(conversionAudioCodecCombo, "aac"),
                AudioBitrate = ComboValue(conversionAudioBitrateCombo, "192k")
            };
        }

        private void EnsureCompatibleConversionChoice()
        {
            if (conversionFormatCombo == null || conversionCodecCombo == null)
                return;
            string format = ComboValue(conversionFormatCombo, "mp4");
            string codec = ComboValue(conversionCodecCombo, "h264");
            string audioCodec = conversionAudioCodecCombo == null ? "aac" : ComboValue(conversionAudioCodecCombo, "aac");
            if (format == "webm" && codec != "av1")
                SelectCombo(conversionCodecCombo, "av1");
            else if ((format == "avi" || format == "mov") && codec == "av1")
                SelectCombo(conversionCodecCombo, "h264");
            else if (format == "avi" && codec == "h265")
                SelectCombo(conversionCodecCombo, "h264");
            if (conversionAudioCodecCombo == null)
                return;
            if (format == "webm" && audioCodec != "opus")
                SelectCombo(conversionAudioCodecCombo, "opus");
            else if (format == "avi" && audioCodec != "mp3")
                SelectCombo(conversionAudioCodecCombo, "mp3");
            else if ((format == "mp4" || format == "mov") && (audioCodec == "opus" || audioCodec == "flac"))
                SelectCombo(conversionAudioCodecCombo, "aac");
        }

        private void ResetConversionChoices()
        {
            SelectCombo(conversionFormatCombo, "mp4");
            SelectCombo(conversionCodecCombo, "h264");
            SelectCombo(conversionAudioCodecCombo, "aac");
            SelectCombo(conversionRateCombo, "crf");
            SelectCombo(conversionCrfCombo, "23");
            SelectCombo(conversionAudioBitrateCombo, "192k");
            SetConversionStatus("Doporučené nastavení", "MP4, H.264 a CRF 23 fungují na většině zařízení.", Theme.Success);
        }

        private void UpdateRateControlVisibility()
        {
            if (conversionRateCombo == null)
                return;
            bool bitrate = ComboValue(conversionRateCombo, "crf") == "bitrate";
            conversionCrfCombo.IsEnabled = !bitrate;
            conversionVideoBitrateCombo.IsEnabled = bitrate;
            if (conversionAudioBitrateCombo != null)
                conversionAudioBitrateCombo.IsEnabled = conversionAudioCodecCombo == null || ComboValue(conversionAudioCodecCombo, "aac") != "flac";
        }

        private void BrowseConversionFolder()
        {
            using (Forms.FolderBrowserDialog dialog = new Forms.FolderBrowserDialog())
            {
                dialog.Description = "Vyber cílovou složku pro převedené soubory";
                dialog.SelectedPath = conversionFolderBox.Text;
                if (dialog.ShowDialog() == Forms.DialogResult.OK)
                    conversionFolderBox.Text = dialog.SelectedPath;
            }
        }

        private void CaptureConversionSettings()
        {
            if (conversionFormatCombo == null)
                return;
            settings.ConversionFormat = ComboValue(conversionFormatCombo, "mp4");
            settings.ConversionCodec = ComboValue(conversionCodecCombo, "h264");
            settings.ConversionAudioCodec = ComboValue(conversionAudioCodecCombo, "aac");
            settings.ConversionDirectory = string.IsNullOrWhiteSpace(conversionFolderBox.Text) ? AppPaths.DefaultDownloadDirectory : conversionFolderBox.Text;
        }

        private void SetConversionStatus(string title, string detail, string colorKey)
        {
            conversionStatusTitle.Text = title;
            Theme.Bind(conversionStatusTitle, TextBlock.ForegroundProperty, colorKey);
            conversionStatusDetail.Text = detail;
        }

        private void AppendConversionLog(string line)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(delegate { AppendConversionLog(line); }));
                return;
            }
            conversionLog.AppendLine(line);
            if (conversionLog.Length > 180000)
            {
                conversionLog.Remove(0, conversionLog.Length - 150000);
                conversionLogBox.Text = conversionLog.ToString();
            }
            else
            {
                conversionLogBox.AppendText(line + Environment.NewLine);
            }
            conversionLogBox.ScrollToEnd();
        }

        private void ToggleConversionLog()
        {
            if (conversionLogCard.Visibility == Visibility.Visible)
            {
                conversionLogCard.Visibility = Visibility.Collapsed;
                conversionLogToggle.Content = IconText("\uE756", "Zobrazit log");
            }
            else
            {
                ShowConversionLog();
            }
        }

        private void ShowConversionLog()
        {
            conversionLogCard.Visibility = Visibility.Visible;
            conversionLogToggle.Content = IconText("\uE70D", "Skrýt log");
        }

        private void UpdateConversionButtons()
        {
            if (conversionStartButton == null)
                return;
            conversionStartButton.IsEnabled = !busy && conversionJobs.Count > 0;
            conversionCancelButton.IsEnabled = busy && activeOperation == "conversion";
            conversionRemoveButton.IsEnabled = !busy && conversionGrid.SelectedItem != null;
            conversionClearButton.IsEnabled = !busy && conversionJobs.Count > 0;
        }

        private void UpdateConversionResponsiveLayout(double windowWidth, double windowHeight)
        {
            if (conversionContent == null)
                return;

            double horizontalMargin = windowWidth >= 1700 ? 44 : windowWidth >= 1200 ? 32 : 20;
            conversionContent.Margin = new Thickness(horizontalMargin, 26, horizontalMargin, 34);
            if (conversionGrid != null)
                conversionGrid.MaxHeight = Math.Max(300, Math.Min(520, windowHeight * 0.39));
            if (conversionLogBox != null)
                conversionLogBox.MaxHeight = Math.Max(260, Math.Min(400, windowHeight * 0.42));
        }
    }
}
