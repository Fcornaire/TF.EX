using MonoMod.Utils;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;
using TF.EX.TowerFallExtensions.ComponentExtensions;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class ProximityBlockExtensions
    {
        public static ProximityBlock GetState(this TowerFall.ProximityBlock entity)
        {
            var dyn = DynamicData.For(entity);

            return new ProximityBlock
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                IsCollidable = entity.Collidable,
                Transitioning = dyn.Get<bool>("transitioning"),
                DisappearTween = dyn.Get<Monocle.Tween>("disappearTween").GetState(),
                AppearTween = dyn.Get<Monocle.Tween>("appearTween").GetState(),
            };
        }

        public static void LoadState(this TowerFall.ProximityBlock entity, ProximityBlock toLoad)
        {
            var dyn = DynamicData.For(entity);

            entity.Collidable = toLoad.IsCollidable;
            dyn.Set("transitioning", toLoad.Transitioning);

            dyn.Get<Monocle.Tween>("disappearTween").LoadState(toLoad.DisappearTween);
            dyn.Get<Monocle.Tween>("appearTween").LoadState(toLoad.AppearTween);
        }
    }
}
