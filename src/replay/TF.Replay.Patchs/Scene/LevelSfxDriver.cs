using HarmonyLib;
using MonoMod.Utils;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    [HarmonyPatch(typeof(Level))]
    internal static class LevelSfxDriver
    {
        private static int _lastStateFrame = int.MinValue;

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void Level_Update__Prefix(Level __instance)
        {
            var state = ServiceCollections.ResolveStateApi();

            if (state == null || state.GetFrameDriver() != "TF.Replay")
            {
                return;
            }

            var frame = state.GetCurrentFrame();

            if (frame == _lastStateFrame)
            {
                return;
            }

            _lastStateFrame = frame;

            DynamicData.For(__instance).Set("FrameCounter", (float)frame);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Level_Update(Level __instance)
        {
            var state = ServiceCollections.ResolveStateApi();

            if (state == null || state.GetFrameDriver() != "TF.Replay")
            {
                return;
            }

            state.SynchronizeSfx((int)__instance.FrameCounter, false);
        }
    }
}
