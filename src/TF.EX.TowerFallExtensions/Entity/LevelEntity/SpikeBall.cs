using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.TowerFallExtensions.ComponentExtensions;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class SpikeBallExtensions
    {
        public static Spikeball GetState(this TowerFall.Spikeball entity)
        {
            var dynSpikeBall = DynamicData.For(entity);
            var rotatePercent = dynSpikeBall.Get<float>("rotatePercent");
            var isFirstHalf = dynSpikeBall.Get<bool>("firstHalf");
            var counter = dynSpikeBall.Get<Counter>("shakeCounter");

            return new Spikeball
            {
                RotatePercent = rotatePercent,
                IsFirstHalf = isFirstHalf,
                ShakeCounter = counter.GetState()
            };
        }

        public static void LoadState(this TowerFall.Spikeball entity, Spikeball toLoad)
        {
            var dynSpikeBall = DynamicData.For(entity);

            dynSpikeBall.Set("rotatePercent", toLoad.RotatePercent);
            dynSpikeBall.Set("firstHalf", toLoad.IsFirstHalf);
            var counter = dynSpikeBall.Get<Counter>("shakeCounter");
            counter.LoadState(toLoad.ShakeCounter);

            dynSpikeBall.Invoke("UpdatePosition");
        }
    }
}
