using System.Runtime.CompilerServices;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class IcicleOwnedState
    {
        public const float NotShaking = -1f;

        private static readonly ConditionalWeakTable<TowerFall.Icicle, StrongBox<float>> _fallCounters = new();

        public static float GetFallCounter(TowerFall.Icicle icicle)
        {
            return _fallCounters.TryGetValue(icicle, out var box) ? box.Value : NotShaking;
        }

        public static void SetFallCounter(TowerFall.Icicle icicle, float value)
        {
            if (_fallCounters.TryGetValue(icicle, out var box))
            {
                box.Value = value;
            }
            else
            {
                _fallCounters.Add(icicle, new StrongBox<float>(value));
            }
        }
    }

    public static class IcicleExtensions
    {
        public static Icicle GetState(this TowerFall.Icicle entity)
        {
            var dyn = DynamicData.For(entity);
            var actualDepth = dyn.Get<double>("actualDepth");
            var cannotHitCounter = dyn.Get<Counter>("cannotHitCounter");
            var cannotHit = dyn.Get<Monocle.Entity>("cannotHit");
            var cannotHitArrow = dyn.Get<TowerFall.Arrow>("cannotHitArrow");

            bool hasCannotHit = false;
            int cannotHitPlayerIndex = -1;
            if (cannotHit is TowerFall.Player player)
            {
                hasCannotHit = true;
                cannotHitPlayerIndex = player.PlayerIndex;
            }
            else if (cannotHit is TowerFall.PlayerGhost ghost)
            {
                hasCannotHit = true;
                cannotHitPlayerIndex = ghost.PlayerIndex;
            }

            bool hasCannotHitArrow = false;
            double cannotHitArrowActualDepth = 0;
            if (cannotHitArrow != null)
            {
                hasCannotHitArrow = true;
                cannotHitArrowActualDepth = DynamicData.For(cannotHitArrow).Get<double>("actualDepth");
            }

            return new Icicle
            {
                Position = entity.Position.ToModel(),
                ActualDepth = actualDepth,
                Falling = dyn.Get<bool>("falling"),
                CanFall = dyn.Get<bool>("canFall"),
                VSpeed = dyn.Get<float>("vSpeed"),
                OwnerIndex = dyn.Get<int>("ownerIndex"),
                CannotHitCounter = cannotHitCounter.Value,
                FallCounter = IcicleOwnedState.GetFallCounter(entity),
                HasCannotHit = hasCannotHit,
                CannotHitPlayerIndex = cannotHitPlayerIndex,
                HasCannotHitArrow = hasCannotHitArrow,
                CannotHitArrowActualDepth = cannotHitArrowActualDepth,
                IsActive = entity.Active,
                IsCollidable = entity.Collidable,
                IsVisible = entity.Visible,
            };
        }

        public static void LoadState(this TowerFall.Icicle entity, Icicle toLoad)
        {
            var dyn = DynamicData.For(entity);

            if (entity.Level == null)
            {
                dyn.Set("Scene", TowerFall.TFGame.Instance.Scene);
                dyn.Set("Level", TowerFall.TFGame.Instance.Scene as TowerFall.Level);
            }

            entity.Position = toLoad.Position.ToTFVector();
            dyn.Set("actualDepth", toLoad.ActualDepth);
            dyn.Set("falling", toLoad.Falling);
            dyn.Set("canFall", toLoad.CanFall);
            dyn.Set("vSpeed", toLoad.VSpeed);
            dyn.Set("ownerIndex", toLoad.OwnerIndex);

            var cannotHitCounter = dyn.Get<Counter>("cannotHitCounter");
            DynamicData.For(cannotHitCounter).Set("counter", toLoad.CannotHitCounter);

            IcicleOwnedState.SetFallCounter(entity, toLoad.FallCounter);

            if (toLoad.HasCannotHit)
            {
                dyn.Set("cannotHit", entity.Level.GetPlayerOrCorpse(toLoad.CannotHitPlayerIndex));
            }
            else
            {
                dyn.Set("cannotHit", null);
            }

            if (toLoad.HasCannotHitArrow)
            {
                dyn.Set("cannotHitArrow", entity.Level.GetEntityByDepth(toLoad.CannotHitArrowActualDepth) as TowerFall.Arrow);
            }
            else
            {
                dyn.Set("cannotHitArrow", null);
            }

            entity.Active = toLoad.IsActive;
            entity.Collidable = toLoad.IsCollidable;
            entity.Visible = toLoad.IsVisible;
        }
    }
}
