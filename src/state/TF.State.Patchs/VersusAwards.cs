using HarmonyLib;
using TF.State.Domain.Context;
using TF.State.Domain.Extensions;
using TowerFall;

namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(VersusAwards))]
    public class VersusAwardsPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(VersusAwards.GetAwards))]
        public static void VersusAwards_GetAwards_Prefix(MatchStats[] stats, out bool __state)
        {
            __state = StateFlags.IsCaptureActive && stats != null;

            if (__state)
            {
                Monocle.Calc.PushRandom(stats.GetAwardsSeed());
            }
        }

        [HarmonyFinalizer]
        [HarmonyPatch(nameof(VersusAwards.GetAwards))]
        public static void VersusAwards_GetAwards_Finalizer(bool __state)
        {
            if (__state)
            {
                Monocle.Calc.PopRandom();
            }
        }
    }
}
