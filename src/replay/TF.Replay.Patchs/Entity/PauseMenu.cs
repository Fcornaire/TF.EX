using HarmonyLib;
using System.Reflection;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Entity
{
    [HarmonyPatch]
    internal static class PauseMenuQuitToBrowser
    {
        private static readonly string[] Exits =
        [
            "Quit",
            "QuitAndSave",
            "VersusMatchSettings",
            "VersusMatchSettingsAndSave",
            "VersusArcherSelect",
            "TrialsMap",
            "TrialsMapAndSave",
            "TrialsNextLevel",
        ];

        public static IEnumerable<MethodBase> TargetMethods() => Exits.Select(name => AccessTools.DeclaredMethod(typeof(PauseMenu), name));

        public static bool Prefix()
        {
            if (ServiceCollections.ResolveReplayService()?.IsPlayback != true)
            {
                return true;
            }

            PlaybackControls.QuitToBrowser();

            return false;
        }
    }

    [HarmonyPatch]
    internal static class PauseMenuRestartReplay
    {
        public static IEnumerable<MethodBase> TargetMethods() =>
        [
            AccessTools.DeclaredMethod(typeof(PauseMenu), "VersusRematch"),
            AccessTools.DeclaredMethod(typeof(PauseMenu), "TrialsRestart"),
        ];

        public static bool Prefix()
        {
            if (ServiceCollections.ResolveReplayService()?.IsPlayback != true)
            {
                return true;
            }

            PlaybackControls.RestartReplay();

            return false;
        }
    }
}
