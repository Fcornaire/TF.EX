using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Calc;
using TF.EX.TowerFallExtensions.Entity.LevelEntity;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class LavaControlPatch : IHookable
    {
        private readonly IOrbService _orbService;

        public LavaControlPatch(IOrbService orbService)
        {
            _orbService = orbService;
        }


        public void Load()
        {
            On.TowerFall.LavaControl.ctor += LavaControl_ctor;
            On.TowerFall.LavaControl.Added += LavaControl_Added;
            On.TowerFall.LavaControl.Update += LavaControl_Update;
            On.TowerFall.LavaControl.Extend += LavaControl_Extend;
        }


        public void Unload()
        {
            On.TowerFall.LavaControl.ctor -= LavaControl_ctor;
            On.TowerFall.LavaControl.Added -= LavaControl_Added;
            On.TowerFall.LavaControl.Update -= LavaControl_Update;
            On.TowerFall.LavaControl.Extend -= LavaControl_Extend;
        }

        private void LavaControl_Extend(On.TowerFall.LavaControl.orig_Extend orig, TowerFall.LavaControl self, int ownerIndex)
        {
            CalcPatch.RegisterRng();
            orig(self, ownerIndex);
            CalcPatch.UnregisterRng();

            Save(self);
        }

        private void LavaControl_Update(On.TowerFall.LavaControl.orig_Update orig, TowerFall.LavaControl self)
        {
            var lava = _orbService.GetOrb().Lava;

            if (!lava.IsDefault())
            {
                self.LoadState(lava);
            }

            orig(self);

            if (!self.MarkedForRemoval)
            {
                Save(self);
            }
        }

        private void LavaControl_Added(On.TowerFall.LavaControl.orig_Added orig, TowerFall.LavaControl self)
        {
            CalcPatch.RegisterRng();
            orig(self);
            CalcPatch.UnregisterRng();

            Save(self);
        }

        private void LavaControl_ctor(On.TowerFall.LavaControl.orig_ctor orig, TowerFall.LavaControl self, TowerFall.LavaControl.LavaMode mode, int ownerIndex)
        {
            CalcPatch.RegisterRng();
            orig(self, mode, ownerIndex);
            CalcPatch.UnregisterRng();
        }

        private void Save(TowerFall.LavaControl self)
        {
            var orb = _orbService.GetOrb();

            orb.Lava = self.GetState();

            _orbService.Save(orb);
        }
    }
}
