using HarmonyLib;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Engine
{
    [HarmonyPatch(typeof(TFGame))]
    internal static class TFGamePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnSceneTransition")]
        public static void TFGame_OnSceneTransition(TFGame __instance)
        {
            if (__instance.PreviousScene is Level && __instance.Scene is not Level)
            {
                if (StandaloneRecorder.ExportPending || __instance.Scene is not LevelLoaderXML)
                {
                    StandaloneRecorder.Finish();
                }

                ServiceCollections.ResolveReplayService().RestorePendingTimeStep();
            }

            if (__instance.Scene is TowerFall.MainMenu)
            {
                StandaloneRecorder.Finish();

                InputDisplayerOverlay.Reset();
                PlaybackControls.Reset();

                if (StandalonePlayback.IsActive)
                {
                    ServiceCollections.ResolveApi().StopPlayback();
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool TFGame_Update() => !StandalonePlayback.IsActive || PlaybackControls.ShouldRunUpdate();
    }
}
