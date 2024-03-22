using Monocle;
using TF.EX.Domain.Ports;

namespace TF.EX.Patchs.Background
{
    public class FadeLayerPatch : IHookable
    {
        private readonly INetplayManager netplayManager;

        public FadeLayerPatch(INetplayManager netplayManager)
        {
            this.netplayManager = netplayManager;
        }

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
            if (netplayManager.IsInit())
            {
                self.Sprite.Position = self.Position - self.Range;
                self.Sprite.Rate = 0.6f;
            }
            else
            {
                orig(self, s);
            }
        }
    }
}
