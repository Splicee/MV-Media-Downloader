using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MVMediaStudio.UI
{
    internal sealed class SourceSupportDialog : Window
    {
        public SourceSupportDialog(Window owner)
        {
            Owner = owner;
            Title = "Podporované weby";
            Width = 920;
            Height = 720;
            MinWidth = 720;
            MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            FontFamily = new FontFamily("Segoe UI");
            Background = (Brush)owner.FindResource(Theme.WindowBackground);
            Foreground = (Brush)owner.FindResource(Theme.Text);
            Resources[typeof(Button)] = owner.FindResource(typeof(Button));
            WindowAppearance.ApplyNativeTheme(this, Theme.IsDarkTheme(owner));

            Grid root = new Grid { Margin = new Thickness(28, 24, 28, 22) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            StackPanel header = new StackPanel();
            header.Children.Add(new TextBlock
            {
                Text = "Podporované zdroje",
                FontSize = 23,
                FontWeight = FontWeights.SemiBold
            });
            header.Children.Add(new TextBlock
            {
                Text = "Zelené zdroje prošly živou kontrolou metadat. Upozornění ukazují přihlášení, DRM nebo změnu přehrávače.",
                FontSize = 12.5,
                Foreground = (Brush)owner.FindResource(Theme.Muted),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
            root.Children.Add(header);

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 20, 0, 16)
            };
            StackPanel content = new StackPanel();
            AdaptiveGrid czechSources = new AdaptiveGrid
            {
                ItemMinWidth = 360,
                MaximumColumns = 2,
                ColumnSpacing = 12,
                RowSpacing = 4,
                Margin = new Thickness(0, 10, 0, 0)
            };
            czechSources.Children.Add(SourceRow(owner, "TV Nova", "Veřejná videa ověřena; Oneplay a DRM obsah ne.", Theme.Success));
            czechSources.Children.Add(SourceRow(owner, "Český rozhlas a MůjRozhlas", "Audio a pořady ověřeny.", Theme.Success));
            czechSources.Children.Add(SourceRow(owner, "Stream.cz a Televize Seznam", "Veřejná videa ověřena.", Theme.Success));
            czechSources.Children.Add(SourceRow(owner, "TV Noe", "Veřejná videa ověřena.", Theme.Success));
            czechSources.Children.Add(SourceRow(owner, "DVTV / video.aktualne.cz", "Veřejná videa ověřena.", Theme.Success));
            czechSources.Children.Add(SourceRow(owner, "JOJ / JOJ Play", "Část obsahu vyžaduje přihlášení.", Theme.Warning));
            czechSources.Children.Add(SourceRow(owner, "Česká televize", "Při HTTP 410 aktualizuj yt-dlp; část obsahu má DRM.", Theme.Warning));
            czechSources.Children.Add(SourceRow(owner, "Prima+ / CNN Prima", "Účet nebo aktualizace extraktoru mohou být nutné.", Theme.Warning));
            czechSources.Children.Add(SourceRow(owner, "Seznam Zprávy", "Souhlas webu může vyžadovat cookies z prohlížeče.", Theme.Warning));
            czechSources.Children.Add(SourceRow(owner, "iDNES / Playtvak", "Extraktor yt-dlp je nyní označený jako nefunkční.", Theme.Danger));
            czechSources.Children.Add(SourceRow(owner, "Oneplay", "Předplacený a DRM obsah aplikace nestahuje.", Theme.Danger));
            content.Children.Add(Group(owner, "České a slovenské zdroje", czechSources));

            AdaptiveGrid groups = new AdaptiveGrid
            {
                ItemMinWidth = 320,
                MaximumColumns = 3,
                ColumnSpacing = 12,
                RowSpacing = 12,
                Margin = new Thickness(0, 12, 0, 0)
            };
            groups.Children.Add(Group(owner, "Video platformy", "YouTube · Vimeo · Dailymotion\nTwitch · Kick · TikTok · Instagram\nFacebook · X · Reddit\nRumble · Streamable"));
            groups.Children.Add(Group(owner, "Hudba a podcasty", "SoundCloud · Bandcamp\nMixcloud · Apple Podcasts\nDalší zdroje podporované yt-dlp"));
            groups.Children.Add(Group(owner, "Soubory", "Webshare\nPřímé odkazy na video a audio\nPřímé odkazy na archivy a dokumenty"));
            content.Children.Add(groups);
            scroll.Content = content;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            Grid footer = new Grid();
            footer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            footer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            TextBlock loginNote = new TextBlock
            {
                Text = "Cookies z prohlížeče pomáhají se souhlasem webu a běžným přihlášením. Prima+ vyžaduje účet podporovaný přímo yt-dlp. Aplikace neobchází DRM ani placený přístup.",
                FontSize = 11.5,
                Foreground = (Brush)owner.FindResource(Theme.Muted),
                TextWrapping = TextWrapping.Wrap
            };
            footer.Children.Add(loginNote);

            Grid actions = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Button fullList = new Button { Content = "Úplný seznam yt-dlp", MinWidth = 150 };
            fullList.Click += delegate
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md",
                    UseShellExecute = true
                });
            };
            Grid.SetColumn(fullList, 1);
            actions.Children.Add(fullList);
            Button close = new Button
            {
                Content = "Zavřít",
                MinWidth = 90,
                Margin = new Thickness(8, 0, 0, 0),
                IsDefault = true,
                IsCancel = true
            };
            close.Click += delegate { Close(); };
            Grid.SetColumn(close, 2);
            actions.Children.Add(close);
            Grid.SetRow(actions, 1);
            footer.Children.Add(actions);
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;
        }

        private static Border Group(Window owner, string title, string sources)
        {
            StackPanel panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            });
            panel.Children.Add(new TextBlock
            {
                Text = sources,
                FontSize = 12,
                Foreground = (Brush)owner.FindResource(Theme.Muted),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                Margin = new Thickness(0, 8, 0, 0)
            });
            Border border = new Border
            {
                Child = panel,
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(7),
                Background = (Brush)owner.FindResource(Theme.Surface),
                BorderBrush = (Brush)owner.FindResource(Theme.Border),
                BorderThickness = new Thickness(1)
            };
            return border;
        }

        private static Border Group(Window owner, string title, UIElement content)
        {
            StackPanel panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            });
            panel.Children.Add(content);
            return new Border
            {
                Child = panel,
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(7),
                Background = (Brush)owner.FindResource(Theme.Surface),
                BorderBrush = (Brush)owner.FindResource(Theme.Border),
                BorderThickness = new Thickness(1)
            };
        }

        private static Grid SourceRow(Window owner, string name, string detail, string colorKey)
        {
            Grid row = new Grid { Margin = new Thickness(0, 4, 8, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            TextBlock icon = new TextBlock
            {
                Text = colorKey == Theme.Success ? "\uE73E" : "\uE7BA",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 11,
                Foreground = (Brush)owner.FindResource(colorKey),
                Margin = new Thickness(0, 3, 9, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            row.Children.Add(icon);
            StackPanel text = new StackPanel();
            text.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            text.Children.Add(new TextBlock
            {
                Text = detail,
                FontSize = 11,
                Foreground = (Brush)owner.FindResource(Theme.Muted),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            return row;
        }
    }
}
