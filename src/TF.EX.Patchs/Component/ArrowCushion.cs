using HarmonyLib;
using TF.EX.Patchs.Calc;
using TowerFall;

namespace TF.EX.Patchs.Component
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
