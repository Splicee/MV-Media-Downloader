using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MVMediaStudio.Core;

namespace MVMediaStudio
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += HandleDispatcherException;
            AppDomain.CurrentDomain.UnhandledException += HandleDomainException;
            TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
        }

        private static void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs eventArgs)
        {
            AppPaths.WriteError(eventArgs.Exception);
        }

        private static void HandleDomainException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            AppPaths.WriteError(eventArgs.ExceptionObject as Exception);
        }

        private static void HandleUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs eventArgs)
        {
            AppPaths.WriteError(eventArgs.Exception);
            eventArgs.SetObserved();
        }
    }
}
