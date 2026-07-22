using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class SensorBlockOwnedState
    {
        public const float Idle = -1f;
        public const float SlamStart = 40f;
        public const float SlamEnd = 60f;
        public const float RiseStart = 80f;
        public const float RiseEnd = 140f;
        public const float Duration = 150f;

        private static readonly ConditionalWeakTable<TowerFall.SensorBlock, StrongBox<float>> _sequenceCounters = new();

        public static float GetSequenceCounter(TowerFall.SensorBlock block)
        {
            return _sequenceCounters.TryGetValue(block, out var box) ? box.Value : Idle;
        }

        public static void SetSequenceCounter(TowerFall.SensorBlock block, float value)
        {
            if (_sequenceCounters.TryGetValue(block, out var box))
            {
                box.Value = value;
            }
            else
            {
                _sequenceCounters.Add(block, new StrongBox<float>(value));
            }
        }

        public static void Apply(TowerFall.SensorBlock block, float sequenceCounter)
        {
            var dyn = DynamicData.For(block);
            var startPos = dyn.Get<Vector2>("startPos");
            var moveTo = dyn.Get<Vector2>("moveTo");

            Vector2 target;
            if (sequenceCounter < SlamStart)
            {
                target = startPos;
            }
            else if (sequenceCounter < SlamEnd)
            {
                target = Vector2.Lerp(startPos, moveTo, Ease.CubeIn(MathHelper.Clamp((sequenceCounter - SlamStart) / 20f, 0f, 1f)));
            }
            else if (sequenceCounter < RiseStart)
            {
                target = moveTo;
            }
            else if (sequenceCounter < RiseEnd)
            {
                target = Vector2.Lerp(moveTo, startPos, Ease.CubeInOut(MathHelper.Clamp((sequenceCounter - RiseStart) / 60f, 0f, 1f)));
            }
            else
            {
                target = startPos;
            }

            block.MoveTo(target);

            ApplyCosmetics(block, sequenceCounter);
        }

        public static void ApplyCosmetics(TowerFall.SensorBlock block, float sequenceCounter)
        {
            var dyn = DynamicData.For(block);

            float t;
            if (sequenceCounter < 0f)
            {
                t = 0f;
            }
            else if (sequenceCounter < 20f)
            {
                t = sequenceCounter / 20f;
            }
            else if (sequenceCounter < 125f)
            {
                t = 1f;
            }
            else
            {
                t = 1f - MathHelper.Clamp((sequenceCounter - 125f) / 20f, 0f, 1f);
            }

            Color targetColor;
            if (block.SquisherIndex == -1)
            {
                targetColor = Color.Gray;
            }
            else if (block.Level.Session.MatchSettings.TeamMode)
            {
                targetColor = TowerFall.ArcherData.Teams[(int)block.Level.Session.MatchSettings.Teams[block.SquisherIndex]].ColorA;
            }
            else
            {
                targetColor = TowerFall.ArcherData.GetColorA(block.SquisherIndex);
            }
            targetColor = Color.Lerp(targetColor, Color.White, 0.5f);

            foreach (var gem in dyn.Get<List<Image>>("gems"))
            {
                gem.Color = Color.Lerp(Color.Gray, targetColor, t);
            }
            foreach (var light in dyn.Get<List<Image>>("lights"))
            {
                light.Color = targetColor * t * 0.7f;
            }
            block.LightColor = Color.Lerp(Color.White, targetColor, t).Invert();
            block.LightAlpha = t;
        }
    }

    public static class SensorBlockExtensions
    {
        public static SensorBlock GetState(this TowerFall.SensorBlock entity)
        {
            var dyn = DynamicData.For(entity);
            using var dynSolid = new DynData<TowerFall.Solid>(entity);

            return new SensorBlock
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                PositionCounter = dynSolid.Get<Vector2>("counter").ToModel(),
                SequenceCounter = SensorBlockOwnedState.GetSequenceCounter(entity),
                SquisherIndex = entity.SquisherIndex,
            };
        }

        public static void LoadState(this TowerFall.SensorBlock entity, SensorBlock toLoad)
        {
            using var dynSolid = new DynData<TowerFall.Solid>(entity);

            entity.Position = toLoad.Position.ToTFVector();
            dynSolid.Set("counter", toLoad.PositionCounter.ToTFVector());
            entity.SquisherIndex = toLoad.SquisherIndex;
            SensorBlockOwnedState.SetSequenceCounter(entity, toLoad.SequenceCounter);
            SensorBlockOwnedState.ApplyCosmetics(entity, toLoad.SequenceCounter);
        }
    }
}
