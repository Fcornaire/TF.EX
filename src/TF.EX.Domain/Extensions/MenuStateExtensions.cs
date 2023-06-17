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
                    return (TowerFall.MainMenu.MenuState)16;
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
                case (TowerFall.MainMenu.MenuState)16:
                    return MenuState.ReplaysBrowser;
                default:
                    throw new System.NotImplementedException("MenuState not found");
            }
        }

    }
}
