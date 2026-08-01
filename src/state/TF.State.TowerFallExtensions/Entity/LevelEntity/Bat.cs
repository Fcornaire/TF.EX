using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.TowerFallExtensions.Component;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class BatExtensions
    {
        public static Bat GetState(this TowerFall.Bat entity)
        {
            var dynBat = DynamicData.For(entity);
            var target = dynBat.Get<TowerFall.Player>("target");
            var bobSine = dynBat.Get<SineWave>("bobSine");

            return new Bat
            {
                ActualDepth = dynBat.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                PositionCounter = dynBat.Get<Microsoft.Xna.Framework.Vector2>("counter").ToModel(),
                PreviousPosition = dynBat.Get<Microsoft.Xna.Framework.Vector2>("previousPosition").ToModel(),
                Speed = entity.Speed.ToModel(),
                Facing = (int)entity.Facing,
                State = dynBat.Get<int>("currentState"),
                SwitchToState = dynBat.Get<int>("switchToState"),
                StateCounter = dynBat.Get<float>("enemyStateCounter"),
                StatePhase = dynBat.Get<int>("enemyStatePhase"),
                SwoopIndex = dynBat.Get<int>("enemySwoopIndex"),
                Health = entity.Health,
                KillerIndex = entity.KillerIndex,
                Dead = dynBat.Get<bool>("dead"),
                IsCollidable = entity.Collidable,
                Seek = entity.Seek,
                BatType = (int)dynBat.Get<TowerFall.Bat.BatType>("batType"),
                Sprite = dynBat.Get<Sprite<string>>("sprite").GetState(),
                BobSine = bobSine.Counter,
                BobSineActive = bobSine.Active,
                TargetIndex = target != null && target.Scene != null ? target.PlayerIndex : -1,
                SwoopTargetSpeed = dynBat.Get<Microsoft.Xna.Framework.Vector2>("swoopTargetSpeed").ToModel(),
                SwoopCooldown = dynBat.Get<Counter>("swoopCooldown").Value,
                IgnoreJumpThrus = entity.IgnoreJumpThrus,
                ArrowCushion = dynBat.Get<TowerFall.ArrowCushion>("arrowCushion").GetState(),
            };
        }

        public static void LoadState(this TowerFall.Bat entity, Bat toLoad)
        {
            var dynBat = DynamicData.For(entity);
            dynBat.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynBat.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            dynBat.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynBat.Set("previousPosition", toLoad.PreviousPosition.ToTFVector());
            entity.Speed = toLoad.Speed.ToTFVector();
            entity.Facing = (TowerFall.Facing)toLoad.Facing;

            dynBat.Set("currentState", toLoad.State);
            dynBat.Set("switchToState", toLoad.SwitchToState);
            dynBat.Set("enemyStateCounter", toLoad.StateCounter);
            dynBat.Set("enemyStatePhase", toLoad.StatePhase);
            dynBat.Set("enemySwoopIndex", toLoad.SwoopIndex);

            dynBat.Set("Health", toLoad.Health);
            dynBat.Set("KillerIndex", toLoad.KillerIndex);
            dynBat.Set("dead", toLoad.Dead);
            entity.Collidable = toLoad.IsCollidable;
            entity.Seek = toLoad.Seek;
            entity.IgnoreJumpThrus = toLoad.IgnoreJumpThrus;

            dynBat.Set("swoopTargetSpeed", toLoad.SwoopTargetSpeed.ToTFVector());
            DynamicData.For(dynBat.Get<Counter>("swoopCooldown")).Set("counter", toLoad.SwoopCooldown);

            var sprite = dynBat.Get<Sprite<string>>("sprite");
            sprite.LoadState(toLoad.Sprite);

            var bobSine = dynBat.Get<SineWave>("bobSine");
            bobSine.UpdateAttributes(toLoad.BobSine);
            bobSine.Active = toLoad.BobSineActive;

            var hitbox = dynBat.Get<TowerFall.WrapHitbox>("hitbox");
            if (toLoad.Dead)
            {
                hitbox.Width = 6f;
                hitbox.Position.X = -3f;
                hitbox.Height = 6f;
                hitbox.Position.Y = -3f;
            }
            else if ((TowerFall.Bat.BatType)toLoad.BatType == TowerFall.Bat.BatType.Bird)
            {
                hitbox.Width = 16f;
                hitbox.Position.X = -8f;
                hitbox.Height = 8f;
                hitbox.Position.Y = -6f;
            }
            else
            {
                hitbox.Width = 14f;
                hitbox.Position.X = -7f;
                hitbox.Height = 8f;
                hitbox.Position.Y = -6f;
            }

            entity.RestoreEnemyTags(!toLoad.Dead);
            entity.LightVisible = !toLoad.Dead;

            var arrowCushion = dynBat.Get<TowerFall.ArrowCushion>("arrowCushion");
            arrowCushion.LoadState(toLoad.ArrowCushion);
            arrowCushion.RemoveArrows();

            dynBat.Get<Coroutine>("coroutine")?.Cancel();
        }

        public static void LoadTarget(this TowerFall.Bat entity, Bat toLoad, TowerFall.Level level)
        {
            var target = toLoad.TargetIndex >= 0 ? level.GetPlayer(toLoad.TargetIndex) : null;
            DynamicData.For(entity).Set("target", target);
        }

        public static void LoadArrowCushionDatas(this TowerFall.Bat entity, Bat toLoad)
        {
            var arrowCushion = DynamicData.For(entity).Get<TowerFall.ArrowCushion>("arrowCushion");

            foreach (var arrowData in toLoad.ArrowCushion.ArrowCushionDatas.ToArray())
            {
                var gameArrow = (TowerFall.TFGame.Instance.Scene as TowerFall.Level).GetEntityByDepth(arrowData.ActualDepth) as TowerFall.Arrow;

                if (gameArrow != null)
                {
                    var inGameArrowData = new TowerFall.ArrowCushion.ArrowData
                    {
                        Arrow = gameArrow,
                        Offset = arrowData.Offset.ToTFVector(),
                        Rotation = arrowData.Rotation
                    };
                    arrowCushion.ArrowDatas.Add(inGameArrowData);
                }
            }
        }
    }
}
