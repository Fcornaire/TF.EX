using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class DummyHeadExtensions
    {
        public static DummyHead GetState(this TowerFall.DummyHead entity)
        {
            var dyn = DynamicData.For(entity);
            var image = dyn.Get<Image>("image");

            return new DummyHead
            {
                Position = entity.Position.ToModel(),
                ActualDepth = dyn.Get<double>("actualDepth"),
                Speed = dyn.Get<Vector2>("speed").ToModel(),
                RotateSign = dyn.Get<int>("rotateSign"),
                PositionCounter = dyn.Get<Vector2>("counter").ToModel(),
                ImageRotation = image?.Rotation ?? 0f,
                ImageScaleX = image?.Scale.X ?? 1f,
            };
        }

        public static void LoadState(this TowerFall.DummyHead entity, DummyHead toLoad)
        {
            var dyn = DynamicData.For(entity);

            entity.Position = toLoad.Position.ToTFVector();

            dyn.Set("actualDepth", toLoad.ActualDepth);
            dyn.Set("speed", toLoad.Speed.ToTFVector());
            dyn.Set("rotateSign", toLoad.RotateSign);
            dyn.Set("counter", toLoad.PositionCounter.ToTFVector());

            var image = dyn.Get<Image>("image");

            if (image != null)
            {
                image.Rotation = toLoad.ImageRotation;
                image.Scale.X = toLoad.ImageScaleX;
            }
        }
    }
}
