using HarmonyLib;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    [HarmonyPatch(typeof(Level))]
    internal static class LevelRecordDriver
    {
        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Level_Update(Level __instance) => StandaloneRecorder.Step(__instance);
    }
}
