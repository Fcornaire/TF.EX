using TF.EX.Domain.Ports;

namespace TF.EX.Patchs.RoundLogic
{
    internal class LastManStandingRoundLogicPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;

        public LastManStandingRoundLogicPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.LastManStandingRoundLogic.OnLevelLoadFinish += LastManStandingRoundLogic_OnLevelLoadFinish; ;
        }

        public void Unload()
        {
            On.TowerFall.LastManStandingRoundLogic.OnLevelLoadFinish -= LastManStandingRoundLogic_OnLevelLoadFinish;
        }

        private void LastManStandingRoundLogic_OnLevelLoadFinish(On.TowerFall.LastManStandingRoundLogic.orig_OnLevelLoadFinish orig, TowerFall.LastManStandingRoundLogic self)
        {
            if (!_netplayManager.IsRollbackFrame()) //Prevent adding a VersusStart on a rollback frame
            {
                orig(self);
            }
        }
    }

}
