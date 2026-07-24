using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MVMediaStudio.Core;
using MVMediaStudio.Services;
using MVMediaStudio.UI;

namespace MVMediaStudio
{
    internal partial class MainWindow
    {
        private void ShowReportOptions(Button anchor, string area, string log)
        {
            ContextMenu menu = new ContextMenu { MinWidth = 190, PlacementTarget = anchor };
            Theme.StyleMenu(menu, this);
            MenuItem github = new MenuItem { Header = "Přes GitHub" };
            github.Click += delegate { ReportProblem(area, log, false); };
            MenuItem email = new MenuItem { Header = "E-mailem" };
            email.Click += delegate { ReportProblem(area, log, true); };
            menu.Items.Add(github);
            menu.Items.Add(email);
            menu.IsOpen = true;
        }

        private void ReportProblem(string area, string log, bool email)
        {
            MessageBoxResult result = MessageBox.Show(
                "Aplikace vytvoří očištěný diagnostický report a otevře " + (email ? "výchozí poštovní aplikaci." : "nové hlášení na veřejném GitHubu.") + "\n\n" +
                "Tokeny, klíče, osobní cesty a hodnoty parametrů URL budou odstraněny. Report přesto může obsahovat názvy souborů a adresy navštívených stránek. Pokračovat?",
                "Nahlásit chybu",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information,
                MessageBoxResult.OK);
            if (result != MessageBoxResult.OK)
                return;

            try
            {
                string path = DiagnosticReportService.Create(area, log, tools);
                string report = File.ReadAllText(path);
                Clipboard.SetText(report);
                Process.Start(new ProcessStartInfo
                {
                    FileName = email ? DiagnosticReportService.BuildEmailUrl(area, report) : DiagnosticReportService.BuildIssueUrl(area, report),
                    UseShellExecute = true
                });
                footerStatus.Text = email ?
                    "E-mail je připravený; celý očištěný report je ve schránce" :
                    "Očištěný report je ve schránce a uložený v datech aplikace";
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                MessageBox.Show("Diagnostický report se nepodařilo připravit.\n\n" + error.Message, "Nahlášení chyby", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
