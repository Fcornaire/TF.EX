using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
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

            if (currentMode == TF.EX.Domain.Models.Modes.Netplay)
            {
                if (TFGame.PlayerAmount >= 2)
                {
                    Sounds.ui_invalid.Play();
                    Notification.Create(TFGame.Instance.Scene, "Cannot start Netplay with more than 2 players");
                    return false;
                }

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
