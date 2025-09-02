using HarmonyLib;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(OptionsButton))]
    internal class OptionsButtonPatch
    {

        [HarmonyPrefix]
        [HarmonyPatch("OnSelect")]
        public static void OptionsButton_OnSelect(OptionsButton __instance)
        {
            var title = Traverse.Create(__instance).Field<string>("title").Value;

            switch (title)
            {
                case Constants.NETPLAY_INPUT_DELAY_TITLE:
                    TFGame.Instance.Commands.Open = true;
                    TFGame.Instance.Commands.Clear();
                    TFGame.Instance.Commands.Log("The input delay that will be applied.");
                    TFGame.Instance.Commands.Log("Lower input delay means less input lag but at the cost of more rollback/jump during netplay.");
                    TFGame.Instance.Commands.Log("Higher input delay mean less rollback/jump during netplay but more input lag.");
                    TFGame.Instance.Commands.Log("You should start low and adjust based off feeling.");
                    TFGame.Instance.Commands.Log("0 or 1 is too low.");

                    break;
                case Constants.NETPLAY_USERNAME_TITLE:
                    TFGame.Instance.Commands.Open = true;
                    TFGame.Instance.Commands.Clear();
                    TFGame.Instance.Commands.Log("The name that will be shown as an indicator during netplay");
                    TFGame.Instance.Commands.Log("Names can be from 1 character to 10 characters long");

                    break;
                default:
                    break;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnDeselect")]
        public static void OptionsButton_OnDeselect()
        {
            TFGame.Instance.Commands.Open = false;
            TFGame.Instance.Commands.Clear();
        }
    }
}
