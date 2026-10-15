using HarmonyLib;
using TowerFall;

namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(UnlockData))]
    public class UnlockDataPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(UnlockData.ShouldOpenPurpleArcherPortal), MethodType.Getter)]
        public static bool UnlockData_ShouldOpenPurpleArcherPortal_Prefix(ref bool __result)
        {
            return SkipInExSession(ref __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UnlockData.ShouldShowMoonBreakSequence), MethodType.Getter)]
        public static bool UnlockData_ShouldShowMoonBreakSequence_Prefix(ref bool __result)
        {
            return SkipInExSession(ref __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UnlockData.ShouldSelectSunkenCity), MethodType.Getter)]
        public static bool UnlockData_ShouldSelectSunkenCity_Prefix(ref bool __result)
        {
            return SkipInExSession(ref __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UnlockData.ShouldSelectTowerForgeInVersus), MethodType.Getter)]
        public static bool UnlockData_ShouldSelectTowerForgeInVersus_Prefix(ref bool __result)
        {
            return SkipInExSession(ref __result);
        }

        private static bool SkipInExSession(ref bool __result)
        {
            if (!LocalSettingsGuard.IsExSession())
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
