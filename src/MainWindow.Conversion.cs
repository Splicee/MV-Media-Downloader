using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shell;
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
        private Button conversionCopyLogButton;
        private Border conversionLogCard;
        private TextBox conversionLogBox;
        private ProgressBar conversionOverallProgress;
        private TextBlock conversionStatusTitle;
        private TextBlock conversionStatusDetail;
        private StackPanel conversionContent;
        private FrameworkElement conversionCodecField;
        private FrameworkElement conversionCodecNoticePanel;
        private int conversionAnalysisCount;

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
            conversionCopyLogButton = ConversionViewControl.ConversionCopyLogButton;
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
            SelectCombo(conversionRateCombo, settings.ConversionRateControl);
            PopulateCombo(
                conversionCrfCombo,
                new ComboItem("18", "18 · vysoká"),
                new ComboItem("20", "20 · velmi dobrá"),
                new ComboItem("23", "23 · doporučená"),
                new ComboItem("28", "28 · menší soubor"));
            SelectCombo(conversionCrfCombo, settings.ConversionCrf);
            PopulateCombo(
                conversionVideoBitrateCombo,
                new ComboItem("2500k", "2,5 Mb/s"),
                new ComboItem("4000k", "4 Mb/s"),
                new ComboItem("6000k", "6 Mb/s"),
                new ComboItem("8000k", "8 Mb/s"),
                new ComboItem("12000k", "12 Mb/s"),
                new ComboItem("20000k", "20 Mb/s"));
            SelectCombo(conversionVideoBitrateCombo, settings.ConversionVideoBitrate);
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
            SelectCombo(conversionAudioBitrateCombo, settings.ConversionAudioBitrate);
            conversionFolderBox.Text = settings.ConversionDirectory;

            ConversionViewControl.ConversionListHost.PreviewDragOver += ConversionDragOver;
            ConversionViewControl.ConversionListHost.Drop += async delegate(
                object sender,
                DragEventArgs eventArgs)
            {
                await ConversionDropAsync(eventArgs);
            };
            conversionGrid.SelectionChanged += delegate { UpdateConversionButtons(); };
            conversionGrid.PreviewKeyDown += delegate (object sender, KeyEventArgs eventArgs)
            {
                if (eventArgs.Key != Key.Delete || busy)
                    return;
                RemoveSelectedConversionJobs();
                eventArgs.Handled = true;
            };
            conversionGrid.MouseDoubleClick += delegate
            {
                ConversionJob item = conversionGrid.SelectedItem as ConversionJob;
                if (item != null)
                    RevealFile(item.SourcePath);
            };
            ConversionViewControl.AddConversionFilesButton.Click += async delegate
            {
                await BrowseConversionFilesAsync();
            };
            conversionRemoveButton.Click += delegate { RemoveSelectedConversionJobs(); };
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
            conversionCopyLogButton.Click += delegate { CopyConversionLog(); };

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
            int capacitySkipped = 0;
            foreach (string path in paths ?? Enumerable.Empty<string>())
            {
                if (conversionJobs.Count >= 20)
                {
                    capacitySkipped++;
                    continue;
                }
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

            if (!busy && (unsupportedCount > 0 || capacitySkipped > 0))
            {
                conversionStatusTitle.Text = added.Count > 0 ? "Videa byla přidána" : "Soubor nelze přidat";
                List<string> details = new List<string>();
                if (unsupportedCount > 0)
                    details.Add(unsupportedCount + " nepodporovaných souborů bylo přeskočeno");
                if (capacitySkipped > 0)
                    details.Add(capacitySkipped + " souborů se nevešlo do limitu 20");
                conversionStatusDetail.Text = string.Join(" · ", details.ToArray()) + ".";
            }

            if (added.Count == 0)
                return;
            if (!tools.HasFfprobe)
                await RefreshToolsAsync(false);

            conversionAnalysisCount += added.Count;
            SetConversionStatus(
                "Analyzuji videa",
                added.Count == 1 ? added[0].FileName : "Zjišťuji kodek a délku " + added.Count + " souborů…",
                Theme.Primary);
            UpdateConversionButtons();
            SemaphoreSlim probeLimit = new SemaphoreSlim(4, 4);
            try
            {
                await Task.WhenAll(added.Select(
                    delegate (ConversionJob item) { return AnalyzeConversionItemAsync(item, probeLimit); }));
            }
            finally
            {
                probeLimit.Dispose();
                conversionAnalysisCount = Math.Max(0, conversionAnalysisCount - added.Count);
                if (!busy)
                    SetConversionStatus("Připraveno ke konverzi", conversionJobs.Count + " souborů čeká ve frontě.", Theme.Success);
                UpdateConversionButtons();
            }
        }

        private async Task AnalyzeConversionItemAsync(
            ConversionJob item,
            SemaphoreSlim probeLimit)
        {
            if (!tools.HasFfprobe)
            {
                item.CodecDetails = "FFprobe není dostupný";
                return;
            }

            item.CodecDetails = "Analyzuji…";
            await probeLimit.WaitAsync();
            try
            {
                MediaInfo media = await Task.Run(
                    delegate { return MediaProbeService.Probe(tools.FfprobePath, item.SourcePath); });
                item.Media = media;
                item.CodecDetails = media.TechnicalSummary;
            }
            catch (Exception error)
            {
                item.CodecDetails = "Analýza se nepovedla";
                AppPaths.WriteError(error);
            }
            finally
            {
                probeLimit.Release();
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

        private void RemoveSelectedConversionJobs()
        {
            List<ConversionJob> selected = conversionGrid.SelectedItems
                .OfType<ConversionJob>()
                .ToList();
            foreach (ConversionJob item in selected)
                conversionJobs.Remove(item);
            UpdateConversionQueue();
        }

        private void UpdateConversionQueue()
        {
            if (conversionCount == null)
                return;
            conversionCount.Text = conversionJobs.Count + " / 20";
            conversionEmptyPanel.Visibility = conversionJobs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ConversionViewControl.ConversionListHost.MinHeight = conversionJobs.Count == 0 ? 200 : 260;
            conversionGrid.MinHeight = conversionJobs.Count == 0 ? 200 : 260;
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
            settings.Save();
            BeginCancellableOperation("conversion");
            conversionLog.Clear();
            conversionLogBox.Clear();
            conversionReportButton.Visibility = Visibility.Collapsed;
            conversionOverallProgress.Value = 0;
            SetBusy(true, "Probíhá konverze");
            SetTaskbarProgress(0, TaskbarItemProgressState.Indeterminate);
            SetConversionStatus("Konverze spuštěna", "Zpracovávám frontu souborů…", Theme.Primary);
            foreach (ConversionJob job in conversionJobs)
            {
                job.Status = "Čeká";
                job.Progress = 0;
            }

            int errors = 0;
            int completed = 0;
            bool cancelled = false;
            Stopwatch elapsed = Stopwatch.StartNew();
            try
            {
                StorageService.EnsureWritableDirectory(settings.ConversionDirectory);
                for (int index = 0; index < conversionJobs.Count; index++)
                {
                    operationCancellation.Token.ThrowIfCancellationRequested();
                    ConversionJob item = conversionJobs[index];
                    item.Status = "Konvertuji";
                    item.Progress = 0;
                    ConversionOptions options = CurrentConversionOptions(item.SourcePath);
                    string outputPath = "";
                    List<string> arguments;
                    try
                    {
                        arguments = ConversionArgumentBuilder.Build(options, out outputPath);
                    }
                    catch (Exception error)
                    {
                        item.Status = "Chyba";
                        errors++;
                        AppendConversionLog("[Chyba] " + item.FileName + " · " + error.Message);
                        continue;
                    }

                    AppendConversionLog("$ ffmpeg " + ArgumentUtilities.Join(arguments));
                    int itemIndex = index;
                    int exitCode;
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
                        exitCode = -1;
                    }

                    if (exitCode == -2)
                    {
                        StorageService.DeleteIncompleteFile(outputPath);
                        item.Status = "Zrušeno";
                        throw new OperationCanceledException(operationCancellation.Token);
                    }
                    if (exitCode != 0 || !File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
                    {
                        StorageService.DeleteIncompleteFile(outputPath);
                        item.Status = "Chyba";
                        errors++;
                        AppendConversionLog("[Chyba] " + item.FileName + " · FFmpeg nevytvořil platný výsledek.");
                    }
                    else
                    {
                        item.Progress = 100;
                        item.Status = "Hotovo";
                        completed++;
                        AppendConversionLog("[Hotovo] " + outputPath);
                    }
                    conversionOverallProgress.Value = ((index + 1d) / conversionJobs.Count) * 100d;
                    SetTaskbarProgress(conversionOverallProgress.Value, TaskbarItemProgressState.Normal);
                }
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                foreach (ConversionJob item in conversionJobs)
                {
                    if (item.Status != "Hotovo" && item.Status != "Chyba")
                        item.Status = "Zrušeno";
                }
            }
            catch (Exception error)
            {
                errors++;
                AppPaths.WriteError(error);
                AppendConversionLog("! " + error.Message);
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
                EndCancellableOperation();
                if (cancelled)
                {
                    SetBusy(false, "Konverze zrušena");
                    SetConversionStatus(
                        "Konverze zrušena",
                        completed + " souborů bylo dokončeno, rozpracovaný výstup byl odstraněn.",
                        Theme.Warning);
                    SetTaskbarProgress(conversionOverallProgress.Value, TaskbarItemProgressState.Paused);
                }
                else if (errors > 0)
                {
                    SetBusy(false, "Konverze dokončena s chybami");
                    SetConversionStatus(
                        "Dokončeno s chybami",
                        completed + " hotovo, " + errors + " chyb · " + FormatElapsed(elapsed.Elapsed) + ". Podrobnosti jsou v logu.",
                        Theme.Danger);
                    SetTaskbarProgress(Math.Max(1, conversionOverallProgress.Value), TaskbarItemProgressState.Error);
                    ShowConversionLog();
                    conversionReportButton.Visibility = Visibility.Visible;
                }
                else
                {
                    conversionOverallProgress.Value = 100;
                    SetBusy(false, "Konverze dokončena");
                    SetConversionStatus(
                        "Konverze dokončena",
                        completed + " souborů je připraveno · " + FormatElapsed(elapsed.Elapsed) + ".",
                        Theme.Success);
                    SetTaskbarProgress(0, TaskbarItemProgressState.None);
                }
                SaveLog(AppPaths.ConversionLogPath, conversionLog.ToString());
            }
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
                        SetTaskbarProgress(conversionOverallProgress.Value, TaskbarItemProgressState.Normal);
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
            SelectCombo(conversionVideoBitrateCombo, "6000k");
            SelectCombo(conversionAudioBitrateCombo, "192k");
            SetConversionStatus("Doporučené nastavení", "MP4, H.264 a CRF 23 fungují na většině zařízení.", Theme.Success);
        }

        private void UpdateRateControlVisibility()
        {
            if (conversionRateCombo == null)
                return;
            bool bitrate = ComboValue(conversionRateCombo, "crf") == "bitrate";
            conversionCrfCombo.IsEnabled = !busy && !bitrate;
            conversionVideoBitrateCombo.IsEnabled = !busy && bitrate;
            if (conversionAudioBitrateCombo != null)
                conversionAudioBitrateCombo.IsEnabled = !busy &&
                    (conversionAudioCodecCombo == null || ComboValue(conversionAudioCodecCombo, "aac") != "flac");
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
            settings.ConversionRateControl = ComboValue(conversionRateCombo, "crf");
            settings.ConversionCrf = ComboValue(conversionCrfCombo, "23");
            settings.ConversionVideoBitrate = ComboValue(conversionVideoBitrateCombo, "6000k");
            settings.ConversionAudioCodec = ComboValue(conversionAudioCodecCombo, "aac");
            settings.ConversionAudioBitrate = ComboValue(conversionAudioBitrateCombo, "192k");
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

        private void CopyConversionLog()
        {
            string text = conversionLog.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                footerStatus.Text = "Log konverze je zatím prázdný";
                return;
            }
            try
            {
                Clipboard.SetText(text);
                footerStatus.Text = "Log konverze byl zkopírován";
            }
            catch
            {
                footerStatus.Text = "Log se nepodařilo zkopírovat";
            }
        }

        private void UpdateConversionButtons()
        {
            if (conversionStartButton == null)
                return;
            conversionStartButton.IsEnabled = !busy && conversionAnalysisCount == 0 && conversionJobs.Count > 0;
            conversionCancelButton.IsEnabled = busy && activeOperation == "conversion" &&
                operationCancellation != null && !operationCancellation.IsCancellationRequested;
            conversionRemoveButton.IsEnabled = !busy && conversionGrid.SelectedItems.Count > 0;
            conversionClearButton.IsEnabled = !busy && conversionJobs.Count > 0;
            ConversionViewControl.AddConversionFilesButton.IsEnabled = !busy && conversionAnalysisCount == 0;
        }

        private void UpdateConversionControlState()
        {
            if (conversionGrid == null)
                return;

            bool editable = !busy;
            conversionGrid.IsEnabled = editable;
            ConversionViewControl.AddConversionFilesButton.IsEnabled = editable && conversionAnalysisCount == 0;
            ConversionViewControl.RecommendedSettingsButton.IsEnabled = editable;
            conversionFormatCombo.IsEnabled = editable;
            conversionCodecCombo.IsEnabled = editable;
            conversionRateCombo.IsEnabled = editable;
            conversionCrfCombo.IsEnabled = editable &&
                ComboValue(conversionRateCombo, "crf") != "bitrate";
            conversionVideoBitrateCombo.IsEnabled = editable &&
                ComboValue(conversionRateCombo, "crf") == "bitrate";
            conversionAudioCodecCombo.IsEnabled = editable;
            conversionAudioBitrateCombo.IsEnabled = editable &&
                ComboValue(conversionAudioCodecCombo, "aac") != "flac";
            conversionFolderBox.IsEnabled = editable;
            ConversionViewControl.BrowseConversionFolderButton.IsEnabled = editable;
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
