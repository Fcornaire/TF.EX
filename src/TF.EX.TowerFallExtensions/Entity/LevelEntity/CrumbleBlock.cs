using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class CrumbleBlockExtensions
    {
        public static CrumbleBlock GetState(this TowerFall.CrumbleBlock entity)
        {
            var dyn = DynamicData.For(entity);

            return new CrumbleBlock
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                Width = (int)entity.Width,
                Height = (int)entity.Height,
                IsActive = entity.Active,
                ExplodeCounter = dyn.Get<float>("explodeCounter"),
            };
        }

        public static void LoadState(this TowerFall.CrumbleBlock entity, CrumbleBlock toLoad)
        {
            var dyn = DynamicData.For(entity);

            if (entity.Level == null)
            {
                dyn.Set("Scene", TowerFall.TFGame.Instance.Scene);
                dyn.Set("Level", TowerFall.TFGame.Instance.Scene as TowerFall.Level);
            }

            entity.Position = toLoad.Position.ToTFVector();
            dyn.Set("actualDepth", toLoad.ActualDepth);
            dyn.Set("explodeCounter", toLoad.ExplodeCounter);
            entity.Active = toLoad.IsActive;
        }
    }
}
