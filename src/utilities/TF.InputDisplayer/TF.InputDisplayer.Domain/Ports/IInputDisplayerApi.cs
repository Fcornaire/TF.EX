namespace TF.InputDisplayer.Domain.Ports
{
    public interface IInputDisplayerApi
    {
        int ApiVersion { get; }

        bool IsEnabled { get; }

        void SetEnabled(bool enabled);

        void StepOpacity(float direction);

        void BeginSession(int seatCount);

        void PushSeat(int frame, int seat, int moveX, int moveY, bool jump, bool shoot, bool altShoot, bool dodge);

        void RenderAt(int frame);

        void EndSession();
    }
}
