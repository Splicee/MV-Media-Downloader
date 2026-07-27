using System.Diagnostics;
using System.IO;
using System.Windows;

namespace MVMediaStudio.UI
{
    internal enum ReportDeliveryChoice
    {
        None,
        GitHub,
        Email
    }

    internal partial class ReportReadyDialog : Window
    {
        private readonly string reportPath;

        public ReportReadyDialog(Window owner, string path)
        {
            reportPath = path;
            Owner = owner;
            InitializeComponent();
            Theme.Apply(this, Theme.IsDarkTheme(owner));
            WindowAppearance.ApplyNativeTheme(this, Theme.IsDarkTheme(owner));
            ReportPathBox.Text = reportPath;
        }

        public ReportDeliveryChoice Choice { get; private set; }

        private void RevealFileClick(object sender, RoutedEventArgs eventArgs)
        {
            if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
                return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "/select,\"" + reportPath + "\"",
                UseShellExecute = true
            });
        }

        private void EmailClick(object sender, RoutedEventArgs eventArgs)
        {
            Choice = ReportDeliveryChoice.Email;
            DialogResult = true;
        }

        private void GitHubClick(object sender, RoutedEventArgs eventArgs)
        {
            Choice = ReportDeliveryChoice.GitHub;
            DialogResult = true;
        }
    }
}
