using HarmonyLib;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(ReadyBanner))]
    internal class ReadyBannerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetReadyState")]
        public static void ReadyBanner_GetReadyState(ref bool __result)
        {
            if (!__result || TowerFall.MainMenu.VersusMatchSettings == null)
            {
                return;
            }

            if (!TowerFall.MainMenu.VersusMatchSettings.Mode.IsNetplay())
            {
                return;
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (matchmakingService.GetOwnLobby().IsEmpty)
            {
                return;
            }

            __result = matchmakingService.IsLobbyReady() || matchmakingService.CanHostStart();
        }
    }
}
