using TowerFall;

namespace TF.EX.Domain.Extensions
{
    public static class LevelExtensions
    {
        public static MainMenu GoToNetplayEntryMenu(this Level level, Models.MenuState state = Models.MenuState.NetplaySelect)
        {
            Sounds.ui_clickBack.Play();

            var menu = new MainMenu(state.ToTFModel());

            TFGame.Instance.Scene = menu;
            level.Session.MatchSettings.LevelSystem.Dispose();

            return menu;
        }
    }
}
