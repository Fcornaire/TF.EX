using TF.State.Domain.Models;
using TF.State.Domain.Models.Entity;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.Domain.Models.Entity.LevelEntity.Chest;
using TF.State.Domain.Models.Entity.LevelEntity.Platform;

namespace TF.State.Domain.Ports
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
        void UpdateRoundChests(int roundIndex, List<Chest> chests);
        void UpdateRoundOrbs(int roundIndex, List<Orb> orbs);
        void UpdateRoundLavaControl(int roundIndex, LavaControl lavaControl);
        Dictionary<int, RoundData> GetRoundData();
        void LoadRoundData(Dictionary<int, RoundData> roundData);
    }
}
