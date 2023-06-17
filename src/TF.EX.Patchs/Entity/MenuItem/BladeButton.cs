using MonoMod.Utils;
using TF.EX.Domain.Extensions;

namespace TF.EX.Patchs.Entity.MenuItem
{
    public class BladeButtonPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.BladeButton.OnSelect += BladeButton_OnSelect;
        }

        public void Unload()
        {
            On.TowerFall.BladeButton.OnSelect -= BladeButton_OnSelect;
        }

        private void BladeButton_OnSelect(On.TowerFall.BladeButton.orig_OnSelect orig, TowerFall.BladeButton self)
        {
            var dynBladeButton = DynamicData.For(self);
            var CreatedState = dynBladeButton.Get<TowerFall.MainMenu.MenuState>("CreatedState");

            if (CreatedState == Domain.Models.MenuState.ReplaysBrowser.ToTFModel())
            {
                self.MainMenu.TweenUICameraToY(Math.Max(0f, self.Y - 120f));
                self.Depth--;
            }

            orig(self);
        }
    }

}
