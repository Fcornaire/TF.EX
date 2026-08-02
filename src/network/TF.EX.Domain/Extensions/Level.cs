using TF.EX.Domain.Context;
using TowerFall;

namespace TF.EX.Domain.Extensions
{
    public static class LevelExtensions
    {
        public static void GoToNetplayEntryMenu(this Level level)
        {
            Sounds.ui_clickBack.Play();

            var state = MenuReturn.NetplayEntry ?? MainMenu.MenuState.VersusOptions;

            TFGame.Instance.Scene = new MainMenu(state);
            level.Session.MatchSettings.LevelSystem.Dispose();
        }
    }
}
