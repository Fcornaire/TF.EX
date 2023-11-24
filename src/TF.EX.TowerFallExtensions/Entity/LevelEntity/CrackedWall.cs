using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class CrackedWallExtensions
    {
        public static CrackedWall GetState(this TowerFall.CrackedWall entity)
        {
            var dynCrackedWall = DynamicData.For(entity);
            var actualDepth = dynCrackedWall.Get<double>("actualDepth");
            var explodeCounter = dynCrackedWall.Get<float>("explodeCounter");
            var explodeNormal = dynCrackedWall.Get<Vector2>("explodeNormal");
            var isActive = entity.Active;
            var positionCounter = dynCrackedWall.Get<Vector2>("counter");

            return new CrackedWall
            {
                ActualDepth = actualDepth,
                ExplodeCounter = explodeCounter,
                ExplodeNormal = explodeNormal.ToModel(),
                IsActive = isActive,
                Position = entity.Position.ToModel(),
                PositionCounter = positionCounter.ToModel(),
                IsCollidable = entity.Collidable
            };
        }

        public static void LoadState(this TowerFall.CrackedWall entity, CrackedWall toLoad)
        {
            var dynCrackedWall = DynamicData.For(entity);

            entity.Position = toLoad.Position.ToTFVector();
            dynCrackedWall.Set("counter", toLoad.PositionCounter.ToTFVector());

            dynCrackedWall.Set("actualDepth", toLoad.ActualDepth);
            dynCrackedWall.Set("explodeCounter", toLoad.ExplodeCounter);
            dynCrackedWall.Set("explodeNormal", toLoad.ExplodeNormal.ToTFVector());

            entity.Active = toLoad.IsActive;
            entity.Collidable = toLoad.IsCollidable;
        }
    }
}
