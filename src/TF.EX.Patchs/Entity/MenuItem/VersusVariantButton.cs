using HarmonyLib;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(VersusVariantsButton))]
    public class VersusVariantButtonPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Render")]
        public static bool VersusVariantsButton_Render()
        {
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            if (currentMode != Domain.Models.Modes.Netplay)
            {
                return true;
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnConfirm")]
        public static bool VersusVariantsButton_OnConfirm()
        {
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode;

            if (!currentMode.ToModel().IsNetplay())
            {
                return true; //Prevent changing variant on netplay mode
            }

            //TODO: ux
            return false;
        }
    }
}
