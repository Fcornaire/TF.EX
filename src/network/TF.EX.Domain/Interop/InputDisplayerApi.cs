using System;

namespace TF.EX.Domain.Interop
{
    public interface IInputDisplayerApi
    {
        int ApiVersion { get; }

        bool IsEnabled { get; }

        void SetEnabled(bool enabled);

        void StepOpacity(float direction);

        void BeginSession(int seatCount);

        void PushSeat(int frame, int seat, int moveX, int moveY, bool jump, bool shoot, bool altShoot, bool dodge, bool jumpPressed, bool shootPressed, bool altShootPressed, bool dodgePressed);

        void RenderAt(int frame);

        void EndSession();
    }

    public static class InputDisplayerApi
    {
        private static Func<IInputDisplayerApi> _resolver;
        private static IInputDisplayerApi _current;
        private static bool _resolved;

        public static void Configure(Func<IInputDisplayerApi> resolver)
        {
            _resolver = resolver;
            _current = null;
            _resolved = false;
        }

        public static IInputDisplayerApi Current
        {
            get
            {
                if (!_resolved)
                {
                    _resolved = true;
                    _current = _resolver?.Invoke();
                }

                return _current;
            }
        }
    }

    public static class InputDisplayerApiData
    {
        public const string Name = "TF.InputDisplayer";
    }
}
