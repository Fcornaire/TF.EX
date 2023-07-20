using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class ArrowExtensions
    {
        public static Arrow GetState(this TowerFall.Arrow entity)
        {
            var dynArrow = DynamicData.For(entity);
            var actualDepth = dynArrow.Get<double>("actualDepth");
            var shootingCounter = dynArrow.Get<Counter>("shootingCounter");
            var cannotPickupCounter = dynArrow.Get<Counter>("cannotPickupCounter");
            var cannotCatchCounter = dynArrow.Get<Counter>("cannotCatchCounter");
            var stuckDir = dynArrow.Get<Vector2>("stuckDir");
            var counter = dynArrow.Get<Vector2>("counter");
            var flashCounter = dynArrow.Get<float>("flashCounter");
            var flashInterval = dynArrow.Get<float>("flashInterval");

            return new Arrow
            {
                ActualDepth = actualDepth,
                ArrowType = entity.ArrowType.ToModel(),
                Direction = entity.Direction,
                PlayerIndex = entity.PlayerIndex,
                Position = entity.Position.ToModel(),
                ShootingCounter = shootingCounter.Value,
                CannotPickupCounter = cannotPickupCounter.Value,
                CannotCatchCounter = cannotCatchCounter.Value,
                Speed = entity.Speed.ToModel(),
                State = entity.State.ToModel(),
                StuckDirection = stuckDir.ToModel(),
                PositionCounter = counter.ToModel(),
                IsActive = entity.Active,
                IsCollidable = entity.Collidable,
                IsFrozen = entity.Frozen,
                IsVisible = entity.Visible,
                MarkedForRemoval = entity.MarkedForRemoval,
                Flash = new Domain.Models.State.Entity.LevelEntity.Flash(entity.Flashing, flashCounter, flashInterval),
                HasUnhittableEntity = entity.CannotHit != null
            };
        }

        public static void LoadState(this TowerFall.Arrow entity, Arrow toLoad)
        {
            var dynArrow = DynamicData.For(entity);
            dynArrow.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynArrow.Set("State", toLoad.State.ToTFModel());
            entity.Position = toLoad.Position.ToTFVector();
            entity.Direction = toLoad.Direction;
            dynArrow.Set("actualDepth", toLoad.ActualDepth);
            dynArrow.Set("PlayerIndex", toLoad.PlayerIndex);
            var shootingCounter = DynamicData.For(dynArrow.Get<Counter>("shootingCounter"));
            shootingCounter.Set("counter", toLoad.ShootingCounter);

            var cannotPickupCounter = DynamicData.For(dynArrow.Get<Counter>("cannotPickupCounter"));
            cannotPickupCounter.Set("counter", toLoad.CannotPickupCounter);

            var cannotCatchCounter = DynamicData.For(dynArrow.Get<Counter>("cannotCatchCounter"));
            cannotCatchCounter.Set("counter", toLoad.CannotCatchCounter);

            entity.Speed = toLoad.Speed.ToTFVector();
            dynArrow.Set("direction", toLoad.Direction);
            dynArrow.Set("stuckDir", toLoad.StuckDirection.ToTFVector());
            entity.Collidable = toLoad.IsCollidable;
            entity.Active = toLoad.IsActive;

            dynArrow.Set("Frozen", toLoad.IsFrozen);
            entity.Visible = toLoad.IsVisible;

            dynArrow.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynArrow.Set("MarkedForRemoval", toLoad.MarkedForRemoval);

            dynArrow.Set("Flashing", toLoad.Flash.IsFlashing);
            dynArrow.Set("flashCounter", toLoad.Flash.FlashCounter);
            dynArrow.Set("flashInterval", toLoad.Flash.FlashInterval);
            dynArrow.Set("onFinish", () => { entity.RemoveSelf(); });

            if (!toLoad.HasUnhittableEntity)
            {
                entity.CannotHit = null;
            }
        }

        public static void LoadCannotHit(this TowerFall.Arrow self, bool hasUnhittable, int playerIndex)
        {
            if (hasUnhittable)
            {
                self.CannotHit = self.Level.GetPlayerOrCorpse(playerIndex);
            }
            else
            {
                self.CannotHit = null;
            }
        }
    }
}
