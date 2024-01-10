using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Layer;
using TF.EX.TowerFallExtensions;

namespace TF.EX.TowerFallExtensions
{
    public static class BGElementExtensions
    {
        public static BGElement GetState(this TowerFall.Background.BGElement bgElement, int index)
        {
            var counter = 0.0f;
            var position = new Vector2f();
            var sprite = new Sprite<int>();
            var sineCounter = 0.0f;

            if (bgElement is TowerFall.Background.ScrollLayer)
            {
                position = (bgElement as TowerFall.Background.ScrollLayer).Image.Position.ToModel();
            }

            if (bgElement is TowerFall.Background.WavyLayer)
            {
                var dynWavyLayer = DynamicData.For(bgElement);
                counter = dynWavyLayer.Get<float>("counter");
            }

            if (bgElement is TowerFall.Background.FadeLayer)
            {
                sprite = (bgElement as TowerFall.Background.FadeLayer).Sprite.GetState();
                var dynFadeLayer = DynamicData.For(bgElement);
                sineCounter = dynFadeLayer.Get<Monocle.SineWave>("sine").Counter;
            }

            return new BGElement
            {
                Index = index,
                ScrollLayer_Position = position,
                WavyLayer_Counter = counter,
                FadeLayer_Sprite = sprite,
                FadeLayer_SineCounter = sineCounter
            };
        }

        public static void LoadState(this TowerFall.Background.BGElement bGElement, BGElement state)
        {
            if (bGElement is TowerFall.Background.ScrollLayer)
            {
                (bGElement as TowerFall.Background.ScrollLayer).Image.Position = state.ScrollLayer_Position.ToTFVector();
                return;
            }

            if (bGElement is TowerFall.Background.WavyLayer)
            {
                var dynWavyLayer = DynamicData.For(bGElement);
                dynWavyLayer.Set("counter", state.WavyLayer_Counter);
                return;
            }

            if (bGElement is TowerFall.Background.FadeLayer)
            {
                (bGElement as TowerFall.Background.FadeLayer).Sprite.LoadState(state.FadeLayer_Sprite);
                var dynFadeLayer = DynamicData.For(bGElement);
                dynFadeLayer.Get<Monocle.SineWave>("sine").UpdateAttributes(state.FadeLayer_SineCounter);
                return;
            }
        }
    }
}
