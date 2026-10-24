using HarmonyLib;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity
{
    [HarmonyPatch(typeof(ScreenTitle))]
    internal class ScreenTitlePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, [typeof(MainMenu.MenuState)])]
        public static void ScreenTitle_ctor(ref MainMenu.MenuState state)
        {
            if (!Enum.IsDefined(typeof(MainMenu.MenuState), state))
            {
                state = MainMenu.MenuState.VersusOptions;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("ChangeState")]
        public static bool ScreenTitle_ChangeState(ScreenTitle __instance, MainMenu.MenuState state)
        {
            var currentState = state.ToDomainModel();
            if (currentState == Domain.Models.MenuState.Changelog
                || currentState == Domain.Models.MenuState.ChangelogHistory
                || currentState == Domain.Models.MenuState.Leaderboard
                || currentState == Domain.Models.MenuState.PlayerProfile)
            {
                Traverse.Create(__instance).Field("targetTexture").SetValue(null);

                return false;
            }

            if (currentState == Domain.Models.MenuState.ReplaysBrowser
                || currentState == Domain.Models.MenuState.LobbyBrowser
                || currentState == Domain.Models.MenuState.LobbyBuilder
                || currentState == Domain.Models.MenuState.NetplaySelect
                || currentState == Domain.Models.MenuState.PrivateSelect
                || currentState == Domain.Models.MenuState.PrivateJoinCode
                || currentState == Domain.Models.MenuState.QuickPlaySearch
                || currentState == Domain.Models.MenuState.RankedSelect
                || currentState == Domain.Models.MenuState.RankedSearch
                || currentState == Domain.Models.MenuState.SeriesLobby)
            {
                Traverse.Create(__instance).Field("targetTexture").SetValue(TFGame.MenuAtlas["menuTitles/fight"]);

                return false;
            }

            return true;
        }
    }
}
