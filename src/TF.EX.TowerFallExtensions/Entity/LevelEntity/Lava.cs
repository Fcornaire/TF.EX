using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.LevelEntity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class LavaExtensions
    {
        public static Lava GetState(this TowerFall.Lava entity)
        {
            var dynLava = DynamicData.For(entity);
            var sine = dynLava.Get<SineWave>("sine");

            return new Lava
            {
                side = entity.Side,
                is_collidable = entity.Collidable,
                position = entity.Position.ToModel(),
                percent = entity.Percent,
                sine_counter = sine.Counter,
            };
        }

        public static void LoadState(this TowerFall.Lava entity, Lava toLoad)
        {
            var dynLava = DynamicData.For(entity);
            var sine = dynLava.Get<SineWave>("sine");

            dynLava.Set("Collidable", toLoad.is_collidable);
            dynLava.Set("Position", toLoad.position.ToTFVector());
            dynLava.Set("Percent", toLoad.percent);
            sine.UpdateAttributes(toLoad.sine_counter);
        }

    }
}
