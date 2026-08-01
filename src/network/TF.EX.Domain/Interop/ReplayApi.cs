using System;

namespace TF.EX.Domain.Interop
{
    public static class ReplayApi
    {
        private static Func<ITfReplayApi> _resolver;
        private static ITfReplayApi _current;

        public static void Configure(Func<ITfReplayApi> resolver)
        {
            _resolver = resolver;
            _current = null;
        }

        public static ITfReplayApi Current => _current ??= _resolver?.Invoke();
    }
}
