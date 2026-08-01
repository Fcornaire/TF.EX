using TowerFall;

namespace TF.EX.Domain.Extensions
{
    public static class LevelExtensions
    {
        public static void GoToVersusOptions(this Level level)
        {
            Sounds.ui_clickBack.Play();
            MainMenu mainMenu = new MainMenu(MainMenu.MenuState.VersusOptions);
            TFGame.Instance.Scene = mainMenu;
            level.Session.MatchSettings.LevelSystem.Dispose();
        }
    }
}
