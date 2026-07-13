using HarmonyLib;
using TF.EX.Patchs.Calc;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
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
