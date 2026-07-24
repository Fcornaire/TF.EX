using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class PrismExtensions
    {
        public static Prism GetState(this TowerFall.Prism entity)
        {
            var dyn = DynamicData.For(entity);

            return new Prism
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                Counter = dyn.Get<float>("counter"),
                Finished = dyn.Get<bool>("finished"),
                StartedShaking = dyn.Get<bool>("startedShaking"),
                OwnerIndex = entity.OwnerIndex,
                EncasedPlayerIndex = entity.EncasedPlayer != null ? entity.EncasedPlayer.PlayerIndex : -1,
                IsCollidable = entity.Collidable,
                OnlyCollidableSwitch = entity.OnlyCollidableSwitch,
                OnlyPlayerIndex = entity.OnlyPlayerIndex,
            };
        }

        public static void LoadState(this TowerFall.Prism entity, Prism toLoad)
        {
            var dyn = DynamicData.For(entity);

            if (entity.Level == null)
            {
                dyn.Set("Scene", TowerFall.TFGame.Instance.Scene);
                dyn.Set("Level", TowerFall.TFGame.Instance.Scene as TowerFall.Level);
            }

            entity.Position = toLoad.Position.ToTFVector();
            dyn.Set("actualDepth", toLoad.ActualDepth);
            dyn.Set("counter", toLoad.Counter);
            dyn.Set("finished", toLoad.Finished);
            dyn.Set("startedShaking", toLoad.StartedShaking);
            dyn.Set("OwnerIndex", toLoad.OwnerIndex);
            entity.Collidable = toLoad.IsCollidable;
            entity.OnlyCollidableSwitch = toLoad.OnlyCollidableSwitch;
            entity.OnlyPlayerIndex = toLoad.OnlyPlayerIndex;
        }
    }
}
