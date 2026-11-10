using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.TowerFallExtensions.ComponentExtensions;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class HeavyPhysicsObjectExtensions
    {
        public const string HaloTexture = "enemies/ghost/halo";
        private const string FadeStartKey = "ghostHaloFadeStart";

        public static bool IsHaloTexture(Subtexture subtexture)
        {
            return subtexture != null && subtexture == TowerFall.TFGame.Atlas[HaloTexture];
        }

        public static bool IsTracked(this TowerFall.HeavyPhysicsObject halo)
        {
            return DynamicData.For(halo).TryGet(FadeStartKey, out _);
        }

        public static void Track(this TowerFall.HeavyPhysicsObject halo)
        {
            var alarm = halo.GetComponent<Alarm>();
            if (alarm != null)
            {
                DynamicData.For(halo).Set(FadeStartKey, alarm.OnComplete);
            }
        }

        public static HeavyPhysicsObject GetState(this TowerFall.HeavyPhysicsObject entity)
        {
            var dyn = DynamicData.For(entity);

            return new HeavyPhysicsObject
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                PositionCounter = dyn.Get<Vector2>("counter").ToModel(),
                Speed = entity.Speed.ToModel(),
                Spin = dyn.Get<float>("spin"),
                Rotation = dyn.Get<Image>("image").Rotation,
                FadeAlarm = entity.GetComponent<Alarm>()?.GetState(),
                FadeTween = entity.GetComponent<Tween>()?.GetState(),
                IsCollidable = entity.Collidable,
            };
        }

        public static void LoadState(this TowerFall.HeavyPhysicsObject entity, HeavyPhysicsObject toLoad)
        {
            var dyn = DynamicData.For(entity);

            if (entity.Level == null)
            {
                dyn.Set("Scene", TowerFall.TFGame.Instance.Scene);
                dyn.Set("Level", TowerFall.TFGame.Instance.Scene as TowerFall.Level);
            }

            entity.Position = toLoad.Position.ToTFVector();
            dyn.Set("counter", toLoad.PositionCounter.ToTFVector());
            dyn.Set("actualDepth", toLoad.ActualDepth);
            entity.Speed = toLoad.Speed.ToTFVector();
            dyn.Set("spin", toLoad.Spin);
            entity.Collidable = toLoad.IsCollidable;

            var image = dyn.Get<Image>("image");
            image.Rotation = toLoad.Rotation;
            image.Color = Color.White;

            entity.DeleteAllComponents<Alarm>();
            entity.DeleteAllComponents<Tween>();

            var fadeStart = dyn.Get(FadeStartKey) as Action;

            if (toLoad.FadeAlarm != null)
            {
                Alarm.Set(entity, toLoad.FadeAlarm.Duration, fadeStart).LoadState(toLoad.FadeAlarm);
            }
            else if (toLoad.FadeTween != null && fadeStart != null)
            {
                fadeStart();
                var tween = entity.GetComponent<Tween>();
                tween.LoadState(toLoad.FadeTween);
                image.Color = Color.White * (Math.Max(0f, tween.FramesLeft) / tween.Duration);
            }
        }
    }
}
