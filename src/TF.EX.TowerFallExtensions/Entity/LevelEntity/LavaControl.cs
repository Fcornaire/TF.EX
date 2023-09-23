using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models.State.Entity.LevelEntity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class LavaControlExtensions
    {
        public static LavaControl GetState(this TowerFall.LavaControl entity)
        {
            var dynLavaControl = DynamicData.For(entity);
            var lavas = dynLavaControl.Get<TowerFall.Lava[]>("lavas");

            var lavasToSave = new Lava[lavas.Length];

            for (int i = 0; i < lavasToSave.Length; i++)
            {
                var currentLava = lavas[i];
                var toSave = currentLava.GetState();
                lavasToSave[i] = toSave;
            }

            var mode = dynLavaControl.Get<TowerFall.LavaControl.LavaMode>("mode");
            var ownerIndex = entity.OwnerIndex;
            var targetCounter = dynLavaControl.Get<Counter>("targetCounter");
            var target = entity.Target;

            if (lavas[0].Percent <= 0 && !targetCounter) //Orb logic OnLavaFinish might have not been called yet
            {
                return null;
            }

            return new LavaControl
            {
                Mode = (LavaMode)mode,
                OwnerIndex = ownerIndex,
                TargetCounter = targetCounter.Value,
                Target = target,
                Lavas = lavasToSave
            };
        }

        public static void LoadState(this TowerFall.LavaControl entity, LavaControl toLoad)
        {
            var dynLavaControl = DynamicData.For(entity);
            var counter = dynLavaControl.Get<Counter>("targetCounter");
            var targetCounter = DynamicData.For(counter);
            targetCounter.Set("counter", toLoad.TargetCounter);

            entity.Target = toLoad.Target;

            var lavas = dynLavaControl.Get<TowerFall.Lava[]>("lavas");

            for (int i = 0; i < toLoad.Lavas.Length; i++)
            {
                var currentLava = toLoad.Lavas[i];
                lavas[i].LoadState(currentLava);
            }
        }
    }
}
