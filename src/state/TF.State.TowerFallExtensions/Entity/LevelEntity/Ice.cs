using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class IceExtensions
    {
        public static Ice GetState(this TowerFall.Ice entity)
        {
            var dyn = DynamicData.For(entity);

            return new Ice
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                X = entity.X,
                Width = entity.Width,
                IsCollidable = entity.Collidable,
                IsActive = entity.Active,
                HasTiles = dyn.Get<Rectangle[]>("tiles") != null,
                OldTilesAlpha = dyn.Get<float>("oldTilesAlpha"),
                OldTilesXOffset = dyn.Get<int>("oldTilesXOffset"),
            };
        }

        public static void LoadState(this TowerFall.Ice entity, Ice toLoad)
        {
            var dyn = DynamicData.For(entity);
            var hasTiles = dyn.Get<Rectangle[]>("tiles") != null;

            if (entity.X != toLoad.X || entity.Width != toLoad.Width || hasTiles != toLoad.HasTiles)
            {
                entity.X = toLoad.X;
                entity.Collider.Width = toLoad.Width;
                dyn.Set("tiles", null);

                if (toLoad.HasTiles)
                {
                    dyn.Invoke("BuildTiles");
                }
            }

            entity.Collidable = toLoad.IsCollidable;
            entity.Active = toLoad.IsActive;
            dyn.Set("oldTilesAlpha", toLoad.OldTilesAlpha);
            dyn.Set("oldTilesXOffset", toLoad.OldTilesXOffset);

            if (toLoad.OldTilesAlpha <= 0f)
            {
                dyn.Set("oldTiles", null);
            }
        }
    }
}
