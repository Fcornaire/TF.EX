using Monocle;
using TF.EX.Patchs.Calc;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class LavaPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.Lava.BubbleComplete += Lava_BubbleComplete;
        }

        public void Unload()
        {
            On.TowerFall.Lava.BubbleComplete -= Lava_BubbleComplete;
        }

        private void Lava_BubbleComplete(On.TowerFall.Lava.orig_BubbleComplete orig, TowerFall.Lava self, Sprite<int> bubble)
        {
            CalcPatch.IgnoreToRegisterRng();
            orig(self, bubble);
            CalcPatch.UnignoreToRegisterRng();
        }
    }
}
