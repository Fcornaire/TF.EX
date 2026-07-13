using HarmonyLib;
using TF.EX.Domain.Extensions;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(TowerFall.MenuItem))]
    internal class MenuItemPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(TowerFall.MenuItem.Update))]
        public static void MenuItem_Update(TowerFall.MenuItem __instance)
        {
            if (MainMenu.VersusMatchSettings != null)
            {
                var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();
                var state = __instance.MainMenu.State.ToDomainModel();

                if (__instance is VersusModeButton
                    && MenuInput.Down
                    && currentMode == Domain.Models.Modes.Netplay
                    && state == Domain.Models.MenuState.VersusOptions)
                {
                    __instance.Selected = false;
                    __instance.DownItem.Selected = false;
                    __instance.MainMenu.Get<VersusBeginButton>().Selected = true;
                }

                if (__instance is VersusBeginButton
                    && MenuInput.Up
                    && currentMode == Domain.Models.Modes.Netplay
                    && state == Domain.Models.MenuState.VersusOptions)
                {
                    __instance.Selected = false;
                    __instance.UpItem.Selected = false;
                    __instance.MainMenu.Get<VersusModeButton>().Selected = true;
                }
            }
        }
    }
}
