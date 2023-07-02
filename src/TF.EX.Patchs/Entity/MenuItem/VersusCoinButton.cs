using TF.EX.Domain.Extensions;

namespace TF.EX.Patchs.Entity.MenuItem
{
    internal class VersusCoinButtonPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.VersusCoinButton.Update += VersusCoinButton_Update;
        }

        public void Unload()
        {
            On.TowerFall.VersusCoinButton.Update -= VersusCoinButton_Update;
        }

        private void VersusCoinButton_Update(On.TowerFall.VersusCoinButton.orig_Update orig, TowerFall.VersusCoinButton self)
        {
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode;

            if (!currentMode.ToModel().IsNetplay())
            {
                orig(self); //Prevent changing on netplay mode
            }
            else
            {
                //TODO: ux
            }

        }
    }
}
