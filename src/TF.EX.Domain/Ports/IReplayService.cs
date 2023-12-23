using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Ports
{
    public interface IReplayService
    {
        void Initialize(GameData gameData = null);

        void AddRecord(GameState gameState, bool shouldSwapPlayer);
        void RemovePredictedRecords(int frame);
        void Export();
        Task LoadAndStart(string pathWithReplayToLoad);
        void RunFrame();
        Replay GetReplay();

        Record GetCurrentRecord();
        void GoTo(int numbreOfFrames);
        void Reset();
        Task<IEnumerable<Replay>> LoadAndGetReplays();

        void RecordScreen();
    }
}
