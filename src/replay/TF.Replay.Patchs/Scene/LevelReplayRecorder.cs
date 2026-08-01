using System.Xml;
using HarmonyLib;
using MonoMod.Utils;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    [HarmonyPatch(typeof(Level))]
    internal static class LevelReplayRecorderPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch([typeof(Session), typeof(XmlElement)])]
        public static void Level_ctor(Level __instance)
        {
            if (__instance.ReplayRecorder != null && StandalonePlayback.IsActive)
            {
                DynamicData.For(__instance).Set("ReplayRecorder", null);
            }
        }
    }

    [HarmonyPatch(typeof(Session))]
    internal static class SessionEndRoundPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("EndRound")]
        public static void Session_EndRound(Session __instance)
        {
            if (!StandalonePlayback.IsActive)
            {
                return;
            }

            var level = __instance.CurrentLevel;

            if (level == null || level.ReplayRecorder != null)
            {
                return;
            }

            level.Frozen = true;
            level.OrbLogic?.CancelSlowMo();
        }
    }
}
