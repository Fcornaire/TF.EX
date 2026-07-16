using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Runtime.CompilerServices;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public sealed class MoonGlassBlockExplodeState
    {
        public bool Exploding;
        public float WaitTicks;
        public int EmittedCells;
    }

    public static class MoonGlassBlockOwnedState
    {
        private static readonly ConditionalWeakTable<TowerFall.MoonGlassBlock, MoonGlassBlockExplodeState> _states = new();

        public static MoonGlassBlockExplodeState Get(TowerFall.MoonGlassBlock block)
        {
            return _states.GetOrCreateValue(block);
        }
    }

    public static class MoonGlassBlockExplodeController
    {
        public const float WaitTicks = 5f;
        private const int CellSize = 10;
        private const int CellsPerBatch = 10;

        public static void StartExplode(TowerFall.MoonGlassBlock block)
        {
            var state = MoonGlassBlockOwnedState.Get(block);
            if (state.Exploding)
            {
                return;
            }

            state.Exploding = true;
            state.WaitTicks = 0f;
            state.EmittedCells = 0;
        }

        public static void StepAll(TowerFall.Level level)
        {
            var moonglass = level[GameTags.Moonglass];
            if (moonglass.Count == 0)
            {
                return;
            }

            var blocks = moonglass.OfType<TowerFall.MoonGlassBlock>()
                .OrderBy(block => DynamicData.For(block).Get<double>("actualDepth"))
                .ToArray();

            foreach (var block in blocks)
            {
                Step(block);
            }
        }

        private static void Step(TowerFall.MoonGlassBlock block)
        {
            var state = MoonGlassBlockOwnedState.Get(block);
            if (!state.Exploding)
            {
                return;
            }

            if (state.WaitTicks < WaitTicks)
            {
                state.WaitTicks += TowerFall.TFGame.TimeMult;
                return;
            }

            int rows = CellCount(block.Height);
            int total = CellCount(block.Width) * rows;
            int end = Math.Min(state.EmittedCells + CellsPerBatch, total);

            for (int cell = state.EmittedCells; cell < end; cell++)
            {
                int i = cell / rows * CellSize;
                int j = cell % rows * CellSize;

                block.Level.Particles.Emit(
                    TowerFall.Particles.MoonglassShatter,
                    1,
                    TowerFall.WrapMath.ApplyWrap(block.Position + Vector2.One * 5f + new Vector2(i, j)),
                    Vector2.One * 3f,
                    Monocle.Calc.Random.NextFloat(MathF.PI * 2f));
            }

            state.EmittedCells = end;

            if (state.EmittedCells >= total)
            {
                block.RemoveSelf();
            }
        }

        private static int CellCount(float size)
        {
            return (int)Math.Ceiling(size / CellSize);
        }
    }

    public static class MoonGlassBlockExtensions
    {
        public static MoonGlassBlock GetState(this TowerFall.MoonGlassBlock entity)
        {
            var dyn = DynamicData.For(entity);
            var state = MoonGlassBlockOwnedState.Get(entity);

            return new MoonGlassBlock
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                Width = (int)entity.Width,
                Height = (int)entity.Height,
                IsCollidable = entity.Collidable,
                IsExploding = state.Exploding,
                ExplodeWaitTicks = state.WaitTicks,
                EmittedCells = state.EmittedCells,
            };
        }

        public static void LoadState(this TowerFall.MoonGlassBlock entity, MoonGlassBlock toLoad)
        {
            var dyn = DynamicData.For(entity);

            dyn.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            entity.Collidable = toLoad.IsCollidable;

            var state = MoonGlassBlockOwnedState.Get(entity);
            state.Exploding = toLoad.IsExploding;
            state.WaitTicks = toLoad.ExplodeWaitTicks;
            state.EmittedCells = toLoad.EmittedCells;
        }
    }
}
