using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows;
using TF.EX.TowerFallExtensions.Component;
using TF.EX.TowerFallExtensions.ComponentExtensions;

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
            var buriedIn = entity.BuriedIn;

            var builder = new ArrowBuilder();
            builder.WithActualDepth(actualDepth);
            builder.WithArrowType(entity.ArrowType);
            builder.WithDirection(entity.Direction);
            builder.WithPlayerIndex(entity.PlayerIndex);
            builder.WithPosition(entity.Position.ToModel());
            builder.WithShootingCounter(shootingCounter.Value);
            builder.WithCannotPickupCounter(cannotPickupCounter.Value);
            builder.WithCannotCatchCounter(cannotCatchCounter.Value);
            builder.WithSpeed(entity.Speed.ToModel());
            builder.WithState(entity.State);
            builder.WithStuckDirection(stuckDir.ToModel());
            builder.WithPositionCounter(counter.ToModel());
            builder.WithIsActive(entity.Active);
            builder.WithIsCollidable(entity.Collidable);
            builder.WithIsFrozen(entity.Frozen);
            builder.WithIsVisible(entity.Visible);
            builder.WithMarkedForRemoval(entity.MarkedForRemoval);
            builder.WithFireControl(entity.Fire.GetState());
            builder.WithFlash(new Domain.Models.State.Entity.LevelEntity.Flash
            {
                IsFlashing = entity.Flashing,
                FlashCounter = flashCounter,
                FlashInterval = flashInterval
            });
            builder.WithHasUnhittableEntity(entity.CannotHit != null);
            builder.WithBuriedIn(buriedIn?.GetState());

            switch (entity.ArrowType)
            {
                case TowerFall.ArrowTypes.Bomb:
                    var bombArrow = (TowerFall.BombArrow)entity;
                    var dynBombArrow = DynamicData.For(bombArrow);
                    bool canExplode = dynBombArrow.Get<bool>("canExplode");
                    Alarm explodeAlarm = dynBombArrow.Get<Alarm>("explodeAlarm");
                    Sprite<int> normalSprite = dynBombArrow.Get<Sprite<int>>("normalSprite");
                    Sprite<int> buriedSprite = dynBombArrow.Get<Sprite<int>>("buriedSprite");

                    builder.WithCanExplode(canExplode);
                    builder.WithExplodeAlarm(explodeAlarm.GetState());
                    builder.WithNormalSprite(normalSprite.GetState());
                    builder.WithBuriedSprite(buriedSprite.GetState());

                    break;
            }

            return builder.Build();
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

            var fireControl = dynArrow.Get<TowerFall.FireControl>("Fire");
            fireControl.LoadState(toLoad.FireControl);

            if (!toLoad.HasUnhittableEntity)
            {
                entity.CannotHit = null;
            }

            switch (toLoad.ArrowType)
            {
                case ArrowTypes.Bomb:
                    var bombArrow = (TowerFall.BombArrow)entity;
                    var dynBombArrow = DynamicData.For(bombArrow);
                    var toLoadBombArrow = (BombArrow)toLoad;

                    dynBombArrow.Set("canExplode", toLoadBombArrow.CanExplode);

                    var explodeAlarm = dynBombArrow.Get<Alarm>("explodeAlarm");
                    explodeAlarm.LoadState(toLoadBombArrow.ExplodeAlarm);

                    var normalSprite = dynBombArrow.Get<Sprite<int>>("normalSprite");
                    normalSprite.LoadState(toLoadBombArrow.NormalSprite);

                    var buriedSprite = dynBombArrow.Get<Sprite<int>>("buriedSprite");
                    buriedSprite.LoadState(toLoadBombArrow.BuriedSprite);

                    if (toLoadBombArrow.BuriedIn != null)
                    {
                        //TODO: Always a player corpse?
                        var arrowCushion = ((entity.Scene as TowerFall.Level).GetEntityByDepth(toLoad.BuriedIn.EntityActualDepth) as TowerFall.PlayerCorpse).ArrowCushion;

                        dynBombArrow.Set("BuriedIn", arrowCushion);
                        bombArrow.BuriedIn.LoadState(toLoadBombArrow.BuriedIn);
                    }

                    break;
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
