using MonoMod.Utils;
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
                case "NETPLAY INPUT DELAY":
                    TFGame.Instance.Commands.Open = true;
                    TFGame.Instance.Commands.Clear();
                    TFGame.Instance.Commands.Log("The input delay that will be applied.");
                    TFGame.Instance.Commands.Log("Low input delay mean the gameplay will feel more close to a local session but at the cost of more rollback/jump during netplay.");
                    TFGame.Instance.Commands.Log("Hight input delay mean less rollback/jump during netplay but more sluggish control.");
                    TFGame.Instance.Commands.Log("You should start low and adjust based off feeling.");
                    TFGame.Instance.Commands.Log("Don't try 0 (even 1) because it's too low anyway.");

                    break;
                case "NETPLAY NAME":
                    TFGame.Instance.Commands.Open = true;
                    TFGame.Instance.Commands.Clear();
                    TFGame.Instance.Commands.Log("The name that will be showed as an indicator during netplay");
                    TFGame.Instance.Commands.Log("The maximum length allowed is 10 characters and the minimum 1");

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
