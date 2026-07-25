using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class SlimeExtensions
    {
        public static Slime GetState(this TowerFall.Slime entity)
        {
            var dynSlime = DynamicData.For(entity);

            return new Slime
            {
                ActualDepth = dynSlime.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                PositionCounter = dynSlime.Get<Microsoft.Xna.Framework.Vector2>("counter").ToModel(),
                PreviousPosition = dynSlime.Get<Microsoft.Xna.Framework.Vector2>("previousPosition").ToModel(),
                Speed = entity.Speed.ToModel(),
                Facing = (int)entity.Facing,
                State = dynSlime.Get<int>("currentState"),
                SwitchToState = dynSlime.Get<int>("switchToState"),
                StateCounter = dynSlime.Get<float>("enemyStateCounter"),
                StatePhase = dynSlime.Get<int>("enemyStatePhase"),
                Health = entity.Health,
                KillerIndex = entity.KillerIndex,
                Dead = dynSlime.Get<bool>("dead"),
                IsCollidable = entity.Collidable,
                Seek = entity.Seek,
                SlimeColor = (int)dynSlime.Get<TowerFall.Slime.SlimeColors>("slimeColor"),
                Sprite = dynSlime.Get<Sprite<string>>("sprite").GetState(),
                ScaleSine = dynSlime.Get<SineWave>("scaleSine").Counter,
            };
        }

        public static void LoadState(this TowerFall.Slime entity, Slime toLoad)
        {
            var dynSlime = DynamicData.For(entity);
            dynSlime.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynSlime.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            dynSlime.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynSlime.Set("previousPosition", toLoad.PreviousPosition.ToTFVector());
            entity.Speed = toLoad.Speed.ToTFVector();
            entity.Facing = (TowerFall.Facing)toLoad.Facing;

            dynSlime.Set("currentState", toLoad.State);
            dynSlime.Set("switchToState", toLoad.SwitchToState);
            dynSlime.Set("enemyStateCounter", toLoad.StateCounter);
            dynSlime.Set("enemyStatePhase", toLoad.StatePhase);

            dynSlime.Set("Health", toLoad.Health);
            dynSlime.Set("KillerIndex", toLoad.KillerIndex);
            dynSlime.Set("dead", toLoad.Dead);
            entity.Collidable = toLoad.IsCollidable;
            entity.Seek = toLoad.Seek;

            var sprite = dynSlime.Get<Sprite<string>>("sprite");
            sprite.LoadState(toLoad.Sprite);
            dynSlime.Get<SineWave>("scaleSine").UpdateAttributes(toLoad.ScaleSine);

            entity.RestoreEnemyTags(!toLoad.Dead);

            if (toLoad.Dead)
            {
                entity.LightVisible = false;
                sprite.OnAnimationComplete = _ => entity.RemoveSelf();
            }
            else
            {
                entity.LightVisible = true;
                sprite.OnAnimationComplete = _ =>
                {
                    if (entity.State == 3)
                    {
                        entity.State = 0;
                    }
                };
            }

            dynSlime.Get<Coroutine>("coroutine")?.Cancel();
        }
    }
}
