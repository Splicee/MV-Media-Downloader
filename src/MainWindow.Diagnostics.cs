using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using MVMediaStudio.Core;
using MVMediaStudio.Services;

namespace MVMediaStudio
{
    internal partial class MainWindow
    {
        private void ReportProblem(string area, string log)
        {
            MessageBoxResult result = MessageBox.Show(
                "Aplikace vytvoří očištěný diagnostický report a otevře nové hlášení na veřejném GitHubu.\n\n" +
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
                    FileName = DiagnosticReportService.BuildIssueUrl(area, report),
                    UseShellExecute = true
                });
                footerStatus.Text = "Očištěný report je ve schránce a uložený v datech aplikace";
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                MessageBox.Show("Diagnostický report se nepodařilo připravit.\n\n" + error.Message, "Nahlášení chyby", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
