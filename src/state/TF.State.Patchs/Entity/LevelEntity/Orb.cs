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

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void Orb_Update_Prefix(Orb __instance, out Monocle.Component __state)
        {
            __state = null;

            if (!CosmeticFreeze.ShouldFreeze)
            {
                return;
            }

            var sprite = Traverse.Create(__instance).Field("sprite").GetValue<Monocle.Component>();

            if (sprite != null && sprite.Active)
            {
                sprite.Active = false;
                __state = sprite;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Orb_Update_Postfix(Monocle.Component __state)
        {
            if (__state != null)
            {
                __state.Active = true;
            }
        }
    }
}
