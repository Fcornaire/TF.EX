using Monocle;
using MonoMod.Utils;

namespace TF.EX.Patchs.Layer
{
    internal class LightningFlashLayerPatch : IHookable
    {
        private readonly Random _random = new Random();

        public void Load()
        {
            On.TowerFall.Background.LightningFlashLayer.SequenceB += LightningFlashLayer_SequenceB;
            On.TowerFall.Background.LightningFlashLayer.SequenceC += LightningFlashLayer_SequenceC;
            On.TowerFall.Background.LightningFlashLayer.SequenceD += LightningFlashLayer_SequenceD;

        }

        public void Unload()
        {
            On.TowerFall.Background.LightningFlashLayer.SequenceB -= LightningFlashLayer_SequenceB;
            On.TowerFall.Background.LightningFlashLayer.SequenceC -= LightningFlashLayer_SequenceC;
            On.TowerFall.Background.LightningFlashLayer.SequenceD -= LightningFlashLayer_SequenceD;
        }

        //Use custom random range to avoid using calc random which is used deterministically
        private void LightningFlashLayer_SequenceD(On.TowerFall.Background.LightningFlashLayer.orig_SequenceD orig, TowerFall.Background.LightningFlashLayer self)
        {
            orig(self);

            var dynLightningFlashLayer = DynamicData.For(self);
            var alarm = dynLightningFlashLayer.Get<Alarm>("alarm");

            alarm.Start(_random.Range(500, 800));
        }

        private void LightningFlashLayer_SequenceC(On.TowerFall.Background.LightningFlashLayer.orig_SequenceC orig, TowerFall.Background.LightningFlashLayer self)
        {
            orig(self);

            var dynLightningFlashLayer = DynamicData.For(self);
            var alarm = dynLightningFlashLayer.Get<Alarm>("alarm");

            alarm.Start(_random.Range(4, 10));
        }

        private void LightningFlashLayer_SequenceB(On.TowerFall.Background.LightningFlashLayer.orig_SequenceB orig, TowerFall.Background.LightningFlashLayer self)
        {
            orig(self);

            var dynLightningFlashLayer = DynamicData.For(self);
            var alarm = dynLightningFlashLayer.Get<Alarm>("alarm");

            alarm.Start(_random.Range(6, 10));
        }


    }
}
