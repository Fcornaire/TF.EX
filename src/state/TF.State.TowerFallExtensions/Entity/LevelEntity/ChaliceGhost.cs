using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.TowerFallExtensions.ComponentExtensions;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class ChaliceGhostExtensions
    {
        public static ChaliceGhost GetState(this TowerFall.ChaliceGhost entity)
        {
            var dynGhost = DynamicData.For(entity);
            var target = dynGhost.Get<TowerFall.Player>("target");

            return new ChaliceGhost
            {
                ActualDepth = dynGhost.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                Speed = dynGhost.Get<Microsoft.Xna.Framework.Vector2>("speed").ToModel(),
                Lerp = dynGhost.Get<float>("lerp"),
                OwnerIndex = dynGhost.Get<int>("ownerIndex"),
                Team = (int)dynGhost.Get<TowerFall.Allegiance>("team"),
                TargetIndex = target != null && target.Scene != null ? target.PlayerIndex : -1,
                CanFindTarget = dynGhost.Get<bool>("canFindTarget"),
                Dead = dynGhost.Get<bool>("dead"),
                Spawned = dynGhost.Get<bool>("spawned"),
                IsCollidable = entity.Collidable,
                Sprite = dynGhost.Get<Sprite<string>>("sprite").GetState(),
                Wiggler = dynGhost.Get<Monocle.Wiggler>("wiggler").GetState(),
                Phase = dynGhost.Get<int>("chaliceGhostPhase"),
                PhaseCounter = dynGhost.Get<float>("chaliceGhostCounter"),
                AttackCooldown = dynGhost.Get<float>("chaliceGhostAttackCooldown"),
            };
        }

        public static void LoadState(this TowerFall.ChaliceGhost entity, ChaliceGhost toLoad)
        {
            var dynGhost = DynamicData.For(entity);
            dynGhost.Set("Scene", TowerFall.TFGame.Instance.Scene);
            dynGhost.Set("ownerIndex", toLoad.OwnerIndex);
            entity.Added();

            dynGhost.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            dynGhost.Set("speed", toLoad.Speed.ToTFVector());
            dynGhost.Set("lerp", toLoad.Lerp);
            dynGhost.Set("team", (TowerFall.Allegiance)toLoad.Team);
            dynGhost.Set("canFindTarget", toLoad.CanFindTarget);
            dynGhost.Set("dead", toLoad.Dead);
            dynGhost.Set("spawned", toLoad.Spawned);
            entity.Collidable = toLoad.IsCollidable;

            dynGhost.Get<Sprite<string>>("sprite").LoadState(toLoad.Sprite);
            dynGhost.Get<Monocle.Wiggler>("wiggler").LoadState(toLoad.Wiggler);

            dynGhost.Set("chaliceGhostPhase", toLoad.Phase);
            dynGhost.Set("chaliceGhostCounter", toLoad.PhaseCounter);
            dynGhost.Set("chaliceGhostAttackCooldown", toLoad.AttackCooldown);

            entity.DeleteAllComponents<Coroutine>();
            entity.DeleteAllComponents<Alarm>();
        }

        public static void LoadTarget(this TowerFall.ChaliceGhost entity, ChaliceGhost toLoad, TowerFall.Level level)
        {
            var target = toLoad.TargetIndex >= 0 ? level.GetPlayer(toLoad.TargetIndex) : null;
            DynamicData.For(entity).Set("target", target);
        }
    }
}
