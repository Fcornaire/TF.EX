using HarmonyLib;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Entity
{
    [HarmonyPatch(typeof(Player))]
    internal static class PlayerHurtboxPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("DoWrapRender")]
        public static void Player_DoWrapRender(Player __instance)
        {
            if (StandalonePlayback.IsActive && PlaybackControls.ShouldShowHurtboxes && !PlaybackControls.HideOverlay)
            {
                __instance.DebugRender();
            }
        }
    }
}
