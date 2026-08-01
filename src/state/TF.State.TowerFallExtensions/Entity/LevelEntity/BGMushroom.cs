using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity.Background;
using TF.State.TowerFallExtensions;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class BGMushroomExtensions
    {
        public static BGMushroom GetState(this TowerFall.BGMushroom entity)
        {
            var dynBGMushroom = DynamicData.For(entity);
            var actualDepth = dynBGMushroom.Get<double>("actualDepth");
            var sprite = entity.GetComponent<Sprite<int>>();

            return new BGMushroom
            {
                ActualDepth = actualDepth,
                Position = entity.Position.ToModel(),
                Sprite = sprite.GetState()
            };
        }

        public static void LoadState(this TowerFall.BGMushroom entity, BGMushroom toLoad)
        {
            var dynBGMushroom = DynamicData.For(entity);

            entity.Position = toLoad.Position.ToTFVector();
            dynBGMushroom.Set("actualDepth", toLoad.ActualDepth);

            var sprite = entity.GetComponent<Sprite<int>>();
            sprite.LoadState(toLoad.Sprite);
        }
    }
}
