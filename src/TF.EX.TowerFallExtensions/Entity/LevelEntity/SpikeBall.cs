using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.TowerFallExtensions.MonocleExtensions;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class SpikeBallExtensions
    {
        public static Spikeball GetState(this TowerFall.Spikeball entity)
        {
            var dynSpikeBall = DynamicData.For(entity);
            var position = dynSpikeBall.Get<Vector2>("Position");
            var rotatePercent = dynSpikeBall.Get<float>("rotatePercent");
            var isFirstHalf = dynSpikeBall.Get<bool>("firstHalf");
            var counter = dynSpikeBall.Get<Counter>("shakeCounter");

            return new Spikeball
            {
                Position = position.ToModel(),
                RotatePercent = rotatePercent,
                IsFirstHalf = isFirstHalf,
                ShakeCounter = counter.GetState()
            };
        }

        public static void LoadState(this TowerFall.Spikeball entity, Spikeball toLoad)
        {
            var dynSpikeBall = DynamicData.For(entity);

            entity.Position = toLoad.Position.ToTFVector();
            dynSpikeBall.Set("rotatePercent", toLoad.RotatePercent);
            dynSpikeBall.Set("firstHalf", toLoad.IsFirstHalf);
            var counter = dynSpikeBall.Get<Counter>("shakeCounter");
            counter.LoadState(toLoad.ShakeCounter);
        }
    }
}
