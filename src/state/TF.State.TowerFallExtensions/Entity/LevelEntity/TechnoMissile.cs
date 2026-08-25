using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class TechnoMissileExtensions
    {
        public static TechnoMissile GetState(this TowerFall.TechnoMage.TechnoMissile entity)
        {
            var dynMissile = DynamicData.For(entity);
            var target = dynMissile.Get<TowerFall.Player>("target");

            return new TechnoMissile
            {
                ActualDepth = dynMissile.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                Normal = dynMissile.Get<Microsoft.Xna.Framework.Vector2>("normal").ToModel(),
                Speed = dynMissile.Get<float>("speed"),
                TargetIndex = target != null ? target.PlayerIndex : -1,
                ExplodeCounter = dynMissile.Get<Counter>("explodeCounter").Value,
                CollidableCounter = dynMissile.Get<Counter>("collidableCounter").Value,
            };
        }

        public static void LoadState(this TowerFall.TechnoMage.TechnoMissile entity, TechnoMissile toLoad, TowerFall.Level level)
        {
            var dynMissile = DynamicData.For(entity);
            dynMissile.Set("Scene", TowerFall.TFGame.Instance.Scene);

            entity.Added();

            dynMissile.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();

            var normal = toLoad.Normal.ToTFVector();
            dynMissile.Set("normal", normal);
            dynMissile.Set("speed", toLoad.Speed);

            var target = toLoad.TargetIndex >= 0 ? level.GetPlayer(toLoad.TargetIndex) : null;
            if (target != null)
            {
                dynMissile.Set("target", target);
            }

            DynamicData.For(dynMissile.Get<Counter>("explodeCounter")).Set("counter", toLoad.ExplodeCounter);
            DynamicData.For(dynMissile.Get<Counter>("collidableCounter")).Set("counter", toLoad.CollidableCounter);

            var image = dynMissile.Get<Image>("image");
            if (image != null)
            {
                image.Rotation = normal.Angle();
            }
        }
    }
}
