using MonoMod.Utils;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    internal class OptionsButtonPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.OptionsButton.OnSelect += OptionsButton_OnSelect;
            On.TowerFall.OptionsButton.OnDeselect += OptionsButton_OnDeselect;
        }

        public void Unload()
        {
            On.TowerFall.OptionsButton.OnSelect -= OptionsButton_OnSelect;
            On.TowerFall.OptionsButton.OnDeselect -= OptionsButton_OnDeselect;
        }

        private void OptionsButton_OnSelect(On.TowerFall.OptionsButton.orig_OnSelect orig, TowerFall.OptionsButton self)
        {
            var dynOptionsButton = DynamicData.For(self);
            var title = dynOptionsButton.Get<string>("title");

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

            orig(self);
        }

        private void OptionsButton_OnDeselect(On.TowerFall.OptionsButton.orig_OnDeselect orig, TowerFall.OptionsButton self)
        {
            TFGame.Instance.Commands.Open = false;
            TFGame.Instance.Commands.Clear();

            orig(self);
        }
    }
}
