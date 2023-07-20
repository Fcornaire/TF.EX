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

            return new LavaControl
            {
                Mode = mode,
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

            var lavas = new TowerFall.Lava[toLoad.Lavas.Length];

            for (int i = 0; i < toLoad.Lavas.Length; i++)
            {
                var currentLava = toLoad.Lavas[i];
                var lavaToLoad = new TowerFall.Lava(entity, currentLava.side);
                lavaToLoad.LoadState(currentLava);
                lavas[i] = lavaToLoad;
            }
            dynLavaControl.Set("lavas", lavas);
        }
    }
}
