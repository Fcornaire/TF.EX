using HarmonyLib;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Rendering
{
    [HarmonyPatch(typeof(Monocle.Engine))]
    internal static class SmoothRenderingDrawPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Draw")]
        public static void Engine_Draw_Prefix(Monocle.Engine __instance)
        {
            if (!StateFlags.SmoothRendering)
            {
                return;
            }

            RenderInterpolation.Apply(__instance.Scene, StateFlags.InterpolationAlpha);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Draw")]
        public static void Engine_Draw_Postfix()
        {
            RenderInterpolation.Restore();
        }
    }

    [HarmonyPatch(typeof(Level))]
    internal static class SmoothRenderingCapturePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Level.Update))]
        public static void Level_Update_Postfix(Level __instance)
        {
            if (!StateFlags.SmoothRendering)
            {
                return;
            }

            RenderInterpolation.Capture(__instance);
        }
    }
}
