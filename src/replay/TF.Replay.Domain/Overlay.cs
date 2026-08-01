namespace TF.Replay.Domain
{
    public static class Overlay
    {
        private static Interop.IWiderSetModApi _widerSet;
        private static bool _resolved;

        public static float CenterOffset
        {
            get
            {
                if (!_resolved)
                {
                    _resolved = true;
                    _widerSet = ServiceCollections.ResolveModCollections()?.ResolveWiderSet();
                }

                return _widerSet?.UIXOffset ?? 0f;
            }
        }

        public static float RightOffset => CenterOffset * 2f;
    }
}
