using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    public class RollCallElementPatch : IHookable
    {
        private INetplayStateMachine _stateMachine;
        private readonly IMatchmakingService _matchmakingService;
        private readonly IArcherService _archerService;

        public RollCallElementPatch(INetplayStateMachine stateMachine, IMatchmakingService matchmakingService, IArcherService archerService)
        {
            _stateMachine = stateMachine;
            _matchmakingService = matchmakingService;
            _archerService = archerService;
        }

        public void Load()
        {
            On.TowerFall.RollcallElement.ctor += RollcallElement_ctor;
            On.TowerFall.RollcallElement.StartVersus += RollcallElement_StartVersus;
            On.TowerFall.RollcallElement.Update += RollcallElement_Update;
            On.TowerFall.RollcallElement.NotJoinedUpdate += RollcallElement_NotJoinedUpdate;
        }

        public void Unload()
        {
            On.TowerFall.RollcallElement.ctor -= RollcallElement_ctor;
            On.TowerFall.RollcallElement.StartVersus -= RollcallElement_StartVersus;
            On.TowerFall.RollcallElement.Update -= RollcallElement_Update;
            On.TowerFall.RollcallElement.NotJoinedUpdate -= RollcallElement_NotJoinedUpdate;
        }

        private int RollcallElement_NotJoinedUpdate(On.TowerFall.RollcallElement.orig_NotJoinedUpdate orig, RollcallElement self)
        {
            var dynRollcallElement = DynamicData.For(self);

            if (IsNetplay1v1())
            {
                var playerIndex = dynRollcallElement.Get<int>("playerIndex");
                var portrait = dynRollcallElement.Get<ArcherPortrait>("portrait");
                var dynArcherPortrait = DynamicData.For(portrait);
                var joined = dynArcherPortrait.Get<bool>("joined");

                if (playerIndex == 1 && _matchmakingService.HasOpponentChoosed() && !joined)
                {
                    UpdatePlayer2UI(dynRollcallElement);
                }
            }

            var res = orig(self);

            var input = dynRollcallElement.Get<TowerFall.PlayerInput>("input");

            if (IsNetplay1v1() && input != null && input.MenuBack)
            {
                self.MainMenu.State = MainMenu.MenuState.VersusOptions;
            }

            return res;
        }

        private void RollcallElement_Update(On.TowerFall.RollcallElement.orig_Update orig, RollcallElement self)
        {
            if (IsNetplay1v1())
            {
                if (_matchmakingService.HasOpponentDeclined())
                {
                    Sounds.ui_invalid.Play();
                    self.MainMenu.State = MainMenu.MenuState.VersusOptions;
                    TFGame.Instance.Commands.Open = false;
                    _matchmakingService.DisconnectFromServer();

                    return;
                }

                _stateMachine.Update();
            }

            orig(self);
        }

        private void RollcallElement_StartVersus(On.TowerFall.RollcallElement.orig_StartVersus orig, RollcallElement self)
        {
            TFGame.Instance.Commands.Clear();
            TFGame.Instance.Commands.Open = false;

            orig(self);


            if (MainMenu.VersusMatchSettings.TeamMode)
            {
                self.MainMenu.State = MainMenu.MenuState.TeamSelect;
                return;
            }

            self.MainMenu.FadeAction = MainMenu.GotoVersusLevelSelect;
            self.MainMenu.State = MainMenu.MenuState.Fade;
        }

        private void RollcallElement_ctor(On.TowerFall.RollcallElement.orig_ctor orig, TowerFall.RollcallElement self, int playerIndex)
        {
            orig(self, playerIndex);

            _archerService.Reset();

            (var stateMachine, _) = ServiceCollections.ResolveStateMachineService();

            _stateMachine = stateMachine; //making sure we have the right service (QP vs Direct)
            _stateMachine.Reset();

            if (IsNetplay1v1()) //TODO: only true 1v1
            {
                TFGame.PlayerInputs[2] = null;
                TFGame.PlayerInputs[3] = null;
            }
        }

        private bool IsNetplay1v1()
        {
            var isNetplay = MainMenu.CurrentMatchSettings != null && (MainMenu.CurrentMatchSettings.Mode.ToModel() == TF.EX.Domain.Models.Modes.Netplay1v1Direct || MainMenu.CurrentMatchSettings.Mode.ToModel() == TF.EX.Domain.Models.Modes.Netplay1v1QuickPlay);

            return MainMenu.CurrentMatchSettings != null && isNetplay;
        }

        private void UpdatePlayer2UI(DynamicData rollcallElement)
        {
            var playerIndex = rollcallElement.Get<int>("playerIndex");
            rollcallElement.Set("archerType", TFGame.AltSelect[playerIndex]);
            var portrait = rollcallElement.Get<ArcherPortrait>("portrait");
            var archerType = rollcallElement.Get<ArcherData.ArcherTypes>("archerType");

            portrait.SetCharacter(TFGame.Characters[playerIndex], archerType, 1);
            portrait.Join(unlock: false);
            TFGame.Players[1] = true;
        }
    }

}
