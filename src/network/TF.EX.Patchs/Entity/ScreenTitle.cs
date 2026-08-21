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
            if (currentState == Domain.Models.MenuState.ReplaysBrowser
                || currentState == Domain.Models.MenuState.LobbyBrowser
                || currentState == Domain.Models.MenuState.LobbyBuilder
                || currentState == Domain.Models.MenuState.VersusSelect)
            {
                Traverse.Create(__instance).Field("targetTexture").SetValue(TFGame.MenuAtlas["menuTitles/fight"]);

                return false;
            }

            return true;
        }
    }
}
