using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.State.Patchs.Calc;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Chain))]
    internal class ChainPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(int)])]
        public static void Chain_ctor_Vector2_int_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(int)])]
        public static void Chain_ctor_Vector2_int_Postfix()
        {
            CalcPatch.UnregisterRng();
        }
    }
}
