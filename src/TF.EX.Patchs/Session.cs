using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs
{
    public class SessionPatch : IHookable
    {
        private readonly ISessionService _sessionService;
        private readonly INetplayManager _netplayManager;
        private readonly IInputService _inputService;

        public SessionPatch(ISessionService sessionService, INetplayManager netplayManager, IInputService inputService)
        {
            _sessionService = sessionService;
            _netplayManager = netplayManager;
            _inputService = inputService;
        }

        public void Load()
        {
            On.TowerFall.Session.ctor += Session_ctor;
            On.TowerFall.Session.StartRound += Session_StartRound;
            On.TowerFall.Session.GetOldScore += Session_GetOldScore;
            On.TowerFall.Session.GetWinner += Session_GetWinner;
        }

        public void Unload()
        {
            On.TowerFall.Session.ctor -= Session_ctor;
            On.TowerFall.Session.StartRound -= Session_StartRound;
            On.TowerFall.Session.GetOldScore -= Session_GetOldScore;
            On.TowerFall.Session.GetWinner -= Session_GetWinner;
        }

        private int Session_GetWinner(On.TowerFall.Session.orig_GetWinner orig, TowerFall.Session self)
        {
            var originalWinner = orig(self);

            if (_netplayManager.IsInit() && _netplayManager.ShouldSwapPlayer())
            {
                if (originalWinner == 0)
                {
                    return _inputService.GetLocalPlayerInputIndex();
                }

                if (originalWinner == 1)
                {
                    return _inputService.GetRemotePlayerInputIndex();
                }
            }

            return originalWinner;
        }

        private int Session_GetOldScore(On.TowerFall.Session.orig_GetOldScore orig, TowerFall.Session self, int scoreIndex)
        {
            if ((_netplayManager.IsInit() || _netplayManager.IsTestMode()) && _netplayManager.ShouldSwapPlayer())
            {
                if (scoreIndex == 0)
                {
                    return orig(self, _inputService.GetLocalPlayerInputIndex());
                }

                if (scoreIndex == 1)
                {
                    return orig(self, _inputService.GetRemotePlayerInputIndex());

                }
            }
            return orig(self, scoreIndex);
        }

        private void Session_StartRound(On.TowerFall.Session.orig_StartRound orig, TowerFall.Session self)
        {
            orig(self);

            var session = _sessionService.GetSession();
            session.RoundStarted = true;
            _sessionService.SaveSession(session);
        }

        private void Session_ctor(On.TowerFall.Session.orig_ctor orig, TowerFall.Session self, TowerFall.MatchSettings settings)
        {
            orig(self, settings);
            //TODO: Methodes
            settings.Variants.DisableAll();
            settings.Variants.TournamentRules();

            self.MatchSettings = settings;
        }
    }
}
