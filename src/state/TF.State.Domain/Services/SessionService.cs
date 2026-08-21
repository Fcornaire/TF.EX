using TF.State.Domain.Context;
using TF.State.Domain.Models;
using TF.State.Domain.Models.Entity;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.Domain.Models.Entity.LevelEntity.Chest;
using TF.State.Domain.Models.Entity.LevelEntity.Platform;
using TF.State.Domain.Ports;

namespace TF.State.Domain.Services
{
    public class SessionService : ISessionService
    {
        private readonly IStateContext _context;

        public SessionService(IStateContext context)
        {
            _context = context;
        }

        public Dictionary<int, double> GetGamePlayLayerActualDepthLookup()
        {
            return _context.GetGamePlayerLayerActualDepthLookup();
        }

        public Session GetSession()
        {
            return _context.GetSession();
        }

        public void Reset()
        {
            _context.UpdateSession(new Session
            {
                RoundEndCounter = Constants.INITIAL_END_COUNTER,
                GhostWaitCounter = Constants.INITIAL_END_COUNTER,
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

        public void UpdateRoundChests(int roundIndex, List<Chest> chests)
        {
            _context.UpdateRoundChests(roundIndex, chests);
        }

        public void UpdateRoundOrbs(int roundIndex, List<Orb> orbs)
        {
            _context.UpdateRoundOrbs(roundIndex, orbs);
        }

        public void UpdateRoundLavaControl(int roundIndex, LavaControl lavaControl)
        {
            _context.UpdateRoundLavaControl(roundIndex, lavaControl);
        }

        public Dictionary<int, RoundData> GetRoundData()
        {
            return _context.GetRoundData();
        }

        public void LoadRoundData(Dictionary<int, RoundData> roundData)
        {
            _context.LoadRoundData(roundData);
        }
    }
}
