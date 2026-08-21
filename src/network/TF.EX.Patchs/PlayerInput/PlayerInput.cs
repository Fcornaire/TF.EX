using HarmonyLib;
using System.Linq;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.PlayerInput
{
    [HarmonyPatch(typeof(TowerFall.PlayerInput))]
    internal class PlayerInputPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("AssignInputs")]
        public static bool PlayerInput_AssignInputs()
        {
            if (TowerFall.MainMenu.VersusMatchSettings?.Mode.IsNetplay() != true || ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty)
            {
                return true;
            }

            if (TFGame.Instance?.Scene is Level level
                && level.Session.GetWinner() != -1
                && !ServiceCollections.ResolveNetplayManager().IsInit())
            {
                return true;
            }

            return TFGame.PlayerInputs.All(input => input == null);
        }

        public static bool TryGetNetplayName(TowerFall.PlayerInput self, out string name)
        {
            name = null;

            if (TFGame.Instance?.Scene is not MainMenu mainMenu
                || mainMenu.State != MainMenu.MenuState.Rollcall
                || TowerFall.MainMenu.VersusMatchSettings == null
                || !TowerFall.MainMenu.VersusMatchSettings.Mode.IsNetplay())
            {
                return false;
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var lobby = matchmakingService.GetOwnLobby();

            if (lobby.IsEmpty)
            {
                return false;
            }

            var seat = ServiceCollections.ResolveInputService().GetInputIndex(self);

            if (seat < 0 || seat >= lobby.Players.Count)
            {
                return false;
            }

            name = ServiceCollections.ResolveNetplayManager().GetNameForSeat(seat);

            return !string.IsNullOrEmpty(name);
        }
    }
}
