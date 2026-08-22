namespace TF.InputDisplayer.Domain
{
    public static class ScreenBounds
    {
        public const float Width = 320f;

        public static Func<float> WideOffset;

        public static float Left => 0f;

        public static float Right => Width + (WideOffset?.Invoke() ?? 0f) * 2f;
    }
}
