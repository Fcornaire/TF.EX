using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Ports
{
    public interface IReplayService
    {
        void Initialize();

        void AddRecord(GameState gameState, bool shouldSwapPlayer);
        void RemovePredictedRecords(int frame);
        void Export();
        Task LoadAndStart(string pathWithReplayToLoad);
        void RunFrame();
        Replay GetReplay();

        Record GetCurrentRecord();
        void GoTo(int numbreOfFrames);
        void Reset();
        IEnumerable<Replay> LoadAndGetReplays();
    }
}
