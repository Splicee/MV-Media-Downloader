using System;
using System.IO;
using System.Reflection;
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
