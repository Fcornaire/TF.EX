using TF.EX.Domain.Context;
using TowerFall;

namespace TF.EX.Domain.Extensions
{
    public static class LevelExtensions
    {
        public static MainMenu GoToNetplayEntryMenu(this Level level)
        {
            Sounds.ui_clickBack.Play();

            var state = MenuReturn.NetplayEntry ?? MainMenu.MenuState.VersusOptions;

            var menu = new MainMenu(state);

            TFGame.Instance.Scene = menu;
            level.Session.MatchSettings.LevelSystem.Dispose();

            return menu;
        }
    }
}
