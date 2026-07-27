using System.Windows;
using System.Windows.Media;

namespace MVMediaStudio.UI
{
    internal static class Theme
    {
        public const string WindowBackground = "WindowBackground";
        public const string TitleBar = "TitleBar";
        public const string TitleBarSurface = "TitleBarSurface";
        public const string TitleBarMuted = "TitleBarMuted";
        public const string Surface = "Surface";
        public const string SurfaceAlt = "SurfaceAlt";
        public const string Input = "Input";
        public const string Border = "Border";
        public const string Text = "Text";
        public const string Muted = "Muted";
        public const string Primary = "Primary";
        public const string PrimaryHover = "PrimaryHover";
        public const string Success = "Success";
        public const string Warning = "Warning";
        public const string Danger = "Danger";
        public const string Console = "Console";
        public const string ConsoleText = "ConsoleText";

        public static void Apply(Window window, bool dark)
        {
            ResourceDictionary resources = window.Resources;
            resources[WindowBackground] = Brush(dark ? "#080C10" : "#F0F3F6");
            resources[TitleBar] = Brush(dark ? "#05080B" : "#0D141A");
            resources[TitleBarSurface] = Brush(dark ? "#0E1419" : "#182128");
            resources[TitleBarMuted] = Brush(dark ? "#86949F" : "#A2ADB5");
            resources[Surface] = Brush(dark ? "#10161B" : "#FFFFFF");
            resources[SurfaceAlt] = Brush(dark ? "#171E24" : "#E9EEF2");
            resources[Input] = Brush(dark ? "#0B1014" : "#F8FAFC");
            resources[Border] = Brush(dark ? "#28323B" : "#BEC9D2");
            resources[Text] = Brush(dark ? "#F3F6F8" : "#17212B");
            resources[Muted] = Brush(dark ? "#96A4AF" : "#60707E");
            resources[Primary] = Brush(dark ? "#20A4F3" : "#087FCE");
            resources[PrimaryHover] = Brush(dark ? "#44B7F6" : "#006EB6");
            resources[Success] = Brush(dark ? "#49D49D" : "#16845D");
            resources[Warning] = Brush(dark ? "#F7C66B" : "#A96200");
            resources[Danger] = Brush(dark ? "#FF7B86" : "#C43B48");
            resources[Console] = Brush(dark ? "#030609" : "#192229");
            resources[ConsoleText] = Brush("#B8F5D1");
        }

        public static void Bind(FrameworkElement element, DependencyProperty property, string key)
        {
            element.SetResourceReference(property, key);
        }

        public static bool IsDarkTheme(Window window)
        {
            SolidColorBrush brush = window.FindResource(WindowBackground) as SolidColorBrush;
            if (brush == null)
                return true;
            Color color = brush.Color;
            return color.R + color.G + color.B < 384;
        }

        private static SolidColorBrush Brush(string value)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        }
    }
}
