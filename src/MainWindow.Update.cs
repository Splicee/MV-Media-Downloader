using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using MVMediaStudio.Core;
using MVMediaStudio.Services;

namespace MVMediaStudio
{
    internal partial class MainWindow
    {
        private bool checkingUpdate;

        private async Task CheckForUpdatesAsync(bool announce)
        {
            if (checkingUpdate || busy)
            {
                if (announce)
                    MessageBox.Show(this, "Aktualizaci lze zkontrolovat po dokončení současné úlohy.", "Aktualizace", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!UpdateService.IsConfigured)
            {
                if (announce)
                    MessageBox.Show(this, "Aktualizační kanál bude dostupný po prvním vydání na GitHubu.", "Aktualizace", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            checkingUpdate = true;
            if (announce)
                footerStatus.Text = "Kontroluji aktualizace";
            try
            {
                UpdateReleaseInfo release = await UpdateService.CheckLatestAsync();
                Version current = Assembly.GetExecutingAssembly().GetName().Version;
                if (release.Version <= current)
                {
                    if (announce)
                        MessageBox.Show(this, "Používáš aktuální verzi " + current.ToString(3) + ".", "Aktualizace", MessageBoxButton.OK, MessageBoxImage.Information);
                    footerStatus.Text = "Aplikace je aktuální";
                    return;
                }

                MessageBoxResult answer = MessageBox.Show(this,
                    "Je dostupná verze " + release.Version.ToString(3) + ".\n\nChceš ji nyní stáhnout? Po ověření se aplikace restartuje. Rozpracovaná úloha nebude přerušena, protože aktualizace není dostupná během stahování ani konverze.",
                    "Nová verze", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (answer != MessageBoxResult.Yes)
                {
                    footerStatus.Text = "Aktualizace odložena";
                    return;
                }

                activeOperation = "update";
                SetBusy(true, "Stahuji aktualizaci");
                string package = await UpdateService.DownloadAsync(release, delegate(double progress, string message)
                {
                    Dispatcher.BeginInvoke(new Action(delegate { footerStatus.Text = message + "  " + progress.ToString("0") + " %"; }));
                });
                footerStatus.Text = "Aktualizace ověřena, připravuji restart";
                UpdateService.LaunchUpdater(release, package);
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                if (announce || activeOperation == "update")
                    MessageBox.Show(this, error.Message, "Aktualizaci se nepodařilo dokončit", MessageBoxButton.OK, MessageBoxImage.Error);
                footerStatus.Text = "Kontrola aktualizací se nezdařila";
            }
            finally
            {
                checkingUpdate = false;
                if (activeOperation == "update")
                {
                    activeOperation = "";
                    SetBusy(false, footerStatus.Text);
                }
            }
        }
    }
}
