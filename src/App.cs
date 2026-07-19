using System;
using System.Net;
using System.Reflection;
using System.Windows;

[assembly: AssemblyTitle("MV Media Downloader")]
[assembly: AssemblyDescription("Stahování a konverze médií")]
[assembly: AssemblyCompany("MV")]
[assembly: AssemblyProduct("MV Media Downloader")]
[assembly: AssemblyCopyright("Copyright © MV 2026")]
[assembly: AssemblyVersion("3.0.1.0")]
[assembly: AssemblyFileVersion("3.0.1.0")]

namespace MVMediaStudio
{
    internal static class AppEntry
    {
        [STAThread]
        public static void Main()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            app.Run(new MainWindow());
        }
    }
}
