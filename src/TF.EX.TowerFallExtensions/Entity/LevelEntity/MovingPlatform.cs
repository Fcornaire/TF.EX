using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;
using TF.EX.TowerFallExtensions.ComponentExtensions;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class MovingPlatformExtensions
    {
        public static MovingPlatform GetState(this TowerFall.MovingPlatform entity)
        {
            var dynMovingPlatform = DynamicData.For(entity);
            using var dynSolid = new DynData<TowerFall.Solid>(entity);

            var positionCounter = dynSolid.Get<Vector2>("counter");
            var moveTarget = dynMovingPlatform.Get<Vector2>("moveTarget");
            var start = dynMovingPlatform.Get<Vector2>("start");
            var end = dynMovingPlatform.Get<Vector2>("end");
            var counter = dynMovingPlatform.Get<Counter>("counter");
            var shakeCounter = dynMovingPlatform.Get<Counter>("shakeCounter");
            var cataclysm = dynMovingPlatform.Get<bool>("cataclysm");
            var isCollidable = entity.Collidable;
            var isActive = entity.Active;
            var actualDepth = dynMovingPlatform.Get<double>("actualDepth");

            return new MovingPlatform
            {
                Position = entity.Position.ToModel(),
                PositionCounter = positionCounter.ToModel(),
                MoveTarget = moveTarget.ToModel(),
                Start = start.ToModel(),
                End = end.ToModel(),
                Counter = counter.GetState(),
                ShakeCounter = shakeCounter.GetState(),
                Cataclysm = cataclysm,
                IsCollidable = isCollidable,
                IsActive = isActive,
                ActualDepth = actualDepth
            };
        }

        public static void LoadState(this TowerFall.MovingPlatform entity, MovingPlatform toLoad)
        {
            var dynMovingPlatform = DynamicData.For(entity);
            using var dynSolid = new DynData<TowerFall.Solid>(entity);

            entity.Collidable = toLoad.IsCollidable;
            entity.Active = toLoad.IsActive;
            entity.Position = toLoad.Position.ToTFVector();
            dynSolid.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynMovingPlatform.Set("moveTarget", toLoad.MoveTarget.ToTFVector());
            dynMovingPlatform.Set("start", toLoad.Start.ToTFVector());
            dynMovingPlatform.Set("end", toLoad.End.ToTFVector());
            dynMovingPlatform.Set("cataclysm", toLoad.Cataclysm);
            dynMovingPlatform.Set("actualDepth", toLoad.ActualDepth);

            var counter = dynMovingPlatform.Get<Counter>("counter");
            counter.LoadState(toLoad.Counter);

            var shakeCounter = dynMovingPlatform.Get<Counter>("shakeCounter");
            shakeCounter.LoadState(toLoad.ShakeCounter);
        }
    }
}
