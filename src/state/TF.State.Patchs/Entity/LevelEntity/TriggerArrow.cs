using HarmonyLib;
using TF.State.Patchs.Calc;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.TriggerArrow))]
    internal class TriggerArrowRngPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("PlayEyeAnim")]
        public static void TriggerArrow_PlayEyeAnim_Prefix() => CalcPatch.RegisterRng();

        [HarmonyPostfix]
        [HarmonyPatch("PlayEyeAnim")]
        public static void TriggerArrow_PlayEyeAnim_Postfix() => CalcPatch.UnregisterRng();
    }
}
