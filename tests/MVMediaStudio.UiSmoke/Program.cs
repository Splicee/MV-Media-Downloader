using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MVMediaStudio.UiSmoke
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] arguments)
        {
            string localData = Path.Combine(
                Path.GetTempPath(),
                "mv-media-ui-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(localData);
            Environment.SetEnvironmentVariable("MV_MEDIA_DOWNLOADER_DATA_DIR", localData);
            AssertIsolatedDataDirectory(localData);

            MVMediaStudio.App app = null;
            MVMediaStudio.MainWindow mainWindow = null;
            try
            {
                app = new MVMediaStudio.App();
                app.InitializeComponent();
                mainWindow = new MVMediaStudio.MainWindow();
                new WindowInteropHelper(mainWindow).EnsureHandle();
                Measure(mainWindow, 1360, 860);
                Measure(mainWindow, 960, 700);
                VerifyInteractiveStates(mainWindow);

                Assembly assembly = typeof(MVMediaStudio.MainWindow).Assembly;
                CreateDialog(assembly, "MVMediaStudio.UI.WebshareLoginDialog", mainWindow, "");
                CreateDialog(assembly, "MVMediaStudio.UI.SourceSupportDialog", mainWindow);
                CreateDialog(
                    assembly,
                    "MVMediaStudio.UI.ReportReadyDialog",
                    mainWindow,
                    Path.Combine(localData, "report.txt"));

                string snapshotDirectory = ArgumentValue(arguments, "--snapshot-dir");
                if (!string.IsNullOrWhiteSpace(snapshotDirectory))
                    RenderSnapshots(mainWindow, snapshotDirectory);

                Console.WriteLine("XAML okno i dialogy byly úspěšně vytvořeny.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                return 1;
            }
            finally
            {
                if (mainWindow != null)
                    mainWindow.Close();
                if (app != null)
                    app.Shutdown();
                try { Directory.Delete(localData, true); } catch { }
            }
        }

        private static void Measure(Window window, double width, double height)
        {
            Size size = new Size(width, height);
            FrameworkElement content = window.Content as FrameworkElement;
            if (content == null)
                throw new InvalidOperationException("Okno nemá vykreslitelný obsah.");
            content.Measure(size);
            content.Arrange(new Rect(size));
            content.UpdateLayout();
        }

        private static void AssertIsolatedDataDirectory(string expected)
        {
            Type appPaths = typeof(MVMediaStudio.MainWindow).Assembly.GetType(
                "MVMediaStudio.Core.AppPaths",
                true);
            FieldInfo dataDirectory = appPaths.GetField(
                "DataDirectory",
                BindingFlags.Public | BindingFlags.Static);
            string selected = Convert.ToString(dataDirectory.GetValue(null));
            if (!string.Equals(
                Path.GetFullPath(expected).TrimEnd('\\'),
                Path.GetFullPath(selected).TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("UI test nepoužívá izolovanou datovou složku.");
        }

        private static void RenderSnapshots(MVMediaStudio.MainWindow window, string directory)
        {
            Directory.CreateDirectory(directory);
            Invoke(window, "Navigate", "download");
            Render(window, Path.Combine(directory, "download-dark.png"));
            Render(window, Path.Combine(directory, "download-dark-compact.png"), 960, 700);
            RenderDownloadActivity(window, Path.Combine(directory, "download-dark-activity.png"));
            Invoke(window, "Navigate", "conversion");
            Render(window, Path.Combine(directory, "conversion-dark.png"));
            Invoke(window, "ToggleTheme");
            Invoke(window, "Navigate", "download");
            Render(window, Path.Combine(directory, "download-light.png"));
            Invoke(window, "Navigate", "conversion");
            Render(window, Path.Combine(directory, "conversion-light.png"));
            Invoke(window, "ToggleAdvanced");
            Render(window, Path.Combine(directory, "conversion-light-advanced.png"));
        }

        private static void VerifyInteractiveStates(MVMediaStudio.MainWindow window)
        {
            FrameworkElement downloadView = FindView(window, "DownloadViewControl");
            TextBox urlBox = Find<TextBox>(downloadView, "DownloadUrlBox");
            Button start = Find<Button>(downloadView, "DownloadStartButton");
            Button cancel = Find<Button>(downloadView, "DownloadCancelButton");
            ComboBox format = Find<ComboBox>(downloadView, "DownloadFormatCombo");
            CheckBox limitEnabled = Find<CheckBox>(downloadView, "DownloadLimitEnabledCheck");
            TextBox rateValue = Find<TextBox>(downloadView, "DownloadRateValueBox");

            Check(!start.IsEnabled && !cancel.IsEnabled, "prázdné stahování má bezpečně vypnutá tlačítka");
            urlBox.Text = "https://example.com/video";
            Check(start.IsEnabled, "platný odkaz aktivuje spuštění");
            Invoke(window, "AppendDownloadInput", "https://example.org/audio");
            TextBlock urlCount = Find<TextBlock>(downloadView, "DownloadUrlCount");
            Check(
                urlBox.Text.Contains("https://example.com/video") &&
                urlBox.Text.Contains("https://example.org/audio") &&
                urlCount.Text == "2 odkazů",
                "další vložený odkaz zachová předchozí seznam");

            Invoke(window, "Navigate", "conversion");
            Invoke(window, "SaveSettings");
            Type appPaths = typeof(MVMediaStudio.MainWindow).Assembly.GetType(
                "MVMediaStudio.Core.AppPaths",
                true);
            string settingsPath = Convert.ToString(
                appPaths.GetField("SettingsPath", BindingFlags.Public | BindingFlags.Static).GetValue(null));
            Check(
                File.ReadAllText(settingsPath).Contains("LastPage=conversion"),
                "poslední otevřená karta se uloží");
            Invoke(window, "Navigate", "download");

            string completedFile = Path.Combine(Path.GetDirectoryName(settingsPath), "hotovo.mp4");
            File.WriteAllText(completedFile, "test");
            Invoke(window, "RememberCompletedDownloadPath", completedFile);
            Button revealLast = Find<Button>(downloadView, "DownloadRevealLastButton");
            Check(revealLast.Visibility == Visibility.Visible, "dokončený soubor nabídne zobrazení ve složce");
            File.Delete(completedFile);
            revealLast.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(revealLast.Visibility == Visibility.Collapsed, "nedostupný poslední soubor se bezpečně skryje");

            Invoke(window, "BeginCancellableOperation", "download");
            SetField(window, "activeDownloadEngine", "direct");
            SetField(window, "downloadCanApplyRate", true);
            SetField(window, "appliedDownloadRateLimit", "3000K");
            Invoke(window, "SetBusy", true, "UI test stahování");
            Check(cancel.IsEnabled, "probíhající stahování aktivuje Zrušit");
            Check(!urlBox.IsEnabled && !format.IsEnabled, "během stahování se nemění zachycené volby");
            Check(limitEnabled.IsEnabled && rateValue.IsEnabled, "limit rychlosti zůstává živě dostupný");

            limitEnabled.IsChecked = true;
            rateValue.Text = "3500";
            Invoke(window, "ApplyDownloadRateNow");
            object rateControl = GetField(window, "directRateControl");
            long bytesPerSecond = Convert.ToInt64(
                rateControl.GetType().GetMethod(
                    "ReadBytesPerSecond",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(
                        rateControl,
                        null));
            Check(bytesPerSecond == 3500L * 1024, "přímý přenos přijme nový limit bez restartu");
            CancellationTokenSource active = (CancellationTokenSource)GetField(window, "activeCancellation");
            CancellationTokenSource root = (CancellationTokenSource)GetField(window, "operationCancellation");
            Check(!active.IsCancellationRequested && !root.IsCancellationRequested, "živá změna přímého přenosu není zrušení");

            SetField(window, "activeDownloadEngine", "ytdlp");
            SetField(window, "downloadCanApplyRate", true);
            SetField(window, "appliedDownloadRateLimit", "3500K");
            rateValue.Text = "4000";
            Invoke(window, "ApplyDownloadRateNow");
            active = (CancellationTokenSource)GetField(window, "activeCancellation");
            root = (CancellationTokenSource)GetField(window, "operationCancellation");
            Check(active.IsCancellationRequested && !root.IsCancellationRequested, "yt-dlp restartuje jen aktuální přenos");
            Check(Convert.ToBoolean(GetField(window, "downloadRateRestartRequested")), "změna yt-dlp je označena jako navázání");

            cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(root.IsCancellationRequested, "tlačítko Zrušit zruší celou úlohu");
            Check(!Convert.ToBoolean(GetField(window, "downloadRateRestartRequested")), "uživatelské zrušení se nezamění za změnu rychlosti");
            Invoke(window, "EndCancellableOperation");
            Invoke(window, "SetBusy", false, "UI test dokončen");
            limitEnabled.IsChecked = false;
            urlBox.Clear();

            FrameworkElement conversionView = FindView(window, "ConversionViewControl");
            IList jobs = (IList)GetField(window, "conversionJobs");
            Type jobType = typeof(MVMediaStudio.MainWindow).Assembly.GetType("MVMediaStudio.Core.ConversionJob", true);
            jobs.Add(Activator.CreateInstance(
                jobType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { Path.Combine(Path.GetTempPath(), "ui-test-video.mp4") },
                null));
            Invoke(window, "UpdateConversionQueue");
            Button conversionStart = Find<Button>(conversionView, "ConversionStartButton");
            Button conversionCancel = Find<Button>(conversionView, "ConversionCancelButton");
            DataGrid conversionGrid = Find<DataGrid>(conversionView, "ConversionGrid");
            Check(conversionStart.IsEnabled, "naplněná fronta aktivuje konverzi");

            Invoke(window, "BeginCancellableOperation", "conversion");
            Invoke(window, "SetBusy", true, "UI test konverze");
            Check(conversionCancel.IsEnabled && !conversionGrid.IsEnabled, "konverze má funkční Zrušit a zamčenou frontu");
            conversionCancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            root = (CancellationTokenSource)GetField(window, "operationCancellation");
            Check(root.IsCancellationRequested, "Zrušit v konverzi používá vlastní aktivní úlohu");
            Invoke(window, "EndCancellableOperation");
            Invoke(window, "SetBusy", false, "UI test dokončen");
            jobs.Clear();
            Invoke(window, "UpdateConversionQueue");

            Invoke(window, "ShowDownloadLog");
            Invoke(window, "ShowConversionLog");
            Border downloadLog = Find<Border>(downloadView, "DownloadLogCard");
            Border conversionLog = Find<Border>(conversionView, "ConversionLogCard");
            Invoke(window, "ToggleConversionLog");
            Check(
                downloadLog.Visibility == Visibility.Visible &&
                conversionLog.Visibility == Visibility.Collapsed,
                "logy stahování a konverze jsou nezávislé");
            Invoke(window, "ToggleDownloadLog");
        }

        private static void RenderDownloadActivity(MVMediaStudio.MainWindow window, string path)
        {
            FrameworkElement view = window.FindName("DownloadViewControl") as FrameworkElement;
            if (view == null)
                throw new InvalidOperationException("Obrazovku stahování nelze najít.");

            Invoke(window, "SetActiveDownloadItem", "Příliš žluťoučký kůň ščěř");
            Invoke(window, "SetDownloadStatus", "Stahování", "6,2 MiB/s · zbývá 1:42", "Primary");
            Invoke(window, "SetDownloadLiveLog", "[download] Příliš žluťoučký kůň ščěř · 56,4 %");
            Invoke(window, "ShowDownloadLog");

            ProgressBar progress = view.FindName("DownloadProgress") as ProgressBar;
            TextBlock percent = view.FindName("DownloadProgressPercent") as TextBlock;
            ScrollViewer scroll = view.FindName("PageScroll") as ScrollViewer;
            if (progress != null)
                progress.Value = 56.4;
            if (percent != null)
                percent.Text = "56,4 %";
            Measure(window, 1360, 860);
            if (scroll != null)
                scroll.ScrollToEnd();
            window.UpdateLayout();
            Render(window, path);

            Invoke(window, "ToggleDownloadLog");
            Invoke(window, "SetActiveDownloadItem", "");
            if (scroll != null)
                scroll.ScrollToHome();
        }

        private static void Render(Window window, string path)
        {
            Render(window, path, 1360, 860);
        }

        private static void Render(Window window, string path, int width, int height)
        {
            Invoke(window, "UpdateDownloadResponsiveLayout", (double)width, (double)height);
            Invoke(window, "UpdateConversionResponsiveLayout", (double)width, (double)height);
            Measure(window, width, height);
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Pbgra32);
            bitmap.Render((Visual)window.Content);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream output = File.Create(path))
                encoder.Save(output);
        }

        private static void Invoke(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new MissingMethodException(target.GetType().FullName, name);
            method.Invoke(target, arguments);
        }

        private static FrameworkElement FindView(Window window, string name)
        {
            FrameworkElement view = window.FindName(name) as FrameworkElement;
            if (view == null)
                throw new InvalidOperationException("Obrazovku nelze najít: " + name);
            return view;
        }

        private static T Find<T>(FrameworkElement view, string name) where T : FrameworkElement
        {
            T element = view.FindName(name) as T;
            if (element == null)
                throw new InvalidOperationException("Prvek nelze najít: " + name);
            return element;
        }

        private static object GetField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new MissingFieldException(target.GetType().FullName, name);
            return field.GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new MissingFieldException(target.GetType().FullName, name);
            field.SetValue(target, value);
        }

        private static void Check(bool value, string name)
        {
            if (!value)
                throw new InvalidOperationException("UI test selhal: " + name);
            Console.WriteLine("OK: " + name);
        }

        private static string ArgumentValue(string[] arguments, string name)
        {
            for (int index = 0; index + 1 < arguments.Length; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                    return arguments[index + 1];
            }
            return "";
        }

        private static void CreateDialog(Assembly assembly, string typeName, params object[] arguments)
        {
            Type type = assembly.GetType(typeName, true);
            Window dialog = Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                arguments,
                null) as Window;
            if (dialog == null)
                throw new InvalidOperationException("Dialog nelze vytvořit: " + typeName);
            Measure(dialog, dialog.Width, dialog.Height);
            dialog.Close();
        }
    }
}
