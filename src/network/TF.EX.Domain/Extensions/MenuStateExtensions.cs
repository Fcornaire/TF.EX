using TF.EX.Domain.Models;

namespace TF.EX.Domain.Extensions
{
    public static class MenuStateExtensions
    {
        private static readonly HashSet<MenuState> NetplayScreens =
        [
            MenuState.NetplaySelect,
            MenuState.QuickPlaySearch,
            MenuState.RankedSelect,
            MenuState.RankedSearch,
            MenuState.Leaderboard,
            MenuState.PlayerProfile,
            MenuState.LobbyBrowser,
            MenuState.LobbyBuilder,
            MenuState.PrivateSelect,
            MenuState.PrivateJoinCode,
            MenuState.Changelog,
            MenuState.ChangelogHistory,
            MenuState.Tutorial,
        ];

        public static TowerFall.MainMenu.MenuState ToTFModel(this MenuState menuState)
        {
            if ((int)menuState <= 15)
            {
                return (TowerFall.MainMenu.MenuState)menuState;
            }

            return menuState switch
            {
                MenuState.ReplaysBrowser => (TowerFall.MainMenu.MenuState)58,
                MenuState.LobbyBrowser => (TowerFall.MainMenu.MenuState)59,
                MenuState.LobbyBuilder => (TowerFall.MainMenu.MenuState)60,
                MenuState.SeriesLobby => (TowerFall.MainMenu.MenuState)61,
                MenuState.NetplaySelect => (TowerFall.MainMenu.MenuState)62,
                MenuState.PrivateSelect => (TowerFall.MainMenu.MenuState)63,
                MenuState.PrivateJoinCode => (TowerFall.MainMenu.MenuState)64,
                MenuState.QuickPlaySearch => (TowerFall.MainMenu.MenuState)65,
                MenuState.Changelog => (TowerFall.MainMenu.MenuState)66,
                MenuState.ChangelogHistory => (TowerFall.MainMenu.MenuState)67,
                MenuState.RankedSelect => (TowerFall.MainMenu.MenuState)68,
                MenuState.Leaderboard => (TowerFall.MainMenu.MenuState)69,
                MenuState.RankedSearch => (TowerFall.MainMenu.MenuState)70,
                MenuState.PlayerProfile => (TowerFall.MainMenu.MenuState)71,
                MenuState.Tutorial => (TowerFall.MainMenu.MenuState)72,
                _ => throw new System.NotImplementedException("MenuState not found"),
            };
        }

        public static MenuState ToDomainModel(this TowerFall.MainMenu.MenuState menuState)
        {
            if ((int)menuState <= 15)
            {
                return (MenuState)menuState;
            }

            return menuState switch
            {
                (TowerFall.MainMenu.MenuState)58 => MenuState.ReplaysBrowser,
                (TowerFall.MainMenu.MenuState)59 => MenuState.LobbyBrowser,
                (TowerFall.MainMenu.MenuState)60 => MenuState.LobbyBuilder,
                (TowerFall.MainMenu.MenuState)61 => MenuState.SeriesLobby,
                (TowerFall.MainMenu.MenuState)62 => MenuState.NetplaySelect,
                (TowerFall.MainMenu.MenuState)63 => MenuState.PrivateSelect,
                (TowerFall.MainMenu.MenuState)64 => MenuState.PrivateJoinCode,
                (TowerFall.MainMenu.MenuState)65 => MenuState.QuickPlaySearch,
                (TowerFall.MainMenu.MenuState)66 => MenuState.Changelog,
                (TowerFall.MainMenu.MenuState)67 => MenuState.ChangelogHistory,
                (TowerFall.MainMenu.MenuState)68 => MenuState.RankedSelect,
                (TowerFall.MainMenu.MenuState)69 => MenuState.Leaderboard,
                (TowerFall.MainMenu.MenuState)70 => MenuState.RankedSearch,
                (TowerFall.MainMenu.MenuState)71 => MenuState.PlayerProfile,
                (TowerFall.MainMenu.MenuState)72 => MenuState.Tutorial,
                _ => MenuState.Unknown,
            };
        }

        public static bool IsNetplayScreen(this MenuState menuState)
        {
            return NetplayScreens.Contains(menuState);
        }
    }
}
