namespace TF.Replay.Domain.Interop
{
    public interface ITfExArcherSkinApi
    {
        void SetReplaySkinSeats(int[] seats, string[] skinArcherIds);
        void ClearReplaySkinSeats();
    }

    public static class TfExApiData
    {
        public const string Name = "TF.EX";
    }
}
