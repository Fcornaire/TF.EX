using Monocle;

namespace TF.EX.Patchs.Background
{
    public class FadeLayerPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.Background.FadeLayer.UpdatePosition += FadeLayer_UpdatePosition;
        }

        public void Unload()
        {
            On.TowerFall.Background.FadeLayer.UpdatePosition -= FadeLayer_UpdatePosition;
        }

        private void FadeLayer_UpdatePosition(On.TowerFall.Background.FadeLayer.orig_UpdatePosition orig, TowerFall.Background.FadeLayer self, Sprite<int> s)
        {
            // Used by FadeLayer, but this largely increase the number of RNG calls which is not good for performance, uncomment for test mode

            //Calc.CalcPatch.RegisterRng();
            orig(self, s);
            //Calc.CalcPatch.UnregisterRng();
        }
    }
}
