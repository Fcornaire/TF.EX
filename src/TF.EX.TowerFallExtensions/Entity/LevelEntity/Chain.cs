using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.LevelEntity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class ChainExtensions
    {
        public static Chain GetState(this TowerFall.Chain chain)
        {
            var dynChain = DynamicData.For(chain);
            var links = dynChain.Get<Image[]>("links");
            var cannotHitCounter = dynChain.Get<Counter>("cannotHitCounter");
            var bottoms = dynChain.Get<Vector2[]>("bottoms");
            var speeds = dynChain.Get<float[]>("speeds");
            var actualDepth = dynChain.Get<double>("actualDepth");
            var position = dynChain.Get<Vector2>("Position");

            var rotations = new float[speeds.Length];
            for (int i = 0; i < speeds.Length; i++)
            {
                rotations[i] = links[i].Rotation;
            }

            return new Chain
            {
                ActualDepth = actualDepth,
                Bottoms = bottoms.ToModel(),
                CannotHitCounter = cannotHitCounter.Value,
                Position = position.ToModel(),
                Speeds = speeds,
                Rotations = rotations
            };
        }

        public static void LoadState(this TowerFall.Chain chain, Chain toLoad)
        {
            var dynChain = DynamicData.For(chain);
            var cannotHit = dynChain.Get<Counter>("cannotHitCounter");
            var cannotHitCounter = DynamicData.For(cannotHit);

            var links = dynChain.Get<Image[]>("links");
            float counter;
            Vector2[] bottoms;
            float[] speeds;
            double actualDepth;
            Vector2 position;

            bottoms = toLoad.Bottoms.ToTFVector();
            counter = toLoad.CannotHitCounter;
            position = toLoad.Position.ToTFVector();
            speeds = toLoad.Speeds;
            actualDepth = toLoad.ActualDepth;
            for (int i = 0; i < toLoad.Rotations.Length; i++)
            {
                links[i].Rotation = toLoad.Rotations[i];
            }

            cannotHitCounter.Set("counter", counter);
            dynChain.Set("bottoms", bottoms);
            dynChain.Set("speeds", speeds);
            dynChain.Set("actualDepth", actualDepth);
            dynChain.Set("position", position);
            dynChain.Set("links", links);
        }
    }
}
