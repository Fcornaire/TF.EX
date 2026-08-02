using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity.Platform;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class LoopPlatformExtensions
    {
        public static LoopPlatform GetState(this TowerFall.LoopPlatform entity)
        {
            var dyn = DynamicData.For(entity);
            using var dynJumpThru = new DynData<TowerFall.JumpThru>(entity);

            return new LoopPlatform
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                PositionCounter = dynJumpThru.Get<Vector2>("counter").ToModel(),
                SinkAmount = dyn.Get<float>("sinkAmount"),
                MoveAdd = dyn.Get<Vector2>("moveAdd").ToModel(),
                IsWaiting = dyn.Get<bool>("waiting"),
            };
        }

        public static void LoadState(this TowerFall.LoopPlatform entity, LoopPlatform toLoad)
        {
            var dyn = DynamicData.For(entity);
            using var dynJumpThru = new DynData<TowerFall.JumpThru>(entity);

            entity.Position = toLoad.Position.ToTFVector();
            dynJumpThru.Set("counter", toLoad.PositionCounter.ToTFVector());
            dyn.Set("sinkAmount", toLoad.SinkAmount);
            dyn.Set("moveAdd", toLoad.MoveAdd.ToTFVector());

            dyn.Set("waiting", toLoad.IsWaiting);
        }
    }
}
