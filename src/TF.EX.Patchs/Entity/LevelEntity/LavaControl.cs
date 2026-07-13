using HarmonyLib;
using TF.EX.Patchs.Calc;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(LavaControl))]
    public class LavaControlPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Extend")]
        public static void LavaControl_Extend_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Extend")]
        public static void LavaControl_Extend_Postfix()
        {
            CalcPatch.UnregisterRng();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Added")]
        public static void LavaControl_Added_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Added")]
        public static void LavaControl_Added_Postfix()
        {
            CalcPatch.UnregisterRng();
        }

        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, [typeof(LavaControl.LavaMode), typeof(int)])]
        public static void LavaControl_ctor_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(LavaControl.LavaMode), typeof(int)])]
        public static void LavaControl_ctor_Postfix()
        {
            CalcPatch.UnregisterRng();
        }
    }
}
