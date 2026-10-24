using TF.EX.Domain.Models;

namespace TF.EX.Domain.Extensions
{
    public static class MenuStateExtensions
    {
        public static TowerFall.MainMenu.MenuState ToTFModel(this MenuState menuState)
        {
            if ((int)menuState <= 15)
            {
                return (TowerFall.MainMenu.MenuState)menuState;
            }

            switch (menuState)
            {
                case MenuState.ReplaysBrowser:
                    return (TowerFall.MainMenu.MenuState)58;
                case MenuState.LobbyBrowser:
                    return (TowerFall.MainMenu.MenuState)59;
                case MenuState.LobbyBuilder:
                    return (TowerFall.MainMenu.MenuState)60;
                case MenuState.SeriesLobby:
                    return (TowerFall.MainMenu.MenuState)61;
                case MenuState.NetplaySelect:
                    return (TowerFall.MainMenu.MenuState)62;
                case MenuState.PrivateSelect:
                    return (TowerFall.MainMenu.MenuState)63;
                case MenuState.PrivateJoinCode:
                    return (TowerFall.MainMenu.MenuState)64;
                case MenuState.QuickPlaySearch:
                    return (TowerFall.MainMenu.MenuState)65;
                case MenuState.Changelog:
                    return (TowerFall.MainMenu.MenuState)66;
                case MenuState.ChangelogHistory:
                    return (TowerFall.MainMenu.MenuState)67;
                case MenuState.RankedSelect:
                    return (TowerFall.MainMenu.MenuState)68;
                case MenuState.Leaderboard:
                    return (TowerFall.MainMenu.MenuState)69;
                case MenuState.RankedSearch:
                    return (TowerFall.MainMenu.MenuState)70;
                case MenuState.PlayerProfile:
                    return (TowerFall.MainMenu.MenuState)71;
                default:
                    throw new System.NotImplementedException("MenuState not found");
            }
        }

        public static MenuState ToDomainModel(this TowerFall.MainMenu.MenuState menuState)
        {
            if ((int)menuState <= 15)
            {
                return (MenuState)menuState;
            }

            switch (menuState)
            {
                case (TowerFall.MainMenu.MenuState)58:
                    return MenuState.ReplaysBrowser;
                case (TowerFall.MainMenu.MenuState)59:
                    return MenuState.LobbyBrowser;
                case (TowerFall.MainMenu.MenuState)60:
                    return MenuState.LobbyBuilder;
                case (TowerFall.MainMenu.MenuState)61:
                    return MenuState.SeriesLobby;
                case (TowerFall.MainMenu.MenuState)62:
                    return MenuState.NetplaySelect;
                case (TowerFall.MainMenu.MenuState)63:
                    return MenuState.PrivateSelect;
                case (TowerFall.MainMenu.MenuState)64:
                    return MenuState.PrivateJoinCode;
                case (TowerFall.MainMenu.MenuState)65:
                    return MenuState.QuickPlaySearch;
                case (TowerFall.MainMenu.MenuState)66:
                    return MenuState.Changelog;
                case (TowerFall.MainMenu.MenuState)67:
                    return MenuState.ChangelogHistory;
                case (TowerFall.MainMenu.MenuState)68:
                    return MenuState.RankedSelect;
                case (TowerFall.MainMenu.MenuState)69:
                    return MenuState.Leaderboard;
                case (TowerFall.MainMenu.MenuState)70:
                    return MenuState.RankedSearch;
                case (TowerFall.MainMenu.MenuState)71:
                    return MenuState.PlayerProfile;
                default:
                    return MenuState.Unknown;
            }
        }

    }
}
