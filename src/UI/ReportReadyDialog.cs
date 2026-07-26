using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MVMediaStudio.UI
{
    internal enum ReportDeliveryChoice
    {
        None,
        GitHub,
        Email
    }

    internal sealed class ReportReadyDialog : Window
    {
        private readonly string reportPath;

        public ReportReadyDialog(Window owner, string path)
        {
            reportPath = path;
            Owner = owner;
            Title = "Diagnostický report";
            Width = 620;
            Height = 350;
            MinWidth = 620;
            MinHeight = 350;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            FontFamily = new FontFamily("Segoe UI");
            Background = (Brush)owner.FindResource(Theme.WindowBackground);
            Foreground = (Brush)owner.FindResource(Theme.Text);
            Resources[typeof(Button)] = owner.FindResource(typeof(Button));
            Resources[typeof(TextBox)] = owner.FindResource(typeof(TextBox));
            WindowAppearance.ApplyNativeTheme(this, Theme.IsDarkTheme(owner));

            Grid root = new Grid { Margin = new Thickness(28, 24, 28, 22) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock title = new TextBlock
            {
                Text = "Diagnostický soubor byl uložen",
                FontSize = 21,
                FontWeight = FontWeights.SemiBold
            };
            root.Children.Add(title);

            TextBlock instructions = new TextBlock
            {
                Text = "Přilož tento .txt soubor k e-mailu správci aplikace, nebo k novému hlášení na GitHubu.",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(instructions, 1);
            root.Children.Add(instructions);

            Grid pathRow = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBox pathBox = new TextBox
            {
                Text = reportPath,
                IsReadOnly = true,
                MinHeight = 38,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            pathRow.Children.Add(pathBox);
            Button folder = new Button
            {
                Content = ButtonContent("\uE838", "Otevřít složku"),
                ToolTip = "Zobrazit uložený report v Průzkumníku",
                Margin = new Thickness(8, 0, 0, 0)
            };
            folder.Click += delegate { RevealFile(); };
            Grid.SetColumn(folder, 1);
            pathRow.Children.Add(folder);
            Grid.SetRow(pathRow, 2);
            root.Children.Add(pathRow);

            TextBlock privacy = new TextBlock
            {
                Text = "Před odesláním můžeš obsah zkontrolovat. Tokeny, klíče, osobní cesty a parametry URL jsou automaticky odstraněné.",
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)owner.FindResource(Theme.Muted),
                Margin = new Thickness(0, 11, 0, 0)
            };
            Grid.SetRow(privacy, 3);
            root.Children.Add(privacy);

            Grid actions = new Grid { Margin = new Thickness(0, 22, 0, 0) };
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Button close = new Button { Content = "Zavřít", MinWidth = 86, IsCancel = true };
            close.Click += delegate { Close(); };
            Grid.SetColumn(close, 1);
            actions.Children.Add(close);

            Button email = new Button
            {
                Content = ButtonContent("\uE715", "E-mail"),
                MinWidth = 104,
                Margin = new Thickness(8, 0, 0, 0)
            };
            email.Click += delegate
            {
                Choice = ReportDeliveryChoice.Email;
                DialogResult = true;
            };
            Grid.SetColumn(email, 2);
            actions.Children.Add(email);

            Button github = new Button
            {
                Content = ButtonContent("\uE71B", "GitHub"),
                MinWidth = 112,
                Margin = new Thickness(8, 0, 0, 0),
                Background = (Brush)owner.FindResource(Theme.Primary),
                BorderBrush = (Brush)owner.FindResource(Theme.Primary),
                Foreground = Brushes.White
            };
            github.Click += delegate
            {
                Choice = ReportDeliveryChoice.GitHub;
                DialogResult = true;
            };
            Grid.SetColumn(github, 3);
            actions.Children.Add(github);
            Grid.SetRow(actions, 5);
            root.Children.Add(actions);

            Content = root;
        }

        public ReportDeliveryChoice Choice { get; private set; }

        private void RevealFile()
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

        private static StackPanel ButtonContent(string glyph, string text)
        {
            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = text,
                Margin = new Thickness(7, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            return panel;
        }
    }
}
