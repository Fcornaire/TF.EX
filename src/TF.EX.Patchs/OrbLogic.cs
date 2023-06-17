using Monocle;
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
            CalcPatch.ShouldRegisterRng = true;
            orig(self);
            CalcPatch.ShouldRegisterRng = false;

            Save(self);
        }

        private void OrbLogic_DoTimeOrb(On.TowerFall.OrbLogic.orig_DoTimeOrb orig, TowerFall.OrbLogic self, bool delay)
        {
            CalcPatch.ShouldRegisterRng = true;
            orig(self, delay);
            CalcPatch.ShouldRegisterRng = false;

            Save(self);
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
            CalcPatch.ShouldRegisterRng = true;
            orig(self);
            CalcPatch.ShouldRegisterRng = false;

            Save(self);
        }

        private void Save(TowerFall.OrbLogic self)
        {
            var orb = _orbService.GetOrb();
            var dynOrb = DynamicData.For(self);
            var slowTimeEndCounter = dynOrb.Get<Counter>("slowTimeEndCounter");
            var slowTimeStartCounter = dynOrb.Get<Counter>("slowTimeStartCounter");
            var gameRateEase = dynOrb.Get<bool>("gameRateEase");
            var gameRateTarget = dynOrb.Get<float>("gameRateTarget");
            var darknessStartCounter = dynOrb.Get<Counter>("darknessStartCounter");
            var darknessEndCounter = dynOrb.Get<Counter>("darknessEndCounter");
            var darkened = dynOrb.Get<bool>("darkened");
            var oldDarkAlpha = dynOrb.Get<float>("oldDarkAlpha");

            orb.Time.Counter.End = slowTimeEndCounter.Value;
            orb.Time.Counter.Start = slowTimeStartCounter.Value;
            orb.Time.GameRateEased = gameRateEase;
            orb.Time.GameRateTarget = gameRateTarget;
            orb.Dark.Counter.Start = darknessStartCounter.Value;
            orb.Dark.Counter.End = darknessEndCounter.Value;
            orb.Dark.IsDarkened = darkened;
            orb.Dark.OldDarkAlpha = oldDarkAlpha;
            orb.Dark.LightingTartgetAlpha = self.Level.LightingLayer.TargetAlpha;

            _orbService.Save(orb);
        }

        public void LoadState(TowerFall.OrbLogic self, TF.EX.Domain.Models.State.Orb.Orb orb)
        {
            var dynOrb = DynamicData.For(self);

            var slowTimeEndCounter = DynamicData.For(dynOrb.Get<Counter>("slowTimeEndCounter"));
            slowTimeEndCounter.Set("counter", orb.Time.Counter.End);
            var slowTimeStartCounter = DynamicData.For(dynOrb.Get<Counter>("slowTimeStartCounter"));
            slowTimeStartCounter.Set("counter", orb.Time.Counter.Start);
            dynOrb.Set("gameRateEase", orb.Time.GameRateEased);
            dynOrb.Set("gameRateTarget", orb.Time.GameRateTarget);
            var darknessStartCounter = DynamicData.For(dynOrb.Get<Counter>("darknessStartCounter"));
            darknessStartCounter.Set("counter", orb.Dark.Counter.Start);
            var darknessEndCounter = DynamicData.For(dynOrb.Get<Counter>("darknessEndCounter"));
            darknessEndCounter.Set("counter", orb.Dark.Counter.End);
            dynOrb.Set("darkened", orb.Dark.IsDarkened);
            dynOrb.Set("oldDarkAlpha", orb.Dark.OldDarkAlpha);
            self.Level.LightingLayer.TargetAlpha = orb.Dark.LightingTartgetAlpha;
        }
    }
}
