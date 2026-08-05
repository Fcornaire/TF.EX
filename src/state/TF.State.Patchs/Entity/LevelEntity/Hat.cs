using HarmonyLib;
using TF.State.Patchs.Calc;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.Crown))]
    internal class CrownRngPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void Crown_Update_Prefix() => CalcPatch.RegisterRng();

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Crown_Update_Postfix() => CalcPatch.UnregisterRng();
    }

    [HarmonyPatch(typeof(TowerFall.SteelHat))]
    internal class SteelHatRngPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void SteelHat_Update_Prefix() => CalcPatch.RegisterRng();

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void SteelHat_Update_Postfix() => CalcPatch.UnregisterRng();
    }

    [HarmonyPatch(typeof(TowerFall.WoodenHat))]
    internal class WoodenHatRngPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void WoodenHat_Update_Prefix() => CalcPatch.RegisterRng();

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void WoodenHat_Update_Postfix() => CalcPatch.UnregisterRng();
    }
}
