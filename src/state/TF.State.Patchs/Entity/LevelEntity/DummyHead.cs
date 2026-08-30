using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.State.Patchs.Calc;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(DummyHead))]
    internal class DummyHeadPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Arrow), typeof(int)])]
        public static void DummyHead_ctor_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Arrow), typeof(int)])]
        public static void DummyHead_ctor_Postfix()
        {
            CalcPatch.UnregisterRng();
        }
    }
}
