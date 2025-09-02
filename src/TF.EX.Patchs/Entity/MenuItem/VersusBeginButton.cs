using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(VersusBeginButton))]
    internal class VersusBeginButtonPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnConfirm")]
        public static bool VersusBeginButton_OnConfirm(VersusBeginButton __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();
            if (!currentMode.IsNetplay())
            {
                MainMenu.CurrentMatchSettings = MainMenu.VersusMatchSettings;
                MainMenu.RollcallMode = MainMenu.RollcallModes.Versus;
                __instance.MainMenu.State = MainMenu.MenuState.Rollcall;
                return false;
            }

            if (currentMode == TF.EX.Domain.Models.Modes.Netplay)
            {
                matchmakingService.ResetLobby();
                __instance.MainMenu.State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Vector2)])]
        public static void VersusBeginButton_ctor()
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var archerService = ServiceCollections.ResolveArcherService();
            archerService.Reset();

            var lobby = matchmakingService.GetOwnLobby();
            if (!lobby.IsEmpty)
            {
                matchmakingService.LeaveLobby(matchmakingService.ResetLobby, matchmakingService.ResetLobby);
            }
        }
    }
}
