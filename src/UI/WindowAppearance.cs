using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MVMediaStudio.UI
{
    internal static class WindowAppearance
    {
        public static void ApplyNativeTheme(Window window, bool dark)
        {
            window.SourceInitialized += delegate
            {
                try
                {
                    IntPtr handle = new WindowInteropHelper(window).Handle;
                    int enabled = dark ? 1 : 0;
                    if (DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)) != 0)
                        DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
                }
                catch
                {
                }
            };
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    }
}
