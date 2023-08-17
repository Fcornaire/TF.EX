using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    internal class HUDPatch : IHookable
    {
        private readonly IReplayService _replayService;
        private readonly INetplayManager _netplayManager;

        public HUDPatch(IReplayService replayService, INetplayManager netplayManager)
        {
            _replayService = replayService;
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.HUD.Update += HUD_Update;
        }

        public void Unload()
        {
            On.TowerFall.HUD.Update -= HUD_Update;
        }

        private void HUD_Update(On.TowerFall.HUD.orig_Update orig, TowerFall.HUD self)
        {
            orig(self);

            if (self is VersusMatchResults && !_netplayManager.IsReplayMode())
            {
                var dynVersusMatchResults = DynamicData.For(self);
                var finished = dynVersusMatchResults.Get<bool>("finished");
                var hasReset = dynVersusMatchResults.Get<bool>("HasReset");

                if (finished && !hasReset)
                {
                    _replayService.Export();
                    ServiceCollections.ResolveNetplayManager().Reset();
                    dynVersusMatchResults.Set("HasReset", true);
                }
            }
        }
    }
}
