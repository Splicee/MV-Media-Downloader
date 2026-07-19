namespace MVMediaStudio.Core
{
    internal static class ScrollWheelTuning
    {
        public const double PixelStep = 32d;
        private const int DeltaPerStep = 120;

        public static int ConsumeSteps(ref int remainder, int delta)
        {
            remainder += delta;
            int steps = remainder / DeltaPerStep;
            remainder -= steps * DeltaPerStep;
            return steps;
        }
    }
}
