using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;

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
                Side = entity.Side,
                IsCollidable = entity.Collidable,
                Position = entity.Position.ToModel(),
                Percent = entity.Percent,
                SineCounter = sine.Counter,
            };
        }

        public static void LoadState(this TowerFall.Lava entity, Lava toLoad)
        {
            var dynLava = DynamicData.For(entity);
            var sine = dynLava.Get<SineWave>("sine");

            dynLava.Set("Collidable", toLoad.IsCollidable);
            dynLava.Set("Position", toLoad.Position.ToTFVector());
            dynLava.Set("Percent", toLoad.Percent);
            sine.UpdateAttributes(toLoad.SineCounter);
        }
    }
}
