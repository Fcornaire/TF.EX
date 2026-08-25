using HarmonyLib;
using TF.State.Domain.Context;
using TF.State.Patchs.Calc;
using TowerFall;

namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(MatchVariants))]
    public class MatchVariantsPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MatchVariants.Randomize))]
        public static bool MatchVariants_Randomize_Prefix(out bool __state)
        {
            if (StateFlags.IsRollbackFrame || StateFlags.HasFramesToReSimulate)
            {
                __state = false;
                return false;
            }

            CalcPatch.RegisterRng();
            __state = true;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(MatchVariants.Randomize))]
        public static void MatchVariants_Randomize_Postfix(bool __state)
        {
            if (__state)
            {
                CalcPatch.UnregisterRng();
            }
        }
    }
}
