using TowerFall;

namespace TF.EX.Domain.Extensions
{
    public static class LevelExtensions
    {
        public static MainMenu GoToNetplayEntryMenu(this Level level)
        {
            Sounds.ui_clickBack.Play();

            var menu = new MainMenu(Models.MenuState.NetplaySelect.ToTFModel());

            TFGame.Instance.Scene = menu;
            level.Session.MatchSettings.LevelSystem.Dispose();

            return menu;
        }
    }
}
