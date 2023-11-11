using Microsoft.Xna.Framework;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    internal class VersusBeginButtonPatch : IHookable
    {
        private readonly IMatchmakingService _matchmakingService;
        private readonly IArcherService _archerService;
        public VersusBeginButtonPatch(IMatchmakingService matchmakingService, IArcherService archerService)
        {
            _matchmakingService = matchmakingService;
            _archerService = archerService;
        }

        public void Load()
        {
            On.TowerFall.VersusBeginButton.ctor += VersusBeginButton_ctor;
            On.TowerFall.VersusBeginButton.OnConfirm += VersusBeginButton_OnConfirm;
        }

        public void Unload()
        {
            On.TowerFall.VersusBeginButton.ctor -= VersusBeginButton_ctor;
            On.TowerFall.VersusBeginButton.OnConfirm -= VersusBeginButton_OnConfirm;
        }

        private void VersusBeginButton_OnConfirm(On.TowerFall.VersusBeginButton.orig_OnConfirm orig, VersusBeginButton self)
        {
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();
            if (!currentMode.IsNetplay())
            {
                MainMenu.CurrentMatchSettings = MainMenu.VersusMatchSettings;
                MainMenu.RollcallMode = MainMenu.RollcallModes.Versus;
                self.MainMenu.State = MainMenu.MenuState.Rollcall;
                return;
            }

            if (currentMode == TF.EX.Domain.Models.Modes.Netplay)
            {
                _matchmakingService.ResetLobby();
                self.MainMenu.State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                return;
            }

            orig(self);
        }

        private void VersusBeginButton_ctor(On.TowerFall.VersusBeginButton.orig_ctor orig, TowerFall.VersusBeginButton self, Vector2 position, Vector2 tweenFrom)
        {
            orig(self, position, tweenFrom);

            _archerService.Reset();
        }
    }
}
