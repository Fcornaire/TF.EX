using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;
using TF.EX.TowerFallExtensions;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    internal class VersusBeginButtonPatch : IHookable
    {

        private readonly IMatchmakingService _matchmakingService;
        private readonly INetplayManager _netplayManager;
        private readonly IArcherService _archerService;

        private bool _isRegistering = false;
        private bool _isWaitingForPlayerChoice = false;
        private bool _hasPlayedStartGameType = false;
        private TF.EX.Domain.Models.Modes _currentMode;
        private bool _isWaitingForOpponent = false;
        private Dialog _dialog = null;
        private bool _hasError = false;

        public VersusBeginButtonPatch(IMatchmakingService matchmakingService, INetplayManager netplayManager, IArcherService archerService)
        {
            _matchmakingService = matchmakingService;
            _netplayManager = netplayManager;
            _archerService = archerService;
        }

        public void Load()
        {
            On.TowerFall.VersusBeginButton.ctor += VersusBeginButton_ctor;
            On.TowerFall.VersusBeginButton.Update += VersusBeginButton_Update;
            On.TowerFall.VersusBeginButton.OnConfirm += VersusBeginButton_OnConfirm;
        }

        public void Unload()
        {
            On.TowerFall.VersusBeginButton.ctor -= VersusBeginButton_ctor;
            On.TowerFall.VersusBeginButton.Update -= VersusBeginButton_Update;
            On.TowerFall.VersusBeginButton.OnConfirm -= VersusBeginButton_OnConfirm;
        }

        private void VersusBeginButton_OnConfirm(On.TowerFall.VersusBeginButton.orig_OnConfirm orig, VersusBeginButton self)
        {
            if (!_currentMode.IsNetplay())
            {
                //orig(self);

                MainMenu.CurrentMatchSettings = MainMenu.VersusMatchSettings;
                MainMenu.RollcallMode = MainMenu.RollcallModes.Versus;
                self.MainMenu.State = MainMenu.MenuState.Rollcall;
                return;
            }


            if (_currentMode == TF.EX.Domain.Models.Modes.Netplay1v1Direct && !_isRegistering && !_hasError)
            {
                _isRegistering = true;
                _matchmakingService.RegisterForDirect();
            }

            if (_currentMode == TF.EX.Domain.Models.Modes.Netplay1v1QuickPlay && !_isRegistering && !_hasError)
            {
                _isRegistering = true;
                _matchmakingService.RegisterForQuickPlay();
            }
        }

        private void VersusBeginButton_Update(On.TowerFall.VersusBeginButton.orig_Update orig, VersusBeginButton self)
        {
            _currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();

            orig(self);

            if (_currentMode.IsNetplay())
            {
                HandleNetplay(self);
            }
        }

        private void VersusBeginButton_ctor(On.TowerFall.VersusBeginButton.orig_ctor orig, TowerFall.VersusBeginButton self, Vector2 position, Vector2 tweenFrom)
        {
            orig(self, position, tweenFrom);

            _archerService.Reset();

            var hasConnected = _matchmakingService.ConnectToServerAndListen();
            if (!hasConnected)
            {
                _dialog = new Dialog("Error", "SERVER CONNEXION FAILED", new Vector2(160f, 120f), CancelDialog, new Dictionary<string, System.Action>(), null, true);
                Add(_dialog);
                _hasError = true;
            }
        }

        private void CancelDialog()
        {
            (TFGame.Instance.Scene as MainMenu).GetMainLayer().Remove((ent) => ent is Dialog);

            _hasError = false;
        }

        private void CancelQuickPlay()
        {
            _matchmakingService.CancelQuickPlay();
            CancelDialog();
        }

        private void CancelSearch()
        {
            Sounds.ui_clickBack.Play();
            _matchmakingService.DisconnectFromServer();
            CancelDialog();
            _isRegistering = false;
            _isWaitingForOpponent = false;
        }

        private void Add(Monocle.Entity entity)
        {
            var dynLayer = DynamicData.For((TFGame.Instance.Scene as MainMenu).GetMainLayer());
            dynLayer.Invoke("Add", entity, false);
        }

        private void AcceptOpponentInQuickPlay()
        {
            _matchmakingService.AcceptOpponentInQuickPlay();
        }

        private void HandleNetplay(VersusBeginButton self)
        {
            if (_currentMode == TF.EX.Domain.Models.Modes.Netplay1v1Direct)
            {
                HandleNetplayDirect(self);
            }

            if (_currentMode == TF.EX.Domain.Models.Modes.Netplay1v1QuickPlay)
            {
                HandleNetplayQuickPlay(self);
            }
        }

        private void HandleNetplayDirect(VersusBeginButton self)
        {
            if (_isRegistering && !string.IsNullOrEmpty(_matchmakingService.GetDirectCode()))
            {
                if (!_hasPlayedStartGameType)
                {
                    _hasPlayedStartGameType = true;
                    Sounds.ui_startGameType.Play();
                }

                MainMenu.CurrentMatchSettings = MainMenu.VersusMatchSettings;
                MainMenu.RollcallMode = MainMenu.RollcallModes.Versus;
                self.MainMenu.State = MainMenu.MenuState.Rollcall;
            }
        }

        private void HandleNetplayQuickPlay(VersusBeginButton self)
        {
            if (_isRegistering && _matchmakingService.HasRegisteredForQuickPlay() && !_isWaitingForOpponent)
            {
                Sounds.ui_startGameType.Play();

                _isWaitingForOpponent = true;
                _dialog = new Dialog("Quick Play", "Searching an opponent...", new Vector2(160f, 120f), CancelSearch, new Dictionary<string, System.Action>());
                Add(_dialog);
            }

            if (_isRegistering && _matchmakingService.HasFoundOpponentForQuickPlay() && !_isWaitingForPlayerChoice)
            {
                _isWaitingForPlayerChoice = true;
                Sounds.ui_startGameType.Play();

                CancelDialog();
                var actions = new Dictionary<string, System.Action>
                {
                    { "Yes", AcceptOpponentInQuickPlay },
                    { "No", CancelQuickPlay }
                };

                var centerAction = new Func<string>(() =>
                {
                    var text = $"{_matchmakingService.GetPingToOpponent()} MS";
                    return text;
                });

                _dialog = new Dialog("Quick Play", $"Play with {_netplayManager.GetPlayer2Name()} ?", new Vector2(160f, 120f), CancelDialog, actions, centerAction);
                Add(_dialog);
            }

            if (_isRegistering && !_matchmakingService.HasFoundOpponentForQuickPlay() && _isWaitingForPlayerChoice)
            {
                _isWaitingForPlayerChoice = false;
                _isWaitingForOpponent = false;
                Sounds.ui_invalid.Play();

                CancelDialog();
            }

            if (_isRegistering && _matchmakingService.HasAcceptedOpponentForQuickPlay())
            {
                CancelDialog();
                _isRegistering = false;
                _isWaitingForPlayerChoice = false;
                _hasPlayedStartGameType = false;
                _isWaitingForOpponent = false;

                if (!_hasPlayedStartGameType) //TODO: Refacto
                {
                    _hasPlayedStartGameType = true;
                    Sounds.ui_startGameType.Play();
                }

                MainMenu.CurrentMatchSettings = MainMenu.VersusMatchSettings;
                MainMenu.RollcallMode = MainMenu.RollcallModes.Versus;
                self.MainMenu.State = MainMenu.MenuState.Rollcall;
            }
        }
    }
}
