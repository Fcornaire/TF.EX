using HarmonyLib;
using TF.State.Patchs.Calc;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Lava))]
    public class LavaPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("BubbleComplete")]
        public static void Lava_BubbleComplete_Prefix()
        {
            CalcPatch.IgnoreToRegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("BubbleComplete")]
        public static void Lava_BubbleComplete_Postfix()
        {
            CalcPatch.UnignoreToRegisterRng();
        }
    }
}
