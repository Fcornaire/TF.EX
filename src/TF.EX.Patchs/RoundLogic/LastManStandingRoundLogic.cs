using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Patchs.RoundLogic
{
    internal class LastManStandingRoundLogicPatch : IHookable
    {
        private readonly ISessionService _sessionService;
        private readonly INetplayManager _netplayManager;

        public LastManStandingRoundLogicPatch(ISessionService sessionService, INetplayManager netplayManager)
        {
            _sessionService = sessionService;
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.LastManStandingRoundLogic.OnUpdate += LastManStandingRoundLogic_OnUpdate;
        }

        public void Unload()
        {
            On.TowerFall.LastManStandingRoundLogic.OnUpdate -= LastManStandingRoundLogic_OnUpdate;
        }

        private void LastManStandingRoundLogic_OnUpdate(On.TowerFall.LastManStandingRoundLogic.orig_OnUpdate orig, TowerFall.LastManStandingRoundLogic self)
        {
            if (!_netplayManager.IsReplayMode())
            {
                var session = _sessionService.GetSession();

                var dynLastManStandingRoundLogic = DynamicData.For(self);
                var endCounter = dynLastManStandingRoundLogic.Get<RoundEndCounter>("roundEndCounter");
                DynamicData.For(endCounter).Set("endCounter", session.RoundEndCounter);
            }

            orig(self);
        }
    }
}
