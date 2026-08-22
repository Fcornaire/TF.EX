namespace TF.InputDisplayer.Domain
{
    public static class DisplayOptions
    {
        public static bool Enabled = true;

        public static bool ShowInInstantReplay = true;

        public static int MaxRows = InputHistory.RowCapacity;

        public static float Opacity = 1f;

        private const float MinOpacity = 0.1f;

        private const float OpacityStep = 0.1f;

        public static void StepOpacity(float direction) => Opacity = Math.Clamp(MathF.Round(Opacity + direction * OpacityStep, 2), MinOpacity, 1f);
    }
}
