namespace TF.Replay.Domain.Models
{
    public static class ReplayMenuState
    {
        public const int ReplaysBrowser = 58;

        public static TowerFall.MainMenu.MenuState ToTFModel() => (TowerFall.MainMenu.MenuState)ReplaysBrowser;
    }
}
