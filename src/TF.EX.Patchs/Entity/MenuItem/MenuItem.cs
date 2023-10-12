using TF.EX.Domain.Extensions;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    internal class MenuItemPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.MenuItem.Update += MenuItem_Update;
        }

        public void Unload()
        {
            On.TowerFall.MenuItem.Update -= MenuItem_Update;
        }

        private void MenuItem_Update(On.TowerFall.MenuItem.orig_Update orig, TowerFall.MenuItem self)
        {
            orig(self);

            if (TowerFall.MainMenu.VersusMatchSettings != null)
            {
                var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();
                var state = self.MainMenu.State.ToDomainModel();

                if (self is VersusModeButton
                    && MenuInput.Down
                    && currentMode == Domain.Models.Modes.Netplay
                    && state == Domain.Models.MenuState.VersusOptions)
                {
                    self.Selected = false;
                    self.DownItem.Selected = false;
                    self.MainMenu.Get<VersusBeginButton>().Selected = true;
                }

                if (self is VersusBeginButton
                    && MenuInput.Up
                    && currentMode == Domain.Models.Modes.Netplay
                    && state == Domain.Models.MenuState.VersusOptions)
                {
                    self.Selected = false;
                    self.UpItem.Selected = false;
                    self.MainMenu.Get<VersusModeButton>().Selected = true;
                }
            }
        }
    }
}
