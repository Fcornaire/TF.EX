using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Layer;

namespace TF.EX.TowerFallExtensions
{
    public static class BGElementExtensions
    {
        public static BackgroundElement GetState(this TowerFall.Background.ScrollLayer scrollBgElement, int index)
        {
            return new BackgroundElement
            {
                index = index,
                Position = scrollBgElement.Image.Position.ToModel()
            };
        }

        public static ForegroundElement GetState(this TowerFall.Background.WavyLayer wavyElement, int index)
        {
            var dynWavyLayer = DynamicData.For(wavyElement);
            var counter = dynWavyLayer.Get<float>("counter");

            return new ForegroundElement
            {
                index = index,
                counter = counter,
            };
        }
    }
}
