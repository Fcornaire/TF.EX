using HarmonyLib;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    [HarmonyPatch(typeof(Session))]
    internal static class SessionRecordStart
    {
        [HarmonyPrefix]
        [HarmonyPatch("StartGame")]
        public static void Session_StartGame(Session __instance) => StandaloneRecorder.TryBegin(__instance);
    }
}
