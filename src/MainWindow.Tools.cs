using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using MVMediaStudio.Core;
using MVMediaStudio.UI;

namespace MVMediaStudio
{
    internal partial class MainWindow
    {
        private ContextMenu BuildRepairMenu()
        {
            ContextMenu menu = new ContextMenu { MinWidth = 245 };
            MenuItem check = new MenuItem { Header = "Zkontrolovat nástroje" };
            check.Click += async delegate { await RefreshToolsAsync(true); };
            menu.Items.Add(check);

            MenuItem ytDlp = new MenuItem { Header = "Stáhnout / aktualizovat yt-dlp" };
            ytDlp.Click += async delegate { await InstallToolAsync("yt-dlp", toolService.InstallYtDlpAsync); };
            menu.Items.Add(ytDlp);

            MenuItem ffmpeg = new MenuItem { Header = "Stáhnout / opravit FFmpeg" };
            ffmpeg.Click += async delegate { await InstallToolAsync("FFmpeg", toolService.InstallFfmpegAsync); };
            menu.Items.Add(ffmpeg);

            MenuItem deno = new MenuItem { Header = "Stáhnout / opravit JS runtime (Deno)" };
            deno.Click += async delegate { await InstallToolAsync("Deno", toolService.InstallDenoAsync); };
            menu.Items.Add(deno);

            MenuItem selectYtDlp = new MenuItem { Header = "Vybrat existující yt-dlp.exe" };
            selectYtDlp.Click += async delegate { await ImportYtDlpAsync(); };
            menu.Items.Add(selectYtDlp);
            menu.Items.Add(new Separator());

            MenuItem checkUpdate = new MenuItem { Header = "Zkontrolovat aktualizace aplikace" };
            checkUpdate.Click += async delegate { await CheckForUpdatesAsync(true); };
            menu.Items.Add(checkUpdate);

            autoUpdateMenuItem = new MenuItem { Header = "Automaticky kontrolovat aktualizace", IsCheckable = true, IsChecked = settings.AutoUpdate };
            autoUpdateMenuItem.Click += delegate
            {
                settings.AutoUpdate = autoUpdateMenuItem.IsChecked;
                settings.Save();
            };
            menu.Items.Add(autoUpdateMenuItem);
            menu.Items.Add(new Separator());

            advancedMenuItem = new MenuItem { Header = "Pokročilé zobrazení", IsCheckable = true, IsChecked = settings.AdvancedMode };
            advancedMenuItem.Click += delegate { ToggleAdvanced(); };
            menu.Items.Add(advancedMenuItem);

            themeMenuItem = new MenuItem { Header = IsDark ? "Světlý režim" : "Tmavý režim" };
            themeMenuItem.Click += delegate { ToggleTheme(); };
            menu.Items.Add(themeMenuItem);
            menu.Items.Add(new Separator());

            MenuItem openData = new MenuItem { Header = "Otevřít data a logy" };
            openData.Click += delegate { OpenDirectory(AppPaths.DataDirectory); };
            menu.Items.Add(openData);
            return menu;
        }

        private async Task RefreshToolsAsync(bool announce)
        {
            if (announce)
                footerStatus.Text = "Kontroluji nástroje…";
            try
            {
                ToolState checkedTools = await Task.Run(delegate { return toolService.Check(); });
                tools = checkedTools;
                RenderToolStatus();
                if (announce)
                {
                    int count = (tools.HasYtDlp ? 1 : 0) + (tools.HasFfmpeg ? 1 : 0) + (tools.HasJsRuntime ? 1 : 0);
                    footerStatus.Text = count == 3 ? "Všechny nástroje jsou připravené" : "Některé volitelné nástroje chybí";
                }
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                footerStatus.Text = "Kontrola nástrojů se nepovedla";
            }
        }

        private void RenderToolStatus()
        {
            if (toolStatusPanel == null)
                return;
            toolStatusPanel.Children.Clear();
            toolStatusPanel.Children.Add(ToolIndicator("yt-dlp", tools.HasYtDlp, tools.YtDlpVersion));
            toolStatusPanel.Children.Add(ToolIndicator("FFmpeg", tools.HasFfmpeg, tools.FfmpegVersion));
            toolStatusPanel.Children.Add(ToolIndicator("JS", tools.HasJsRuntime, tools.JsRuntimeVersion));
        }

        private Border ToolIndicator(string name, bool ready, string detail)
        {
            StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new TextBlock
            {
                Text = ready ? "\uE73E" : "\uE7BA",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 10,
                Foreground = ready ? Brush("#49D49D") : Brush("#F7C66B"),
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(new TextBlock { Text = name, Foreground = Brush("#B8C2CA"), FontSize = 10.5, Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            Border pill = new Border { Background = Brush("#171E24"), CornerRadius = new CornerRadius(5), Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 6, 0), Child = row };
            pill.ToolTip = ready ? (string.IsNullOrWhiteSpace(detail) ? name + " je připravený" : detail) : name + " nebyl nalezen";
            return pill;
        }

        private async Task<bool> EnsureYtDlpAsync()
        {
            if (!tools.HasYtDlp)
                await RefreshToolsAsync(false);
            if (tools.HasYtDlp)
                return true;

            MessageBoxResult answer = MessageBox.Show(this, "Pro stahování je potřeba yt-dlp. Chceš ho teď stáhnout z oficiálního vydání?", "Chybí yt-dlp", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer != MessageBoxResult.Yes)
                return false;
            await InstallToolAsync("yt-dlp", toolService.InstallYtDlpAsync);
            return tools.HasYtDlp;
        }

        private async Task<bool> EnsureFfmpegAsync()
        {
            if (!tools.HasFfmpeg)
                await RefreshToolsAsync(false);
            if (tools.HasFfmpeg)
                return true;

            MessageBoxResult answer = MessageBox.Show(this, "Pro konverzi je potřeba FFmpeg. Chceš ho teď stáhnout a ověřit?", "Chybí FFmpeg", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer != MessageBoxResult.Yes)
                return false;
            await InstallToolAsync("FFmpeg", toolService.InstallFfmpegAsync);
            return tools.HasFfmpeg;
        }

        private async Task InstallToolAsync(string label, Func<Action<double, string>, Task> installer)
        {
            if (busy)
                return;
            activeOperation = "tools";
            SetBusy(true, "Instaluji " + label);
            try
            {
                await installer(delegate(double progress, string message)
                {
                    Dispatcher.BeginInvoke(new Action(delegate { footerStatus.Text = message + "  " + progress.ToString("0") + " %"; }));
                });
                await RefreshToolsAsync(false);
                footerStatus.Text = label + " je připravený";
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                footerStatus.Text = "Instalace " + label + " se nepovedla";
                MessageBox.Show(this, "Nástroj se nepodařilo připravit.\n\n" + error.Message, "Instalace se nepovedla", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                activeOperation = "";
                SetBusy(false, footerStatus.Text);
            }
        }

        private async Task ImportYtDlpAsync()
        {
            OpenFileDialog dialog = new OpenFileDialog { Title = "Vyber yt-dlp.exe", Filter = "yt-dlp|yt-dlp.exe|Spustitelné soubory|*.exe" };
            if (dialog.ShowDialog(this) != true)
                return;
            try
            {
                AppPaths.EnsureDirectories();
                File.Copy(dialog.FileName, Path.Combine(AppPaths.BinDirectory, "yt-dlp.exe"), true);
                await RefreshToolsAsync(true);
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                MessageBox.Show(this, error.Message, "Soubor se nepodařilo použít", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
