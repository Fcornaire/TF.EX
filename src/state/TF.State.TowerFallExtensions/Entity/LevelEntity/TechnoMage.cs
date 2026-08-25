using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.TowerFallExtensions.ComponentExtensions;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class TechnoMageExtensions
    {
        private const int STATE_DEAD = 2;

        public static TechnoMage GetState(this TowerFall.TechnoMage entity)
        {
            var dynMage = DynamicData.For(entity);
            var target = dynMage.Get<TowerFall.Player>("target");
            var moveSine = dynMage.Get<SineWave>("moveSine");

            return new TechnoMage
            {
                ActualDepth = dynMage.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                PositionCounter = dynMage.Get<Microsoft.Xna.Framework.Vector2>("counter").ToModel(),
                PreviousPosition = dynMage.Get<Microsoft.Xna.Framework.Vector2>("previousPosition").ToModel(),
                Speed = entity.Speed.ToModel(),
                Facing = (int)entity.Facing,
                State = dynMage.Get<int>("currentState"),
                SwitchToState = dynMage.Get<int>("switchToState"),
                StateCounter = dynMage.Get<float>("enemyStateCounter"),
                StatePhase = dynMage.Get<int>("enemyStatePhase"),
                ShotIndex = dynMage.Get<int>("enemyShotIndex"),
                Health = entity.Health,
                KillerIndex = entity.KillerIndex,
                Dead = dynMage.Get<bool>("dead"),
                IsCollidable = entity.Collidable,
                Seek = entity.Seek,
                Sprite = dynMage.Get<Sprite<string>>("sprite").GetState(),
                TargetSpeed = dynMage.Get<Microsoft.Xna.Framework.Vector2>("targetSpeed").ToModel(),
                MoveSineCounter = moveSine.Counter,
                MoveSineRate = moveSine.Rate,
                TargetIndex = target != null && target.Scene != null ? target.PlayerIndex : -1,
                TrackedState = dynMage.Get<int>("enemyTrackedState"),
            };
        }

        public static void LoadState(this TowerFall.TechnoMage entity, TechnoMage toLoad)
        {
            var dynMage = DynamicData.For(entity);
            dynMage.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynMage.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            dynMage.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynMage.Set("previousPosition", toLoad.PreviousPosition.ToTFVector());
            entity.Speed = toLoad.Speed.ToTFVector();
            entity.Facing = (TowerFall.Facing)toLoad.Facing;

            dynMage.Set("currentState", toLoad.State);
            dynMage.Set("switchToState", toLoad.SwitchToState);
            dynMage.Set("enemyStateCounter", toLoad.StateCounter);
            dynMage.Set("enemyStatePhase", toLoad.StatePhase);
            dynMage.Set("enemyShotIndex", toLoad.ShotIndex);
            dynMage.Set("enemyTrackedState", toLoad.TrackedState);

            dynMage.Set("Health", toLoad.Health);
            dynMage.Set("KillerIndex", toLoad.KillerIndex);
            dynMage.Set("dead", toLoad.Dead);
            entity.Collidable = toLoad.IsCollidable;
            entity.Seek = toLoad.Seek;

            dynMage.Set("targetSpeed", toLoad.TargetSpeed.ToTFVector());

            var moveSine = dynMage.Get<SineWave>("moveSine");
            moveSine.Rate = toLoad.MoveSineRate;
            moveSine.LoadState(new TF.State.Domain.Models.Component.SineWave { Counter = toLoad.MoveSineCounter });

            dynMage.Get<Sprite<string>>("sprite").LoadState(toLoad.Sprite);

            var collider = toLoad.State == STATE_DEAD
                ? new TowerFall.WrapHitbox(12f, 12f, -6f, -6f)
                : new TowerFall.WrapHitbox(12f, 14f, -6f, -7f);
            entity.Collider = collider;

            entity.RestoreEnemyTags(!toLoad.Dead);
            entity.LightVisible = !toLoad.Dead;

            entity.DeleteAllComponents<Coroutine>();
        }

        public static void LoadTarget(this TowerFall.TechnoMage entity, TechnoMage toLoad, TowerFall.Level level)
        {
            var target = toLoad.TargetIndex >= 0 ? level.GetPlayer(toLoad.TargetIndex) : null;
            if (target != null)
            {
                DynamicData.For(entity).Set("target", target);
            }
        }
    }
}
