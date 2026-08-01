using HarmonyLib;
using TF.Replay.Domain;
using TF.Replay.Domain.Extensions;
using TowerFall;

namespace TF.Replay.Patchs
{
    [HarmonyPatch(typeof(TrialsControl))]
    internal static class TrialsHudKeep
    {
        [HarmonyPrefix]
        [HarmonyPatch("TweenOut")]
        public static bool TweenOut() => ServiceCollections.ResolveReplayService()?.IsPlayback != true;
    }

    [HarmonyPatch(typeof(TrialsResults))]
    internal static class TrialsResultsSkip
    {
        [HarmonyPrefix]
        [HarmonyPatch("Added")]
        public static bool Added(TrialsResults __instance)
        {
            if (ServiceCollections.ResolveReplayService()?.IsPlayback != true)
            {
                return true;
            }

            __instance.Visible = false;
            __instance.Active = false;
            __instance.RemoveSelf();

            if (__instance.Scene is Level level)
            {
                level.Get<HUDFade>()?.RemoveSelf();

                level.Frozen = false;
            }

            return false;
        }
    }
}
