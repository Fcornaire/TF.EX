using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Patchs.PlayerInput
{
    public class KeyboardInputPatch : IHookable
    {
        private IInputService _inputService;
        private INetplayManager _netplayManager;
        private INetplayStateMachine _stateMachine;
        private TF.EX.Domain.Models.Modes _currerntMode;

        private delegate bool MenuConfirm_orig(KeyboardInput self);
        private delegate bool MenuSkipReplay_orig(KeyboardInput self);
        private delegate bool MenuSaveReplay_orig(KeyboardInput self);
        private delegate bool MenuSaveReplayCheck_orig(KeyboardInput self);
        private static IDetour MenuConfirm_hook;
        private static IDetour MenuSkipReplay_hook;
        private static IDetour MenuSaveReplay_hook;
        private static IDetour MenuSaveReplayCheck_hook;


        public KeyboardInputPatch(IInputService inputService, INetplayManager netplayManager, INetplayStateMachine stateMachine)
        {
            _inputService = inputService;
            _netplayManager = netplayManager;
            _stateMachine = stateMachine;
        }


        public void Load()
        {
            On.TowerFall.KeyboardInput.GetState += KeyboardInput_GetState;

            MenuConfirm_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuConfirm").GetGetMethod(), MenuConfirm_patch);
            MenuSkipReplay_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuSkipReplay").GetGetMethod(), MenuSkipReplay_patch);
            MenuSaveReplay_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuSaveReplay").GetGetMethod(), MenuSaveReplay_patch);
            MenuSaveReplayCheck_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuSaveReplayCheck").GetGetMethod(), MenuSaveReplayCheck_patch);
        }

        public void Unload()
        {
            On.TowerFall.KeyboardInput.GetState -= KeyboardInput_GetState;

            MenuConfirm_hook.Dispose();
            MenuSkipReplay_hook.Dispose();
            MenuSaveReplay_hook.Dispose();
            MenuSaveReplayCheck_hook.Dispose();
        }

        private static bool MenuConfirm_patch(MenuConfirm_orig orig, KeyboardInput self)
        {
            return Intercept(orig(self));
        }

        private static bool MenuSkipReplay_patch(MenuSkipReplay_orig orig, KeyboardInput self)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            return netplayManager != null && netplayManager.IsInit() || netplayManager.IsReplayMode() ? true : orig(self);
        }

        private static bool MenuSaveReplay_patch(MenuSkipReplay_orig orig, KeyboardInput self)
        {
            return false;
        }

        private static bool MenuSaveReplayCheck_patch(MenuSkipReplay_orig orig, KeyboardInput self)
        {
            return false;
        }

        private InputState KeyboardInput_GetState(On.TowerFall.KeyboardInput.orig_GetState orig, KeyboardInput self)
        {
            //EnsureStateMachine();
            if (_netplayManager.GetNetplayMode() != Domain.Models.NetplayMode.Test && !_netplayManager.IsSynchronized())
            {
                return orig(self);
            }

            (_, var mode) = ServiceCollections.ResolveStateMachineService();

            if (!mode.IsNetplay() && _netplayManager.GetNetplayMode() == Domain.Models.NetplayMode.Uninitialized)
            {
                return orig(self);
            }

            var polledInput = orig(self);

            if (IsLocalPlayerKeyboard)
            {
                if (!_netplayManager.IsReplayMode())
                {
                    _inputService.UpdatePolledInput(polledInput);
                }
                return _inputService.GetCurrentInput(_inputService.GetLocalPlayerInputIndex()); //TODO: get by player instead of index
            }

            return _inputService.GetCurrentInput(_inputService.GetRemotePlayerInputIndex());
        }

        private static bool Intercept(bool actualInput)
        {
            (var state_machine, _) = ServiceCollections.ResolveStateMachineService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            var init = state_machine.IsInitialized();
            var canStart = state_machine.CanStart();
            var isNetplayInit = netplayManager.IsInit();

            if (netplayManager.IsDisconnected())
            {
                return actualInput;
            }

            if (TFGame.Instance.Scene is Level && (TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() != -1)
            {
                return actualInput;
            }

            if (IsPlayer2 && TFGame.Instance.Scene is MainMenu)
            {
                var dynMenu = DynamicData.For(TFGame.Instance.Scene as MainMenu);
                var state = dynMenu.Get<MainMenu.MenuState>("state");

                if (state == MainMenu.MenuState.Rollcall && matchmakingService.HasOpponentChoosed())
                {
                    return true;
                }
            }

            if (canStart)
            {
                return true;
            }

            if (init)
            {
                return false;
            }

            if (!init)
            {
                return actualInput;
            }
            else
            {
                if (isNetplayInit)
                {
                    return true;
                }
                else
                {
                    return actualInput;
                }
            }
        }

        private bool IsLocalPlayerKeyboard => TFGame.PlayerInputs[0] is KeyboardInput;

        private static bool IsPlayer2 => TFGame.PlayerInputs[1] is KeyboardInput;
    }
}
