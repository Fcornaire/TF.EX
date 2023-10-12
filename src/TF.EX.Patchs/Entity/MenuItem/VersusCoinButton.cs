using MonoMod.Utils;
using TF.EX.Domain.Extensions;

namespace TF.EX.Patchs.Entity.MenuItem
{
    internal class VersusCoinButtonPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.VersusCoinButton.Render += VersusCoinButton_Render;
        }

        public void Unload()
        {
            On.TowerFall.VersusCoinButton.Render -= VersusCoinButton_Render;
        }

        private void VersusCoinButton_Render(On.TowerFall.VersusCoinButton.orig_Render orig, TowerFall.VersusCoinButton self)
        {
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            if (currentMode != Domain.Models.Modes.Netplay)
            {
                orig(self);
            }
        }
    }
}
