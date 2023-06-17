using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    public class FightButtonPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.FightButton.MenuAction += MenuAction_Patch;
        }

        public void Unload()
        {
            On.TowerFall.FightButton.MenuAction -= MenuAction_Patch;
        }

        private static void MenuAction_Patch(On.TowerFall.FightButton.orig_MenuAction orig, TowerFall.FightButton self)
        {
            (Monocle.Engine.Instance.Scene as MainMenu).State = MainMenu.MenuState.VersusOptions;
        }
    }

}
