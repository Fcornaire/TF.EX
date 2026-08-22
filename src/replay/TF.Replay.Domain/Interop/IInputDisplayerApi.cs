namespace TF.Replay.Domain.Interop
{
    public interface IInputDisplayerApi
    {
        int ApiVersion { get; }

        bool IsEnabled { get; }

        void BeginSession(int seatCount);

        void PushSeat(int frame, int seat, int moveX, int moveY, bool jump, bool shoot, bool altShoot, bool dodge);

        void RenderAt(int frame);

        void EndSession();
    }

    public static class InputDisplayerApiData
    {
        public const string Name = "TF.InputDisplayer";
    }
}
