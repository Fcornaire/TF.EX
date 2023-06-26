using TF.EX.Domain.Ports;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class ArrowPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;

        public ArrowPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.Arrow.EnterFallMode += Arrow_EnterFallMode;
            On.TowerFall.Arrow.DoWrapRender += Arrow_DoWrapRender;
            On.TowerFall.Arrow.Removed += Arrow_Removed;
            On.TowerFall.Arrow.EnforceLimit += Arrow_EnforceLimit;
        }

        public void Unload()
        {
            On.TowerFall.Arrow.EnterFallMode -= Arrow_EnterFallMode;
            On.TowerFall.Arrow.DoWrapRender -= Arrow_DoWrapRender;
            On.TowerFall.Arrow.Removed -= Arrow_Removed;
            On.TowerFall.Arrow.EnforceLimit -= Arrow_EnforceLimit;
        }

        //TODO: Properly track arrow decay to remove this
        private void Arrow_EnforceLimit(On.TowerFall.Arrow.orig_EnforceLimit orig, TowerFall.Level level)
        {
            //Console.WriteLine("Arrow EnforceLimit Ignore for now");
        }

        //TODO: remove this when a test without this is done
        private void Arrow_Removed(On.TowerFall.Arrow.orig_Removed orig, TowerFall.Arrow self)
        {
            orig(self);
            TowerFall.Arrow.FlushCache();
        }

        private void Arrow_DoWrapRender(On.TowerFall.Arrow.orig_DoWrapRender orig, TowerFall.Arrow self)
        {
            if (_netplayManager.IsTestMode() || _netplayManager.IsReplayMode())
            {
                self.DebugRender();
            }

            orig(self);
        }

        private void Arrow_EnterFallMode(On.TowerFall.Arrow.orig_EnterFallMode orig, TowerFall.Arrow self, bool bounce, bool zeroX, bool sound)
        {
            Calc.CalcPatch.RegisterRng();
            orig(self, bounce, zeroX, sound);
            Calc.CalcPatch.UnregisterRng();
        }
    }
}
