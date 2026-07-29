using System;
using System.Runtime.InteropServices;

namespace MVMediaStudio.Services
{
    internal static class SystemPowerService
    {
        private const uint Continuous = 0x80000000;
        private const uint SystemRequired = 0x00000001;

        public static bool PreventSleep()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return false;
            try
            {
                return SetThreadExecutionState(Continuous | SystemRequired) != 0;
            }
            catch
            {
                return false;
            }
        }

        public static void AllowSleep()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;
            try
            {
                SetThreadExecutionState(Continuous);
            }
            catch
            {
            }
        }

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint executionState);
    }
}
