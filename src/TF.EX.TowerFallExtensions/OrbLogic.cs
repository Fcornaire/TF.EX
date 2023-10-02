using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.TowerFallExtensions.ComponentExtensions;
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
            var spaceCounter = dynOrb.Get<Counter>("spaceCounter");
            var spaceTween = dynOrb.Get<Tween>("spaceTween");
            var targetSpaceSpeed = dynOrb.Get<Vector2>("targetSpaceSpeed");
            var spaceSpeed = dynOrb.Get<Vector2>("spaceSpeed");
            var screenOffsetStart = Vector2.Zero;
            var screenOffsetEnd = Vector2.Zero;
            if (spaceTween != null && spaceTween.FramesLeft > 0)
            {
                var dynSpaceTween = DynamicData.For(spaceTween);
                screenOffsetStart = dynSpaceTween.Get<Vector2>("ScreenOffsetStart");
                screenOffsetEnd = dynSpaceTween.Get<Vector2>("ScreenOffsetEnd");
            }

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
            orb.Space.SpaceCounter = spaceCounter.GetState();
            orb.Space.SpaceTweenTimer = spaceTween != null ? spaceTween.FramesLeft : 0;
            orb.Space.TargetSpaceSpeed = targetSpaceSpeed.ToModel();
            orb.Space.SpaceSpeed = spaceSpeed.ToModel();
            orb.Space.ScreenOffsetStart = screenOffsetStart.ToModel();
            orb.Space.ScreenOffsetEnd = screenOffsetEnd.ToModel();

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

            var spaceCounter = dynOrb.Get<Counter>("spaceCounter");
            spaceCounter.LoadState(orb.Space.SpaceCounter);

            if (orb.Space.SpaceTweenTimer > 0)
            {
                dynOrb.Set("targetSpaceSpeed", orb.Space.TargetSpaceSpeed.ToTFVector());
                dynOrb.Set("spaceSpeed", orb.Space.SpaceSpeed.ToTFVector());


                var spaceTween = dynOrb.Get<Tween>("spaceTween");
                var dynSpaceTween = DynamicData.For(spaceTween);

                dynSpaceTween.Set("FramesLeft", orb.Space.SpaceTweenTimer);

                spaceTween.OnUpdate = delegate (Tween t)
                {
                    Engine.Instance.Screen.Offset = Vector2.Lerp(orb.Space.ScreenOffsetStart.ToTFVector(), orb.Space.ScreenOffsetEnd.ToTFVector(), t.Eased);
                };

                dynSpaceTween.Set("ScreenOffsetStart", orb.Space.ScreenOffsetStart.ToTFVector());
                dynSpaceTween.Set("ScreenOffsetEnd", orb.Space.ScreenOffsetEnd.ToTFVector());
                dynOrb.Set("spaceTween", spaceTween);
            }
            else
            {
                var spaceTween = dynOrb.Get<Tween>("spaceTween");
                if (spaceTween != null)
                {
                    spaceTween.Stop();
                    dynOrb.Set("spaceTween", null);
                }
            }
        }
    }
}
