using HarmonyLib;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity
{
    [HarmonyPatch(typeof(ScreenTitle))]
    internal class ScreenTitlePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ChangeState")]
        public static bool ScreenTitle_ChangeState(ScreenTitle __instance, MainMenu.MenuState state)
        {
            var currentState = state.ToDomainModel();
            if (currentState == Domain.Models.MenuState.ReplaysBrowser
                || currentState == Domain.Models.MenuState.LobbyBrowser
                || currentState == Domain.Models.MenuState.LobbyBuilder)
            {
                Traverse.Create(__instance).Field("targetTexture").SetValue(TFGame.MenuAtlas["menuTitles/fight"]);

                return false;
            }

            return true;
        }
    }
}
