using MonoMod.Utils;
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
            orig(self);

            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode;

            if (currentMode.ToModel().IsNetplay() && TowerFall.MainMenu.VersusMatchSettings.MatchLength != TowerFall.MatchSettings.MatchLengths.Standard)
            {
                TowerFall.MainMenu.VersusMatchSettings.MatchLength = TowerFall.MatchSettings.MatchLengths.Standard; //Prevent changing on netplay mode
                var dynVersusCoinButton = DynamicData.For(self);
                dynVersusCoinButton.Invoke("UpdateSides");
                TowerFall.Sounds.ui_invalid.Play();
            }
        }
    }
}
