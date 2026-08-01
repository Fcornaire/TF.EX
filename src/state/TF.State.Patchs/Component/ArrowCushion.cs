using HarmonyLib;
using TF.State.Patchs.Calc;
using TowerFall;

namespace TF.State.Patchs.Component
{
    [HarmonyPatch(typeof(ArrowCushion))]
    public class ArrowCushionPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("AddArrow")]
        public static void AddArrow_Patch_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("AddArrow")]
        public static void AddArrow_Patch_Postfix()
        {
            CalcPatch.UnregisterRng();
        }
    }
}
