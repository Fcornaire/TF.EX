using MonoMod.Utils;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    internal class HUDPatch : IHookable
    {
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

            if (self is VersusMatchResults)
            {
                var dynVersusMatchResults = DynamicData.For(self);
                var finished = (bool)dynVersusMatchResults.Get("finished");

                if (finished)
                {
                    ServiceCollections.ResetState();
                    ServiceCollections.ResolveNetplayManager().Reset();
                    ServiceCollections.ResolveReplayService().Reset();
                    (var stateMachine, _) = ServiceCollections.ResolveStateMachineService();
                    stateMachine.Reset();
                }
            }
        }
    }
}
