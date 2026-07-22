using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class RotatePlatformExtensions
    {
        public static RotatePlatform GetState(this TowerFall.RotatePlatform entity)
        {
            var dyn = DynamicData.For(entity);
            using var dynJumpThru = new DynData<TowerFall.JumpThru>(entity);

            return new RotatePlatform
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                PositionCounter = dynJumpThru.Get<Vector2>("counter").ToModel(),
                SinkAmount = dyn.Get<float>("sinkAmount"),
                CurrentAngle = dyn.Get<float>("currentAngle"),
            };
        }

        public static void LoadState(this TowerFall.RotatePlatform entity, RotatePlatform toLoad)
        {
            var dyn = DynamicData.For(entity);
            using var dynJumpThru = new DynData<TowerFall.JumpThru>(entity);

            entity.Position = toLoad.Position.ToTFVector();
            dynJumpThru.Set("counter", toLoad.PositionCounter.ToTFVector());
            dyn.Set("sinkAmount", toLoad.SinkAmount);
            dyn.Set("currentAngle", toLoad.CurrentAngle);
        }
    }
}
