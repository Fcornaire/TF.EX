using HarmonyLib;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.PlayerInput
{
    //Only allow back input for spectator in Rollcall
    internal static class SpectatorRollcallGate
    {
        private static bool _leaving;

        public static bool IsInert()
        {
            if (TFGame.Instance.Scene is not MainMenu mainMenu || TowerFall.MainMenu.VersusMatchSettings?.Mode.IsNetplay() != true)
            {
                _leaving = false;
                return false;
            }

            var state = Traverse.Create(mainMenu).Field("state").GetValue<MainMenu.MenuState>();
            if (state != MainMenu.MenuState.Rollcall)
            {
                _leaving = false;
                return false;
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            return !matchmakingService.GetOwnLobby().IsEmpty
                && matchmakingService.IsSpectator()
                && !matchmakingService.IsLobbyReady();
        }

        public static bool HandleBack(bool actualInput)
        {
            if (!actualInput || _leaving)
            {
                return false;
            }

            _leaving = true;
            Sounds.ui_clickBack.Play();

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            Task.Run(async () =>
            {
                await matchmakingService.LeaveLobby(() => { }, () => { });
            });

            (TFGame.Instance.Scene as MainMenu).State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
            matchmakingService.ResetPeer();

            return false;
        }
    }
}
