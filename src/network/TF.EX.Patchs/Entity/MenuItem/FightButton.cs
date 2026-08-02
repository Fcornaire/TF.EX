using HarmonyLib;
using TF.EX.Domain.Extensions;
using TF.EX.Patchs.Scene;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(FightButton))]
    public class FightButtonPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch("MenuAction")]
        public static void FightButton_MenuAction(FightButton __instance)
        {
            if (WiderSetMenu.SelectionState.HasValue)
            {
                return;
            }

            __instance.MainMenu.State = TF.EX.Domain.Models.MenuState.VersusSelect.ToTFModel();
        }
    }
}
