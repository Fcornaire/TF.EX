using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.State.Patchs.Calc;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Orb))]
    internal class OrbPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(bool)])]
        public static void Orb_ctor_Vector2_bool_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(bool)])]
        public static void Orb_ctor_Vector2_bool_PostFix(Orb __instance)
        {
            CalcPatch.UnregisterRng();

            if (TF.State.Domain.Context.StateFlags.IsCaptureActive)
            {
                Traverse.Create(__instance).Field("ownerIndex").SetValue(-1);
            }
        }
    }
}
