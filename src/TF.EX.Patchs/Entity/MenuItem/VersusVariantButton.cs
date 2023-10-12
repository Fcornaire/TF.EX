using TF.EX.Domain.Extensions;

namespace TF.EX.Patchs.Entity.MenuItem
{
    public class VersusVariantButtonPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.VersusVariantsButton.OnConfirm += VersusVariantsButton_OnConfirm;
            On.TowerFall.VersusVariantsButton.Render += VersusVariantsButton_Render;
        }

        public void Unload()
        {
            On.TowerFall.VersusVariantsButton.OnConfirm -= VersusVariantsButton_OnConfirm;
            On.TowerFall.VersusVariantsButton.Render -= VersusVariantsButton_Render;
        }

        private void VersusVariantsButton_Render(On.TowerFall.VersusVariantsButton.orig_Render orig, TowerFall.VersusVariantsButton self)
        {
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            if (currentMode != Domain.Models.Modes.Netplay)
            {
                orig(self);
            }
        }

        private void VersusVariantsButton_OnConfirm(On.TowerFall.VersusVariantsButton.orig_OnConfirm orig, TowerFall.VersusVariantsButton self)
        {
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode;

            if (!currentMode.ToModel().IsNetplay())
            {
                orig(self); //Prevent changing variant on netplay mode
            }
            else
            {
                //TODO: ux
            }
        }
    }
}
