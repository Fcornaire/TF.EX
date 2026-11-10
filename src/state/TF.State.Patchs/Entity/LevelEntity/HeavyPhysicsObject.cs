using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TF.State.Domain.Context;
using TF.State.Patchs.Calc;
using TF.State.TowerFallExtensions.Entity.LevelEntity;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.HeavyPhysicsObject))]
    internal class HeavyPhysicsObjectPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, typeof(Vector2), typeof(Subtexture), typeof(Monocle.SFX))]
        public static void HeavyPhysicsObject_ctor_Prefix(Subtexture subtexture, out bool __state)
        {
            __state = StateFlags.IsCaptureActive && !StateFlags.IsRestoring && HeavyPhysicsObjectExtensions.IsHaloTexture(subtexture);

            if (__state)
            {
                CalcPatch.RegisterRng();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, typeof(Vector2), typeof(Subtexture), typeof(Monocle.SFX))]
        public static void HeavyPhysicsObject_ctor_Postfix(TowerFall.HeavyPhysicsObject __instance, Subtexture subtexture, bool __state)
        {
            if (__state)
            {
                CalcPatch.UnregisterRng();
            }

            if (StateFlags.IsCaptureActive && HeavyPhysicsObjectExtensions.IsHaloTexture(subtexture))
            {
                __instance.Track();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool HeavyPhysicsObject_Update_Prefix(TowerFall.HeavyPhysicsObject __instance)
        {
            if (!__instance.IsTracked())
            {
                return !CosmeticFreeze.ShouldFreeze;
            }

            CalcPatch.RegisterRng();
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void HeavyPhysicsObject_Update_Postfix(TowerFall.HeavyPhysicsObject __instance)
        {
            if (!__instance.IsTracked())
            {
                return;
            }

            CalcPatch.UnregisterRng();
        }
    }
}
