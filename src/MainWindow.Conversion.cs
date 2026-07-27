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
    public partial class MainWindow
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

        private void InitializeConversionView()
        {
            conversionContent = ConversionViewControl.ConversionContent;
            conversionGrid = ConversionViewControl.ConversionGrid;
            conversionEmptyPanel = ConversionViewControl.ConversionEmptyPanel;
            conversionCount = ConversionViewControl.ConversionCount;
            conversionFormatCombo = ConversionViewControl.ConversionFormatCombo;
            conversionCodecCombo = ConversionViewControl.ConversionCodecCombo;
            conversionRateCombo = ConversionViewControl.ConversionRateCombo;
            conversionCrfCombo = ConversionViewControl.ConversionCrfCombo;
            conversionVideoBitrateCombo = ConversionViewControl.ConversionVideoBitrateCombo;
            conversionAudioCodecCombo = ConversionViewControl.ConversionAudioCodecCombo;
            conversionAudioBitrateCombo = ConversionViewControl.ConversionAudioBitrateCombo;
            conversionFolderBox = ConversionViewControl.ConversionFolderBox;
            conversionStartButton = ConversionViewControl.ConversionStartButton;
            conversionCancelButton = ConversionViewControl.ConversionCancelButton;
            conversionRemoveButton = ConversionViewControl.ConversionRemoveButton;
            conversionClearButton = ConversionViewControl.ConversionClearButton;
            conversionReportButton = ConversionViewControl.ConversionReportButton;
            conversionLogToggle = ConversionViewControl.ConversionLogToggle;
            conversionLogCard = ConversionViewControl.ConversionLogCard;
            conversionLogBox = ConversionViewControl.ConversionLogBox;
            conversionOverallProgress = ConversionViewControl.ConversionOverallProgress;
            conversionStatusTitle = ConversionViewControl.ConversionStatusTitle;
            conversionStatusDetail = ConversionViewControl.ConversionStatusDetail;
            conversionAdvancedPanel = ConversionViewControl.ConversionAdvancedPanel;
            conversionCodecColumn = ConversionViewControl.ConversionCodecColumn;
            conversionCodecField = ConversionViewControl.ConversionCodecField;
            conversionCodecNoticePanel = ConversionViewControl.ConversionCodecNoticePanel;

            ConfigurePageScroll(ConversionViewControl.PageScroll);
            conversionGrid.ItemsSource = conversionJobs;
            PopulateCombo(
                conversionFormatCombo,
                new ComboItem("mp4", "MP4 · nejběžnější"),
                new ComboItem("mkv", "MKV · flexibilní"),
                new ComboItem("webm", "WebM · web"),
                new ComboItem("mov", "MOV · editace"),
                new ComboItem("avi", "AVI · starší zařízení"));
            SelectCombo(conversionFormatCombo, settings.ConversionFormat);
            PopulateCombo(
                conversionCodecCombo,
                new ComboItem("h264", "H.264 · kompatibilní"),
                new ComboItem("h265", "H.265 / HEVC · menší soubor"),
                new ComboItem("av1", "AV1 · moderní, pomalejší"));
            SelectCombo(conversionCodecCombo, settings.ConversionCodec);
            PopulateCombo(
                conversionRateCombo,
                new ComboItem("crf", "CRF · stálá kvalita"),
                new ComboItem("bitrate", "Pevný bitrate"));
            SelectCombo(conversionRateCombo, "crf");
            PopulateCombo(
                conversionCrfCombo,
                new ComboItem("18", "18 · vysoká"),
                new ComboItem("20", "20 · velmi dobrá"),
                new ComboItem("23", "23 · doporučená"),
                new ComboItem("28", "28 · menší soubor"));
            SelectCombo(conversionCrfCombo, "23");
            PopulateCombo(
                conversionVideoBitrateCombo,
                new ComboItem("2500k", "2,5 Mb/s"),
                new ComboItem("4000k", "4 Mb/s"),
                new ComboItem("6000k", "6 Mb/s"),
                new ComboItem("8000k", "8 Mb/s"),
                new ComboItem("12000k", "12 Mb/s"),
                new ComboItem("20000k", "20 Mb/s"));
            SelectCombo(conversionVideoBitrateCombo, "6000k");
            PopulateCombo(
                conversionAudioCodecCombo,
                new ComboItem("aac", "AAC · kompatibilní"),
                new ComboItem("mp3", "MP3 · univerzální"),
                new ComboItem("opus", "Opus · efektivní"),
                new ComboItem("flac", "FLAC · bezztrátový"));
            SelectCombo(conversionAudioCodecCombo, settings.ConversionAudioCodec);
            PopulateCombo(
                conversionAudioBitrateCombo,
                new ComboItem("128k", "128 kb/s"),
                new ComboItem("192k", "192 kb/s"),
                new ComboItem("256k", "256 kb/s"),
                new ComboItem("320k", "320 kb/s"));
            SelectCombo(conversionAudioBitrateCombo, "192k");
            conversionFolderBox.Text = settings.ConversionDirectory;

            ConversionViewControl.ConversionListHost.PreviewDragOver += ConversionDragOver;
            ConversionViewControl.ConversionListHost.Drop += async delegate(
                object sender,
                DragEventArgs eventArgs)
            {
                await ConversionDropAsync(eventArgs);
            };
            conversionGrid.SelectionChanged += delegate { UpdateConversionButtons(); };
            ConversionViewControl.AddConversionFilesButton.Click += async delegate
            {
                await BrowseConversionFilesAsync();
            };
            conversionRemoveButton.Click += delegate { RemoveSelectedConversionJob(); };
            conversionClearButton.Click += delegate
            {
                conversionJobs.Clear();
                UpdateConversionQueue();
            };
            ConversionViewControl.RecommendedSettingsButton.Click += delegate
            {
                ResetConversionChoices();
            };
            conversionFormatCombo.SelectionChanged += delegate
            {
                EnsureCompatibleConversionChoice();
            };
            conversionCodecCombo.SelectionChanged += delegate
            {
                EnsureCompatibleConversionChoice();
            };
            conversionRateCombo.SelectionChanged += delegate
            {
                UpdateRateControlVisibility();
            };
            conversionAudioCodecCombo.SelectionChanged += delegate
            {
                EnsureCompatibleConversionChoice();
                UpdateRateControlVisibility();
            };
            ConversionViewControl.BrowseConversionFolderButton.Click += delegate
            {
                BrowseConversionFolder();
            };
            ConversionViewControl.OpenConversionFolderButton.Click += delegate
            {
                OpenDirectory(conversionFolderBox.Text);
            };
            conversionStartButton.Click += async delegate { await StartConversionAsync(); };
            conversionCancelButton.Click += delegate { CancelActiveWork(); };
            conversionReportButton.Click += delegate
            {
                SaveProblemReport("Konverze", conversionLog.ToString());
            };
            conversionLogToggle.Click += delegate { ToggleConversionLog(); };

            UpdateRateControlVisibility();
            UpdateConversionQueue();
            EnsureCompatibleConversionChoice();
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
            conversionCancelButton.IsEnabled = busy && activeOperation == "conversion" &&
                activeCancellation != null && !activeCancellation.IsCancellationRequested;
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
