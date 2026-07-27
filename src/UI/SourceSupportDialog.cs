using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace MVMediaStudio.UI
{
    internal partial class SourceSupportDialog : Window
    {
        public SourceSupportDialog(Window owner)
        {
            Owner = owner;
            InitializeComponent();
            Theme.Apply(this, Theme.IsDarkTheme(owner));
            WindowAppearance.ApplyNativeTheme(this, Theme.IsDarkTheme(owner));
            CzechSources.ItemsSource = new[]
            {
                Entry("TV Nova", "Veřejná videa ověřena; Oneplay a DRM obsah ne.", Theme.Success),
                Entry("Český rozhlas a MůjRozhlas", "Audio a pořady ověřeny.", Theme.Success),
                Entry("Stream.cz a Televize Seznam", "Veřejná videa ověřena.", Theme.Success),
                Entry("TV Noe", "Veřejná videa ověřena.", Theme.Success),
                Entry("DVTV / video.aktualne.cz", "Veřejná videa ověřena.", Theme.Success),
                Entry("JOJ / JOJ Play", "Část obsahu vyžaduje přihlášení.", Theme.Warning),
                Entry("Česká televize", "Při HTTP 410 aktualizuj yt-dlp; část obsahu má DRM.", Theme.Warning),
                Entry("Prima+ / CNN Prima", "Účet nebo aktualizace extraktoru mohou být nutné.", Theme.Warning),
                Entry("Seznam Zprávy", "Souhlas webu může vyžadovat cookies z prohlížeče.", Theme.Warning),
                Entry("iDNES / Playtvak", "Extraktor yt-dlp je nyní označený jako nefunkční.", Theme.Danger),
                Entry("Oneplay", "Předplacený a DRM obsah aplikace nestahuje.", Theme.Danger)
            };
        }

        private SourceSupportEntry Entry(string name, string detail, string colorKey)
        {
            return new SourceSupportEntry
            {
                Name = name,
                Detail = detail,
                Glyph = colorKey == Theme.Success ? "\uE73E" : "\uE7BA",
                StatusBrush = (Brush)FindResource(colorKey)
            };
        }

        private void OpenFullListClick(object sender, RoutedEventArgs eventArgs)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md",
                UseShellExecute = true
            });
        }

        private sealed class SourceSupportEntry
        {
            public string Name { get; set; }
            public string Detail { get; set; }
            public string Glyph { get; set; }
            public Brush StatusBrush { get; set; }
        }
    }
}
