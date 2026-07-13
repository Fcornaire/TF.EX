using HarmonyLib;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(VersusCoinButton))]
    internal class VersusCoinButtonPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Render")]
        public static bool VersusCoinButton_Render()
        {
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            if (currentMode != Domain.Models.Modes.Netplay)
            {
                return true;
            }

            return false;
        }
    }
}
