using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.TowerFallExtensions.ComponentExtensions;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
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
                Id = brambles.ID,
                IsCollidable = brambles.Collidable,
                IsVisible = brambles.Visible,
                ActiveTween = brambles.GetFirst<Tween>()?.GetState(),
            };
        }

        public static void LoadState(this TowerFall.Brambles brambles, Brambles state)
        {
            var dynBrambles = DynamicData.For(brambles);

            if (brambles.Level == null)
            {
                dynBrambles.Set("Scene", TowerFall.TFGame.Instance.Scene);
                dynBrambles.Set("Level", TowerFall.TFGame.Instance.Scene as TowerFall.Level);
            }

            dynBrambles.Set("actualDepth", state.ActualDepth);
            dynBrambles.Set("soundPlayed", state.HasSoundPlayed);
            dynBrambles.Set("ID", state.Id);

            Alarm deathAlarm = dynBrambles.Get<Alarm>("deathAlarm");
            deathAlarm.LoadState(state.DeathAlarm);
            Alarm delayAlarm = dynBrambles.Get<Alarm>("delayAlarm");
            delayAlarm.LoadState(state.DelayAlarm);
            brambles.Fire.LoadState(state.Fire);

            dynBrambles.Set("OwnerIndex", state.OwnerIndex);
            dynBrambles.Set("counter", state.PositionCounter.ToTFVector());

            brambles.Position = state.Position.ToTFVector();

            LoadTween(brambles, dynBrambles, state);

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

        private static void LoadTween(TowerFall.Brambles brambles, DynamicData dynBrambles, Brambles state)
        {
            brambles.Remove<Tween>();

            if (state.ActiveTween != null)
            {
                if (state.HasTweenedOut)
                {
                    dynBrambles.Set("tweenedOut", false);
                    brambles.TweenOutNoSound();
                }
                else
                {
                    dynBrambles.Invoke("TweenIn");
                }

                brambles.GetFirst<Tween>()?.LoadState(state.ActiveTween);
            }
            else
            {
                dynBrambles.Get<Image>("image").Scale = state.IsVisible ? Vector2.One : Vector2.Zero;
            }

            dynBrambles.Set("tweenedOut", state.HasTweenedOut);
            brambles.Collidable = state.IsCollidable;
            brambles.Visible = state.IsVisible;
        }
    }
}
