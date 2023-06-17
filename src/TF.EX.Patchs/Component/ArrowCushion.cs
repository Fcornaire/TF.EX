using TF.EX.Patchs.Calc;

namespace TF.EX.Patchs.Component
{
    public class ArrowCushionPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.ArrowCushion.AddArrow += AddArrow_Patch;
        }

        public void Unload()
        {
            On.TowerFall.ArrowCushion.AddArrow -= AddArrow_Patch;
        }

        private void AddArrow_Patch(On.TowerFall.ArrowCushion.orig_AddArrow orig, TowerFall.ArrowCushion self, TowerFall.Arrow arrow, float moveIn, bool drawHead)
        {
            CalcPatch.ShouldRegisterRng = true;
            orig(self, arrow, moveIn, drawHead);
            CalcPatch.ShouldRegisterRng = false;
        }
    }
}
