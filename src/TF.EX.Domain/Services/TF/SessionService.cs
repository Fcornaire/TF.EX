using TF.EX.Domain.Context;
using TF.EX.Domain.Models.State;
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
    }
}
