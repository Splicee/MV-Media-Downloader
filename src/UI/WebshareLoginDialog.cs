using System.Windows;

namespace MVMediaStudio.UI
{
    internal partial class WebshareLoginDialog : Window
    {
        public WebshareLoginDialog(Window owner, string userName)
        {
            Owner = owner;
            InitializeComponent();
            Theme.Apply(this, Theme.IsDarkTheme(owner));
            WindowAppearance.ApplyNativeTheme(this, Theme.IsDarkTheme(owner));
            UserNameBox.Text = userName ?? "";
            Loaded += delegate
            {
                if (string.IsNullOrWhiteSpace(UserNameBox.Text))
                    UserNameBox.Focus();
                else
                    PasswordBox.Focus();
            };
        }

        public string UserName
        {
            get { return UserNameBox.Text.Trim(); }
        }

        public string Password
        {
            get { return PasswordBox.Password; }
        }

        public bool Remember
        {
            get { return RememberCheck.IsChecked == true; }
        }

        private void LoginClick(object sender, RoutedEventArgs eventArgs)
        {
            if (string.IsNullOrWhiteSpace(UserNameBox.Text) ||
                string.IsNullOrEmpty(PasswordBox.Password))
                return;
            DialogResult = true;
        }
    }
}
