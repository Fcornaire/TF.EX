using TF.InputDisplayer.Domain.Models;
using TF.InputDisplayer.Domain.Ports;

namespace TF.InputDisplayer.Domain.Api
{
    public sealed class InputDisplayerApi : IInputDisplayerApi
    {
        private readonly InputHistory _history = new InputHistory();

        private bool _active;

        public int ApiVersion => 2;

        public bool IsEnabled => DisplayOptions.Enabled;

        public void SetEnabled(bool enabled) => DisplayOptions.Enabled = enabled;

        public void StepOpacity(float direction) => DisplayOptions.StepOpacity(direction);

        public void BeginSession(int seatCount)
        {
            _history.Begin(seatCount);
            _active = seatCount > 0;
        }

        public void PushSeat(int frame, int seat, int moveX, int moveY, bool jump, bool shoot, bool altShoot, bool dodge, bool jumpPressed, bool shootPressed, bool altShootPressed, bool dodgePressed)
        {
            if (!_active)
            {
                return;
            }

            Rounds.Consume(_history, frame);
            _history.Push(frame, seat, InputPacker.Pack(moveX, moveY, jump, shoot, altShoot, dodge, jumpPressed, shootPressed, altShootPressed, dodgePressed));
        }

        public void RenderAt(int frame)
        {
            if (!_active)
            {
                return;
            }

            Rounds.Consume(_history, frame);

            InputDisplay.RenderInside(_history, frame);
            RenderQueue.Request(_history, frame);
        }

        public void EndSession()
        {
            _active = false;
            RenderQueue.Clear();
            _history.Begin(0);
        }
    }
}
