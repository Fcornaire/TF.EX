using MonoMod.Utils;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Calc;

namespace TF.EX.Patchs
{
    public class OrbLogicPatch : IHookable
    {
        private readonly IOrbService _orbService;

        public OrbLogicPatch(IOrbService orbService)
        {
            _orbService = orbService;
        }

        public void Load()
        {
            On.TowerFall.OrbLogic.Update += OrbLogic_Update;
            On.TowerFall.OrbLogic.DoLavaOrb += OrbLogic_DoLavaOrb;
            On.TowerFall.OrbLogic.DoTimeOrb += OrbLogic_DoTimeOrb;
            On.TowerFall.OrbLogic.DoDarkOrb += OrbLogic_DoDarkOrb;
        }

        public void Unload()
        {
            On.TowerFall.OrbLogic.Update -= OrbLogic_Update;
            On.TowerFall.OrbLogic.DoLavaOrb -= OrbLogic_DoLavaOrb;
            On.TowerFall.OrbLogic.DoTimeOrb -= OrbLogic_DoTimeOrb;
            On.TowerFall.OrbLogic.DoDarkOrb -= OrbLogic_DoDarkOrb;
        }

        private void OrbLogic_DoDarkOrb(On.TowerFall.OrbLogic.orig_DoDarkOrb orig, TowerFall.OrbLogic self)
        {
            CalcPatch.RegisterRng();
            orig(self);
            CalcPatch.UnregisterRng();

            //Save(self);
        }

        private void OrbLogic_DoTimeOrb(On.TowerFall.OrbLogic.orig_DoTimeOrb orig, TowerFall.OrbLogic self, bool delay)
        {
            CalcPatch.RegisterRng();
            orig(self, delay);
            CalcPatch.UnregisterRng();

            //Save(self);
        }

        private void OrbLogic_DoLavaOrb(On.TowerFall.OrbLogic.orig_DoLavaOrb orig, TowerFall.OrbLogic self, int ownerIndex)
        {
            var lava = _orbService.GetOrb().Lava;

            if (lava.IsDefault())
            {
                var dynOrb = DynamicData.For(self);
                dynOrb.Set("control", null);
            }

            orig(self, ownerIndex);
        }

        private void OrbLogic_Update(On.TowerFall.OrbLogic.orig_Update orig, TowerFall.OrbLogic self)
        {
            CalcPatch.RegisterRng();
            orig(self);
            CalcPatch.UnregisterRng();

            //Save(self);
        }

        private void Save(TowerFall.OrbLogic self)
        {
            //_orbService.Save(self.GetState());
        }
    }
}
