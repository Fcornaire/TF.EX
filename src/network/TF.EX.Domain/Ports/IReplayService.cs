using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Ports
{
    /// <summary>
    /// EX's view of replay
    /// </summary>
    public interface IReplayService
    {
        void RegisterPlaybackCallbacks();

        void Initialize(GameData gameData = null, ICollection<Models.WebSocket.CustomMod> mods = null);

        void AddRecord(byte[] state, int frame);

        void RemovePredictedRecords(int frame);
        void Export();
        void Reset();

        Task<string> LoadAndStart(string replayFilename, string currentSong = "");
        void RunFrame();
        int GetFrame();

        int GetLoadedReplayMode();

        byte[] GetCurrentStateBytes();

        void GoTo(int numberOfFrames);

    }
}
