using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity.Platform;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class GhostPlatformExtensions
    {
        public static GhostPlatform GetState(this TowerFall.GhostPlatform entity)
        {
            var dyn = DynamicData.For(entity);
            using var dynJumpThru = new DynData<TowerFall.JumpThru>(entity);

            return new GhostPlatform
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                PositionCounter = dynJumpThru.Get<Vector2>("counter").ToModel(),
                SinkAmount = dyn.Get<float>("sinkAmount"),
            };
        }

        public static void LoadState(this TowerFall.GhostPlatform entity, GhostPlatform toLoad)
        {
            var dyn = DynamicData.For(entity);
            using var dynJumpThru = new DynData<TowerFall.JumpThru>(entity);

            entity.Position = toLoad.Position.ToTFVector();
            dynJumpThru.Set("counter", toLoad.PositionCounter.ToTFVector());
            dyn.Set("sinkAmount", toLoad.SinkAmount);
        }
    }
}
