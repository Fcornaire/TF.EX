using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class HatExtensions
    {
        public static Hat GetState(this TowerFall.Hat entity)
        {
            var dynHat = DynamicData.For(entity);
            var image = dynHat.Get<Image>("image");
            var sine = entity is TowerFall.DefaultHat ? dynHat.Get<SineWave>("sine") : null;

            return new Hat
            {
                ActualDepth = dynHat.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                PositionCounter = dynHat.Get<Microsoft.Xna.Framework.Vector2>("counter").ToModel(),
                Speed = entity.Speed.ToModel(),
                PlayerIndex = entity.PlayerIndex,
                HatState = (int)entity.HatState,
                Spin = sine == null ? dynHat.Get<float>("spin") : 0f,
                SineCounter = sine?.Counter ?? 0f,
                SineRate = sine?.Rate ?? 0f,
                SineValue = sine?.Value ?? 0f,
                Rotation = image?.Rotation ?? 0f,
                Flipped = image?.FlipX ?? false,
            };
        }

        public static void LoadState(this TowerFall.Hat entity, Hat toLoad)
        {
            var dynHat = DynamicData.For(entity);
            dynHat.Set("Scene", TowerFall.TFGame.Instance.Scene);

            if (!toLoad.Pending)
            {
                entity.Added();
            }

            dynHat.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            dynHat.Set("counter", toLoad.PositionCounter.ToTFVector());
            entity.Speed = toLoad.Speed.ToTFVector();

            var sine = entity is TowerFall.DefaultHat ? dynHat.Get<SineWave>("sine") : null;

            if (sine != null)
            {
                sine.Rate = toLoad.SineRate;
                var dynSine = DynamicData.For(sine);
                dynSine.Set("Counter", toLoad.SineCounter);
                dynSine.Set("Value", toLoad.SineValue);
            }
            else
            {
                dynHat.Set("spin", toLoad.Spin);
            }

            var image = dynHat.Get<Image>("image");

            if (image != null)
            {
                image.Rotation = toLoad.Rotation;
                image.FlipX = toLoad.Flipped;
            }
        }
    }
}
