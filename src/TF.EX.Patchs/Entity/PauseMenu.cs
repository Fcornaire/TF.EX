using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs.Entity
{
    public class PauseMenuPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;

        public PauseMenuPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.PauseMenu.VersusMatchSettingsAndSave += PauseMenu_VersusMatchSettingsAndSave;
        }

        public void Unload()
        {
            On.TowerFall.PauseMenu.VersusMatchSettingsAndSave -= PauseMenu_VersusMatchSettingsAndSave;
        }

        private void PauseMenu_VersusMatchSettingsAndSave(On.TowerFall.PauseMenu.orig_VersusMatchSettingsAndSave orig, TowerFall.PauseMenu self)
        {
            if (_netplayManager.IsReplayMode())
            {
                Sounds.ui_clickBack.Play();
                MainMenu mainMenu = new MainMenu(MainMenu.MenuState.Main);
                TFGame.Instance.Scene = mainMenu;

                var dynPauseMenu = DynamicData.For(self);
                var level = dynPauseMenu.Get<Level>("level");

                level.Session.MatchSettings.LevelSystem.Dispose();

                return;
            }

            orig(self);

        }
    }
}
