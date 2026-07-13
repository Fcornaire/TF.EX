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
                default:
                    return MenuState.Unknown;
            }
        }

    }
}
