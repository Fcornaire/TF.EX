using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;
using TF.EX.TowerFallExtensions.Entity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class PlayerGhostExtensions
    {
        public static PlayerGhost GetState(this TowerFall.PlayerGhost entity)
        {
            var dynPlayerGhost = DynamicData.For(entity);
            var lastDir = dynPlayerGhost.Get<float?>("lastDir");
            var despawnCorpse = dynPlayerGhost.Get<TowerFall.PlayerCorpse>("despawnCorpse");

            return new PlayerGhost
            {
                ActualDepth = dynPlayerGhost.Get<double>("actualDepth"),
                PlayerIndex = entity.PlayerIndex,
                Position = entity.Position.ToModel(),
                PositionCounter = dynPlayerGhost.Get<Microsoft.Xna.Framework.Vector2>("counter").ToModel(),
                Speed = entity.Speed.ToModel(),
                Facing = (int)entity.Facing,
                State = dynPlayerGhost.Get<int>("currentState"),
                SwitchToState = dynPlayerGhost.Get<int>("switchToState"),
                StateCounter = dynPlayerGhost.Get<float>("ghostStateCounter"),
                Sine = dynPlayerGhost.Get<SineWave>("sine").Counter,
                LastDir = lastDir ?? 0f,
                HasLastDir = lastDir.HasValue,
                MoveMax = dynPlayerGhost.Get<float>("moveMax"),
                DodgeCooldown = dynPlayerGhost.Get<Counter>("dodgeCooldown").Value,
                KillDelayCounter = dynPlayerGhost.Get<Counter>("killDelayCounter").Value,
                Health = entity.Health,
                IsCollidable = entity.Collidable,
                Seek = entity.Seek,
                Sprite = dynPlayerGhost.Get<Sprite<string>>("sprite").GetState(),
                DespawnCorpseActualDepth = despawnCorpse != null
                    ? DynamicData.For(despawnCorpse).Get<double>("actualDepth")
                    : -1.0,
                DespawnStart = dynPlayerGhost.Get<Microsoft.Xna.Framework.Vector2>("ghostDespawnStart").ToModel(),
            };
        }

        public static void LoadState(this TowerFall.PlayerGhost entity, PlayerGhost toLoad)
        {
            var dynPlayerGhost = DynamicData.For(entity);
            dynPlayerGhost.Set("Scene", TowerFall.TFGame.Instance.Scene);

            dynPlayerGhost.Set("PlayerIndex", toLoad.PlayerIndex);

            entity.Added();

            dynPlayerGhost.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            dynPlayerGhost.Set("counter", toLoad.PositionCounter.ToTFVector());
            entity.Speed = toLoad.Speed.ToTFVector();
            entity.Facing = (TowerFall.Facing)toLoad.Facing;

            dynPlayerGhost.Set("currentState", toLoad.State);
            dynPlayerGhost.Set("switchToState", toLoad.SwitchToState);
            dynPlayerGhost.Set("ghostStateCounter", toLoad.StateCounter);
            dynPlayerGhost.Set("ghostDespawnStart", toLoad.DespawnStart.ToTFVector());

            dynPlayerGhost.Get<SineWave>("sine").UpdateAttributes(toLoad.Sine);
            dynPlayerGhost.Set("lastDir", toLoad.HasLastDir ? toLoad.LastDir : (float?)null);
            dynPlayerGhost.Set("moveMax", toLoad.MoveMax);

            DynamicData.For(dynPlayerGhost.Get<Counter>("dodgeCooldown")).Set("counter", toLoad.DodgeCooldown);
            DynamicData.For(dynPlayerGhost.Get<Counter>("killDelayCounter")).Set("counter", toLoad.KillDelayCounter);

            dynPlayerGhost.Set("Health", toLoad.Health);
            entity.Collidable = toLoad.IsCollidable;
            entity.Seek = toLoad.Seek;

            dynPlayerGhost.Get<Sprite<string>>("sprite").LoadState(toLoad.Sprite);

            entity.DeleteAllComponents<Tween>();
            dynPlayerGhost.Get<Coroutine>("coroutine")?.Cancel();
        }

        public static void LoadDespawnCorpse(this TowerFall.PlayerGhost entity, PlayerGhost toLoad, TowerFall.Level level)
        {
            var despawnCorpse = toLoad.DespawnCorpseActualDepth >= 0.0
                ? level.GetEntityByDepth(toLoad.DespawnCorpseActualDepth) as TowerFall.PlayerCorpse
                : null;

            DynamicData.For(entity).Set("despawnCorpse", despawnCorpse);
        }
    }
}
