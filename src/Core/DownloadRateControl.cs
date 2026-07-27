using System;
using System.Globalization;
using System.Threading;

namespace MVMediaStudio.Core
{
    internal sealed class DownloadRateControl
    {
        private long bytesPerSecond;

        public DownloadRateControl()
        {
        }

        public DownloadRateControl(string rateLimit)
        {
            Set(rateLimit);
        }

        public long ReadBytesPerSecond()
        {
            return Interlocked.Read(ref bytesPerSecond);
        }

        public void Set(string rateLimit)
        {
            Interlocked.Exchange(ref bytesPerSecond, ParseBytesPerSecond(rateLimit));
        }

        public static bool CanApply(
            bool busy,
            bool downloadActive,
            bool cancellationRequested,
            bool restartRequested,
            bool valid,
            bool changed)
        {
            if (!valid)
                return false;
            if (!busy)
                return true;
            return downloadActive && !cancellationRequested && !restartRequested && changed;
        }

        internal static long ParseBytesPerSecond(string rateLimit)
        {
            string value = (rateLimit ?? "").Trim().ToUpperInvariant();
            if (value.Length < 2)
                return 0;

            char unit = value[value.Length - 1];
            decimal amount;
            if (!decimal.TryParse(
                value.Substring(0, value.Length - 1),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out amount) || amount <= 0)
                return 0;

            decimal multiplier;
            if (unit == 'K')
                multiplier = 1024m;
            else if (unit == 'M')
                multiplier = 1024m * 1024m;
            else if (unit == 'G')
                multiplier = 1024m * 1024m * 1024m;
            else
                return 0;

            decimal result = amount * multiplier;
            return result >= long.MaxValue ? long.MaxValue : (long)result;
        }
    }
}
