using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.TowerFallExtensions.ComponentExtensions;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class BramblesExtensions
    {
        public static Brambles GetState(this TowerFall.Brambles brambles)
        {
            var dynBrambles = DynamicData.For(brambles);
            double actualDepth = dynBrambles.Get<double>("actualDepth");
            bool soundPlayed = dynBrambles.Get<bool>("soundPlayed");
            Alarm deathAlarm = dynBrambles.Get<Alarm>("deathAlarm");
            Alarm delayAlarm = dynBrambles.Get<Alarm>("delayAlarm");
            bool tweenedOut = dynBrambles.Get<bool>("tweenedOut");
            Vector2 counter = dynBrambles.Get<Vector2>("counter");

            TowerFall.Solid Riding = dynBrambles.Get<TowerFall.Solid>("riding");
            double ridingActualDepth = -1;
            if (Riding != null)
            {
                var dynRiding = DynamicData.For(Riding);
                ridingActualDepth = dynRiding.Get<double>("actualDepth");
            }

            return new Brambles
            {
                ActualDepth = actualDepth,
                RidingActualDepth = ridingActualDepth,
                HasSoundPlayed = soundPlayed,
                DeathAlarm = deathAlarm.GetState(),
                DelayAlarm = delayAlarm.GetState(),
                Fire = brambles.Fire.GetState(),
                HasTweenedOut = tweenedOut,
                OwnerIndex = brambles.OwnerIndex,
                Position = brambles.Position.ToModel(),
                PositionCounter = counter.ToModel(),
            };
        }

        public static void LoadState(this TowerFall.Brambles brambles, Brambles state)
        {
            var dynBrambles = DynamicData.For(brambles);

            if (brambles.Level == null)
            {
                dynBrambles.Set("Scene", TowerFall.TFGame.Instance.Scene);
                dynBrambles.Set("Level", TowerFall.TFGame.Instance.Scene as TowerFall.Level);

                //var tags = brambles.Tags.ToList();
                //brambles.Tags.Clear();
                //foreach (var tag in tags)
                //{
                //    brambles.Tags.Add(tag);
                //}
            }

            dynBrambles.Set("actualDepth", state.ActualDepth);
            dynBrambles.Set("soundPlayed", state.HasSoundPlayed);
            dynBrambles.Set("tweenedOut", state.HasTweenedOut);
            dynBrambles.Set("counter", state.PositionCounter.ToTFVector());

            Alarm deathAlarm = dynBrambles.Get<Alarm>("deathAlarm");
            deathAlarm.LoadState(state.DeathAlarm);
            Alarm delayAlarm = dynBrambles.Get<Alarm>("delayAlarm");
            delayAlarm.LoadState(state.DelayAlarm);
            brambles.Fire.LoadState(state.Fire);

            dynBrambles.Set("OwnerIndex", state.OwnerIndex);
            dynBrambles.Set("counter", state.PositionCounter.ToTFVector());

            brambles.Position = state.Position.ToTFVector();

            if (state.RidingActualDepth == -1)
            {
                dynBrambles.Set("riding", null);
            }
            else
            {
                TowerFall.Solid Riding = dynBrambles.Get<TowerFall.Solid>("riding");
                if (Riding == null)
                {
                    Riding = brambles.Level.GetEntityByDepth(state.RidingActualDepth) as TowerFall.Solid;
                    dynBrambles.Set("riding", Riding);
                }
                else
                {
                    var dynRiding = DynamicData.For(Riding);
                    var ridingActualDepth = dynRiding.Get<double>("actualDepth");
                    if (ridingActualDepth != state.RidingActualDepth)
                    {
                        Riding = brambles.Level.GetEntityByDepth(state.RidingActualDepth) as TowerFall.Solid;
                        dynBrambles.Set("riding", Riding);
                    }
                }
            }
        }
    }
}
