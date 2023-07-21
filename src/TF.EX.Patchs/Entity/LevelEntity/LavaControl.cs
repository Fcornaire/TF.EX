using TF.EX.Patchs.Calc;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class LavaControlPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.LavaControl.ctor += LavaControl_ctor;
            On.TowerFall.LavaControl.Added += LavaControl_Added;
            On.TowerFall.LavaControl.Extend += LavaControl_Extend;
        }

        public void Unload()
        {
            On.TowerFall.LavaControl.ctor -= LavaControl_ctor;
            On.TowerFall.LavaControl.Added -= LavaControl_Added;
            On.TowerFall.LavaControl.Extend -= LavaControl_Extend;
        }

        private void LavaControl_Extend(On.TowerFall.LavaControl.orig_Extend orig, TowerFall.LavaControl self, int ownerIndex)
        {
            CalcPatch.RegisterRng();
            orig(self, ownerIndex);
            CalcPatch.UnregisterRng();
        }

        private void LavaControl_Added(On.TowerFall.LavaControl.orig_Added orig, TowerFall.LavaControl self)
        {
            CalcPatch.RegisterRng();
            orig(self);
            CalcPatch.UnregisterRng();
        }

        private void LavaControl_ctor(On.TowerFall.LavaControl.orig_ctor orig, TowerFall.LavaControl self, TowerFall.LavaControl.LavaMode mode, int ownerIndex)
        {
            CalcPatch.RegisterRng();
            orig(self, mode, ownerIndex);
            CalcPatch.UnregisterRng();
        }
    }
}
