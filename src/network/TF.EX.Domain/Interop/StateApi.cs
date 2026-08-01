using System;

namespace TF.EX.Domain.Interop
{
    public static class StateApi
    {
        private static Func<ITfStateApi> _resolver;
        private static ITfStateApi _current;

        public static void Configure(Func<ITfStateApi> resolver)
        {
            _resolver = resolver;
            _current = null;
        }

        public static ITfStateApi Current => _current ??= _resolver?.Invoke();
    }

    public static class TfStateApiData
    {
        public const string Name = "TF.State";
    }
}
