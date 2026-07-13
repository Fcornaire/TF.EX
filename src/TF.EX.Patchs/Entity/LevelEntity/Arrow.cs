using HarmonyLib;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Arrow))]
    public class ArrowPatch
    {
        //TODO: Properly track arrow decay to remove this
        [HarmonyPrefix]
        [HarmonyPatch("EnforceLimit")]
        public static bool Arrow_EnforceLimit()
        {
            //Console.WriteLine("Arrow EnforceLimit Ignore for now");
            return false;
        }

        //TODO: remove this when a test without this is done
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Arrow.Removed))]
        public static void Arrow_Removed()
        {
            TowerFall.Arrow.FlushCache();
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoWrapRender")]
        public static void Arrow_DoWrapRender(Arrow __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (netplayManager.IsTestMode() || netplayManager.IsReplayMode())
            {
                __instance.DebugRender();
            }
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
