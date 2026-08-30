using HarmonyLib;
using TF.State.Domain.Context;

namespace TF.State.Patchs
{
    //This is to prevent particle to emit 4X than vanilla at 240hz 
    [HarmonyPatch(typeof(Monocle.Scene))]
    internal class SceneIntervalPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnInterval", [typeof(int)])]
        public static bool Scene_OnInterval(Monocle.Scene __instance, int interval, ref bool __result)
        {
            var scale = DrivenTickScale();

            if (scale <= 1)
            {
                return true;
            }

            __result = (int)__instance.FrameCounter % (interval * scale) == 0;

            return false;
        }

        private static int DrivenTickScale()
        {
            if (!StateFlags.IsCaptureActive && !StateFlags.IsReplayMode && StateFlags.FrameDriverOwner == null)
            {
                return 1;
            }

            var game = Monocle.Engine.Instance;

            if (game == null || !game.IsFixedTimeStep)
            {
                return 1;
            }

            return (int)Math.Round(1.0 / game.TargetElapsedTime.TotalSeconds / 60.0);
        }
    }
}
