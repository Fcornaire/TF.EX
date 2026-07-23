using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.TowerFallExtensions.ComponentExtensions;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class SpikeballOwnedState
    {
        public const float NotStarted = -1f;
        private const float Wait = 300f;
        private const float Ramp = 600f;

        private static readonly ConditionalWeakTable<TowerFall.Spikeball, StrongBox<float>> _spinTimers = new();

        public static float GetSpinTimer(TowerFall.Spikeball spikeball)
        {
            return _spinTimers.TryGetValue(spikeball, out var box) ? box.Value : NotStarted;
        }

        public static void SetSpinTimer(TowerFall.Spikeball spikeball, float value)
        {
            if (_spinTimers.TryGetValue(spikeball, out var box))
            {
                box.Value = value;
            }
            else
            {
                _spinTimers.Add(spikeball, new StrongBox<float>(value));
            }
        }

        public static float ComputeSpinRate(float timer)
        {
            if (timer < Wait)
            {
                return 1f;
            }

            var elapsed = timer - Wait;
            if (elapsed >= Ramp)
            {
                return 1f;
            }

            var percent = (Ramp - elapsed) / Ramp;
            return MathHelper.Lerp(1f, 2f, percent);
        }

        public static void ApplySpinRate(TowerFall.Spikeball spikeball, float timer)
        {
            DynamicData.For(spikeball).Set("spinRate", ComputeSpinRate(timer));
        }
    }

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
                ShakeCounter = counter.GetState(),
                SpinTimer = SpikeballOwnedState.GetSpinTimer(entity),
                ActualDepth = dynSpikeBall.Get<double>("actualDepth")
            };
        }

        public static void LoadState(this TowerFall.Spikeball entity, Spikeball toLoad)
        {
            var dynSpikeBall = DynamicData.For(entity);

            if (entity.Level == null)
            {
                dynSpikeBall.Set("Scene", TowerFall.TFGame.Instance.Scene);
                dynSpikeBall.Set("Level", TowerFall.TFGame.Instance.Scene as TowerFall.Level);
            }

            dynSpikeBall.Set("actualDepth", toLoad.ActualDepth);
            dynSpikeBall.Set("rotatePercent", toLoad.RotatePercent);
            dynSpikeBall.Set("firstHalf", toLoad.IsFirstHalf);
            var counter = dynSpikeBall.Get<Counter>("shakeCounter");
            counter.LoadState(toLoad.ShakeCounter);

            SpikeballOwnedState.SetSpinTimer(entity, toLoad.SpinTimer);
            SpikeballOwnedState.ApplySpinRate(entity, toLoad.SpinTimer);

            dynSpikeBall.Invoke("UpdatePosition");
        }
    }
}
