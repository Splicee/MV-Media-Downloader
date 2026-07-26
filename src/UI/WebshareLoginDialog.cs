using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MVMediaStudio.UI
{
    internal sealed class WebshareLoginDialog : Window
    {
        private readonly TextBox userNameBox;
        private readonly PasswordBox passwordBox;
        private readonly CheckBox rememberCheck;

        public WebshareLoginDialog(Window owner, string userName)
        {
            Owner = owner;
            Title = "Přihlášení Webshare";
            Width = 460;
            Height = 380;
            MinWidth = 460;
            MinHeight = 380;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            FontFamily = new FontFamily("Segoe UI");
            Background = (Brush)owner.FindResource(Theme.WindowBackground);
            Foreground = (Brush)owner.FindResource(Theme.Text);
            Resources[typeof(Button)] = owner.FindResource(typeof(Button));
            Resources[typeof(TextBox)] = owner.FindResource(typeof(TextBox));
            Resources[typeof(CheckBox)] = owner.FindResource(typeof(CheckBox));
            WindowAppearance.ApplyNativeTheme(this, Theme.IsDarkTheme(owner));

            Grid root = new Grid { Margin = new Thickness(26, 22, 26, 22) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock title = new TextBlock { Text = "Webshare účet", FontSize = 21, FontWeight = FontWeights.SemiBold };
            root.Children.Add(title);

            StackPanel userPanel = Field("Uživatelské jméno nebo e-mail");
            userNameBox = new TextBox { Text = userName ?? "", MinHeight = 38 };
            userPanel.Children.Add(userNameBox);
            Grid.SetRow(userPanel, 1);
            root.Children.Add(userPanel);

            StackPanel passwordPanel = Field("Heslo");
            passwordBox = new PasswordBox
            {
                MinHeight = 38,
                Padding = new Thickness(12, 8, 12, 8),
                Background = (Brush)owner.FindResource(Theme.Input),
                Foreground = (Brush)owner.FindResource(Theme.Text),
                BorderBrush = (Brush)owner.FindResource(Theme.Border),
                BorderThickness = new Thickness(1)
            };
            passwordPanel.Children.Add(passwordBox);
            Grid.SetRow(passwordPanel, 2);
            root.Children.Add(passwordPanel);

            rememberCheck = new CheckBox { Content = "Zapamatovat relaci v tomto počítači", IsChecked = true, Margin = new Thickness(0, 13, 0, 0) };
            Grid.SetRow(rememberCheck, 3);
            root.Children.Add(rememberCheck);

            TextBlock privacy = new TextBlock
            {
                Text = "Heslo se neukládá. Přihlašovací relace je chráněná účtem Windows.",
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)owner.FindResource(Theme.Muted),
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(privacy, 4);
            root.Children.Add(privacy);

            StackPanel actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0)
            };
            Button cancel = new Button { Content = "Zrušit", MinWidth = 90, IsCancel = true };
            Button login = new Button { Content = "Přihlásit", MinWidth = 100, Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
            login.Click += delegate
            {
                if (string.IsNullOrWhiteSpace(userNameBox.Text) || string.IsNullOrEmpty(passwordBox.Password))
                    return;
                DialogResult = true;
            };
            actions.Children.Add(cancel);
            actions.Children.Add(login);
            Grid.SetRow(actions, 5);
            root.Children.Add(actions);
            Content = root;

            Loaded += delegate
            {
                if (string.IsNullOrWhiteSpace(userNameBox.Text))
                    userNameBox.Focus();
                else
                    passwordBox.Focus();
            };
        }

        public string UserName
        {
            get { return userNameBox.Text.Trim(); }
        }

        public string Password
        {
            get { return passwordBox.Password; }
        }

        public bool Remember
        {
            get { return rememberCheck.IsChecked == true; }
        }

        private static StackPanel Field(string label)
        {
            StackPanel panel = new StackPanel { Margin = new Thickness(0, 13, 0, 0) };
            panel.Children.Add(new TextBlock { Text = label, FontSize = 11.5, Margin = new Thickness(0, 0, 0, 5) });
            return panel;
        }
    }
}
