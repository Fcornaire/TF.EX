using HarmonyLib;
using TF.State.Domain;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Arrow))]
    public class ArrowPatch
    {
        //TODO: Properly track arrow decay to remove this
        [HarmonyPrefix]
        [HarmonyPatch("EnforceLimit")]
        public static bool Arrow_EnforceLimit()
        {
            return !StateFlags.IsCaptureActive;
        }

        //TODO: remove this when a test without this is done
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Arrow.Removed))]
        public static void Arrow_Removed()
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            TowerFall.Arrow.FlushCache();
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoWrapRender")]
        public static void Arrow_DoWrapRender(Arrow __instance)
        {
            if (StateFlags.IsTestMode || StateFlags.IsReplayMode)
            {
                __instance.DebugRender();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Drop")]
        public static void Arrow_Drop_Prefix()
        {
            Calc.CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Drop")]
        public static void Arrow_Drop_Postfix()
        {
            Calc.CalcPatch.UnregisterRng();
        }

        [HarmonyPrefix]
        [HarmonyPatch("EnterFallMode")]
        public static void Arrow_EnterFallMode_Prefix()
        {
            Calc.CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("EnterFallMode")]
        public static void Arrow_EnterFallMode_Postfix()
        {
            Calc.CalcPatch.UnregisterRng();
        }
    }
}
