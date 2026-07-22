using TF.EX.Domain.Context;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Chest;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Domain.Services.TF
{
    public class SessionService : ISessionService
    {
        private readonly IGameContext _context;

        public SessionService(IGameContext context)
        {
            _context = context;
        }

        public void AddBramblesState(float frameCounter, IEnumerable<MovingPlatform> movingPlatformsStates, Vector2f spreadOrigin)
        {
            _context.AddBramblesState(frameCounter, movingPlatformsStates, spreadOrigin);
        }

        public IEnumerable<BramblesStartingState> GetBramblesStartingState()
        {
            return _context.GetBramblesStartingState();
        }

        public Dictionary<int, double> GetGamePlayLayerActualDepthLookup()
        {
            return _context.GetGamePlayerLayerActualDepthLookup();
        }

        public Session GetSession()
        {
            return _context.GetSession();
        }

        public void LoadBramblesStartingState(IEnumerable<BramblesStartingState> bramblesStartingState)
        {
            _context.LoadBramblesStartingState(bramblesStartingState);
        }

        public void Reset()
        {
            _context.UpdateSession(new Session
            {
                RoundEndCounter = Constants.INITIAL_END_COUNTER,
                IsEnding = false,
                Miasma = Miasma.Default(),
                RoundStarted = false
            });
        }

        public void SaveGamePlayLayerActualDepthLookup(Dictionary<int, double> toSave)
        {
            _context.SaveGamePlayerLayerActualDepthLookup(toSave);
        }

        public void SaveSession(Session session)
        {
            _context.UpdateSession(session);
        }

        public void UpdateChestsState(int roundIndex, List<Chest> chests)
        {
            _context.UpdateChestsState(roundIndex, chests);
        }

        public Dictionary<int, List<Chest>> GetChestsState()
        {
            return _context.GetChestsState();
        }

        public void LoadChestsState(Dictionary<int, List<Chest>> chestsPerRound)
        {
            _context.LoadChestsState(chestsPerRound);
        }
    }
}
