using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Chest;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;

namespace TF.EX.Domain.Ports.TF
{
    public interface ISessionService
    {
        Session GetSession();
        void Reset();
        void SaveSession(Session session);

        Dictionary<int, double> GetGamePlayLayerActualDepthLookup();
        void SaveGamePlayLayerActualDepthLookup(Dictionary<int, double> toSave);
        void AddBramblesState(float frameCounter, IEnumerable<MovingPlatform> movingPlatformsStates, Vector2f spreadOrigin);
        IEnumerable<BramblesStartingState> GetBramblesStartingState();
        void LoadBramblesStartingState(IEnumerable<BramblesStartingState> bramblesStartingState);
        void UpdateChestsState(int roundIndex, List<Chest> chests);
        Dictionary<int, List<Chest>> GetChestsState();
        void LoadChestsState(Dictionary<int, List<Chest>> chestsPerRound);
    }
}
