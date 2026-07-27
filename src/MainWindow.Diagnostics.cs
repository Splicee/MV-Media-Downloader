using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MVMediaStudio.Core;
using MVMediaStudio.Services;
using MVMediaStudio.UI;

namespace MVMediaStudio
{
    public partial class MainWindow
    {
        private void SaveProblemReport(string area, string log)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Uložit diagnostický report",
                FileName = DiagnosticReportService.SuggestedFileName(area),
                DefaultExt = ".txt",
                AddExtension = true,
                Filter = "Textový soubor (*.txt)|*.txt",
                FilterIndex = 1,
                OverwritePrompt = true,
                CheckPathExists = true
            };
            if (saveDialog.ShowDialog(this) != true)
                return;

            try
            {
                string report = DiagnosticReportService.Build(area, log, tools);
                string path = DiagnosticReportService.Save(saveDialog.FileName, report);
                footerStatus.Text = "Diagnostický report byl uložen";

                ReportReadyDialog dialog = new ReportReadyDialog(this, path);
                dialog.ShowDialog();
                if (dialog.Choice == ReportDeliveryChoice.None)
                    return;

                string target = dialog.Choice == ReportDeliveryChoice.Email ?
                    DiagnosticReportService.BuildEmailUrl(area, path) :
                    DiagnosticReportService.BuildIssueUrl(area, path);
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
                footerStatus.Text = dialog.Choice == ReportDeliveryChoice.Email ?
                    "Přilož uložený .txt soubor k e-mailu správci" :
                    "Přilož uložený .txt soubor k hlášení na GitHubu";
            }
            catch (Exception error)
            {
                AppPaths.WriteError(error);
                MessageBox.Show("Diagnostický report se nepodařilo připravit.\n\n" + error.Message, "Nahlášení chyby", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
