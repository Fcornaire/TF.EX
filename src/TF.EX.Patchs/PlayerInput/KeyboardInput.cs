using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Patchs.PlayerInput
{
    //TODO: refactor
    public class KeyboardInputPatch : IHookable
    {
        private IInputService _inputService;
        private INetplayManager _netplayManager;

        private delegate bool MenuConfirm_orig(KeyboardInput self);
        private delegate bool MenuSkipReplay_orig(KeyboardInput self);
        private delegate bool MenuSaveReplay_orig(KeyboardInput self);
        private delegate bool MenuSaveReplayCheck_orig(KeyboardInput self);
        private delegate bool MenuLeft_orig(KeyboardInput self);
        private delegate bool MenuRight_orig(KeyboardInput self);
        private delegate bool MenuUp_orig(KeyboardInput self);
        private delegate bool MenuDown_orig(KeyboardInput self);

        private static IDetour MenuConfirm_hook;
        private static IDetour MenuSkipReplay_hook;
        private static IDetour MenuSaveReplay_hook;
        private static IDetour MenuSaveReplayCheck_hook;
        private static IDetour MenuLeft_hook;
        private static IDetour MenuRight_hook;
        private static IDetour MenuUp_hook;
        private static IDetour MenuDown_hook;

        public KeyboardInputPatch(IInputService inputService, INetplayManager netplayManager)
        {
            _inputService = inputService;
            _netplayManager = netplayManager;
        }


        public void Load()
        {
            On.TowerFall.KeyboardInput.GetState += KeyboardInput_GetState;

            MenuConfirm_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuConfirm").GetGetMethod(), MenuConfirm_patch);
            MenuSkipReplay_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuSkipReplay").GetGetMethod(), MenuSkipReplay_patch);
            MenuSaveReplay_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuSaveReplay").GetGetMethod(), MenuSaveReplay_patch);
            MenuSaveReplayCheck_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuSaveReplayCheck").GetGetMethod(), MenuSaveReplayCheck_patch);
            MenuLeft_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuLeft").GetGetMethod(), MenuLeft_patch);
            MenuRight_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuRight").GetGetMethod(), MenuRight_patch);
            MenuUp_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuUp").GetGetMethod(), MenuUp_patch);
            MenuDown_hook = new Hook(typeof(KeyboardInput).GetProperty("MenuDown").GetGetMethod(), MenuDown_patch);
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
            return Intercept(self, orig(self));
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

        private static bool MenuLeft_patch(MenuConfirm_orig orig, KeyboardInput self)
        {
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.Instance.Scene is MainMenu
               && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
               && inputService.GetInputIndex(self) != 0)
            {
                return false;
            }

            return orig(self);
        }

        private static bool MenuRight_patch(MenuConfirm_orig orig, KeyboardInput self)
        {
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.Instance.Scene is MainMenu
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
                && inputService.GetInputIndex(self) != 0)
            {
                return false;
            }

            return orig(self);
        }

        private static bool MenuUp_patch(MenuConfirm_orig orig, KeyboardInput self)
        {
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.Instance.Scene is MainMenu
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
                && inputService.GetInputIndex(self) != 0)
            {
                return false;
            }

            return orig(self);
        }

        private static bool MenuDown_patch(MenuConfirm_orig orig, KeyboardInput self)
        {
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.Instance.Scene is MainMenu
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
                && inputService.GetInputIndex(self) != 0)
            {
                return false;
            }

            return orig(self);
        }

        private InputState KeyboardInput_GetState(On.TowerFall.KeyboardInput.orig_GetState orig, KeyboardInput self)
        {
            //EnsureStateMachine();
            if (_netplayManager.GetNetplayMode() != Domain.Models.NetplayMode.Test && !_netplayManager.IsReplayMode() && !_netplayManager.IsSynchronized())
            {
                return orig(self);
            }

            var mode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            if (!mode.IsNetplay() && _netplayManager.GetNetplayMode() == Domain.Models.NetplayMode.Uninitialized)
            {
                return orig(self);
            }

            var polledInput = orig(self);

            if (IsLocalPlayerKeyboard(self))
            {
                if (!_netplayManager.IsReplayMode())
                {
                    _inputService.UpdatePolledInput(polledInput);
                }
                return _inputService.GetCurrentInput(_inputService.GetLocalPlayerInputIndex()); //TODO: get by player instead of index
            }

            return _inputService.GetCurrentInput(_inputService.GetRemotePlayerInputIndex());
        }

        //TODO: refactor to have a unique intercept for all inputs
        private static bool Intercept(KeyboardInput self, bool actualInput)
        {
            (var state_machine, _) = ServiceCollections.ResolveStateMachineService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var inputService = ServiceCollections.ResolveInputService();

            var init = state_machine.IsInitialized();
            var canStart = state_machine.CanStart();
            var isNetplayInit = netplayManager.IsInit();

            if (TFGame.Instance.Scene is MainMenu
               && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
               && inputService.GetInputIndex(self) != 0)
            {
                return false; //Ignore input for other players in netplay
            }

            if (netplayManager.IsDisconnected())
            {
                return actualInput;
            }

            if (TFGame.Instance.Scene is Level && (TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() != -1)
            {
                return actualInput;
            }

            var isPlayer2 = inputService.GetInputIndex(self) == 1;

            if (isPlayer2 && TFGame.Instance.Scene is MainMenu)
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

        private bool IsLocalPlayerKeyboard(KeyboardInput self)
        {
            return _inputService.GetInputIndex(self) == 0;
        }

        private bool IsPlayer2(KeyboardInput self)
        {
            return _inputService.GetInputIndex(self) == 1;
        }
    }
}
