using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity
{
    internal class ScreenTitlePatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.ScreenTitle.ChangeState += ScreenTitle_ChangeState;
        }

        public void Unload()
        {
            On.TowerFall.ScreenTitle.ChangeState -= ScreenTitle_ChangeState;
        }

        private void ScreenTitle_ChangeState(On.TowerFall.ScreenTitle.orig_ChangeState orig, TowerFall.ScreenTitle self, TowerFall.MainMenu.MenuState state)
        {
            var currentState = state.ToDomainModel();
            if (currentState == Domain.Models.MenuState.ReplaysBrowser
                || currentState == Domain.Models.MenuState.LobbyBrowser
                || currentState == Domain.Models.MenuState.LobbyBuilder)
            {
                var dynScreenTitle = DynamicData.For(self);
                dynScreenTitle.Set("targetTexture", TFGame.MenuAtlas["menuTitles/fight"]);
            }
            else
            {
                orig(self, state);
            }
        }
    }
}
