using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TF.EX.TowerFallExtensions
{
    public static class OrbLogicExtenions
    {
        public static TF.EX.Domain.Models.State.OrbLogic.OrbLogic GetState(this TowerFall.OrbLogic self)
        {
            var orb = new TF.EX.Domain.Models.State.OrbLogic.OrbLogic();
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
            orb.Time.EngineTimeMult = TFGame.TimeMult;
            orb.Time.EngineTimeRate = TFGame.TimeRate;
            orb.Dark.Counter.Start = darknessStartCounter.Value;
            orb.Dark.Counter.End = darknessEndCounter.Value;
            orb.Dark.IsDarkened = darkened;
            orb.Dark.OldDarkAlpha = oldDarkAlpha;
            orb.Dark.LightingTartgetAlpha = self.Level.LightingLayer.TargetAlpha;

            return orb;
        }

        public static void LoadState(this TowerFall.OrbLogic self, TF.EX.Domain.Models.State.OrbLogic.OrbLogic orb)
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

            var dynTFGame = DynamicData.For(TFGame.Instance);
            dynTFGame.Set("TimeMult", orb.Time.EngineTimeMult);
            dynTFGame.Set("TimeRate", orb.Time.EngineTimeRate);

        }
    }
}
