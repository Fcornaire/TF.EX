using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class SnowClumpExtensions
    {
        public static SnowClump GetState(this TowerFall.SnowClump entity)
        {
            var dyn = DynamicData.For(entity);
            var actualDepth = dyn.Get<double>("actualDepth");
            var solid = dyn.Get<TowerFall.Solid>("solid");

            double solidActualDepth = -1;
            if (solid != null)
            {
                solidActualDepth = DynamicData.For(solid).Get<double>("actualDepth");
            }

            return new SnowClump
            {
                Position = entity.Position.ToModel(),
                ActualDepth = actualDepth,
                Melting = dyn.Get<bool>("melting"),
                Alpha = dyn.Get<float>("alpha"),
                SolidActualDepth = solidActualDepth,
                SolidOffset = dyn.Get<Vector2>("solidOffset").ToModel(),
                IsActive = entity.Active,
                IsCollidable = entity.Collidable,
                IsVisible = entity.Visible,
            };
        }

        public static void LoadState(this TowerFall.SnowClump entity, SnowClump toLoad)
        {
            var dyn = DynamicData.For(entity);

            if (entity.Level == null)
            {
                dyn.Set("Scene", TowerFall.TFGame.Instance.Scene);
                dyn.Set("Level", TowerFall.TFGame.Instance.Scene as TowerFall.Level);
            }

            entity.Position = toLoad.Position.ToTFVector();
            dyn.Set("actualDepth", toLoad.ActualDepth);
            dyn.Set("melting", toLoad.Melting);
            dyn.Set("alpha", toLoad.Alpha);
            dyn.Set("solidOffset", toLoad.SolidOffset.ToTFVector());

            if (toLoad.SolidActualDepth == -1)
            {
                dyn.Set("solid", null);
            }
            else
            {
                dyn.Set("solid", entity.Level.GetEntityByDepth(toLoad.SolidActualDepth) as TowerFall.Solid);
            }

            entity.Active = toLoad.IsActive;
            entity.Collidable = toLoad.IsCollidable;
            entity.Visible = toLoad.IsVisible;
        }
    }
}
