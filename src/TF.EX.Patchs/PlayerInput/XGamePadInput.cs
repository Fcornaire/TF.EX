using HarmonyLib;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.Patchs.PlayerInput
{
    //TODO: refactor
    [HarmonyPatch(typeof(XGamepadInput))]
    public class XGamePadInputPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("get_MenuConfirm")]

        public static void MenuConfirm_patch(ref bool __result, XGamepadInput __instance)
        {
            __result = InterceptConfirm(__instance, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuStart")]
        public static void MenuStart_patch(ref bool __result, XGamepadInput __instance)
        {
            __result = InterceptStart(__instance, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuBack")]
        public static void MenuBack_patch(ref bool __result, XGamepadInput __instance)
        {
            __result = InterceptBack(__instance, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuLeft")]
        public static void MenuLeft_patch(ref bool __result, XGamepadInput __instance)
        {
            __result = InterceptLR(__instance, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuRight")]
        public static void MenuRight_patch(ref bool __result, XGamepadInput __instance)
        {
            __result = InterceptLR(__instance, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuUp")]
        public static void MenuUp_patch(ref bool __result, XGamepadInput __instance)
        {
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.Instance.Scene is MainMenu
               && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
               && inputService.GetInputIndex(__instance) != 0)
            {
                __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuDown")]
        public static void MenuDown_patch(ref bool __result, XGamepadInput __instance)
        {
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.Instance.Scene is MainMenu
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
                && inputService.GetInputIndex(__instance) != 0)
            {
                __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuSkipReplay")]
        public static void MenuSkipReplay_patch(ref bool __result)
        {
            __result = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuSaveReplay")]
        public static void MenuSaveReplay_patch(ref bool __result)
        {
            if (TFGame.Instance.Scene is not MainMenu || TFGame.Instance.Scene is MainMenu mainMenu && mainMenu.State != MainMenu.MenuState.Rollcall)
            {
                __result = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("get_MenuSaveReplayCheck")]
        public static bool MenuSaveReplayCheck_patch()
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(XGamepadInput.GetState))]
        public static void XGamepadInput_GetState(ref InputState __result, XGamepadInput __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();

            if (netplayManager.GetNetplayMode() != Domain.Models.NetplayMode.Test
                && netplayManager.GetNetplayMode() != Domain.Models.NetplayMode.Replay
                && !netplayManager.IsSynchronized())
            {
                return;
            }

            var level = TFGame.Instance.Scene as TowerFall.Level;

            if (level == null)
            {
                return;
            }

            var roundStarted = level.Session.RoundLogic.RoundStarted;

            if (!roundStarted) //Ignore if round not started to prevent useless rollback
            {
                inputService.ResetPolledInput();
                __result = new InputState();
                return;
            }

            if (netplayManager.IsInit() || netplayManager.IsReplayMode())
            {
                if (IsLocalPlayerGamePad(__instance, inputService))
                {
                    if (!netplayManager.IsReplayMode())
                    {
                        inputService.UpdatePolledInput(__result, __instance.GetRightStick());
                    }
                    else
                    {
                        //InterceptReplay(polledInput); TODO: implement replay interception
                    }
                    __result = inputService.GetCurrentInput(inputService.GetLocalPlayerInputIndex()).ToTFInput();
                    return;
                }

                __result = inputService.GetCurrentInput(inputService.GetRemotePlayerInputIndex()).ToTFInput();
            }
            //else
            //{
            //    return orig(self);
            //}
        }

        //TODO: refactor to have a unique intercept for all inputs
        private static bool InterceptConfirm(XGamepadInput self, bool actualInput)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            var isNetplayInit = netplayManager.IsInit();
            var isReplayMode = netplayManager.IsReplayMode();
            var isPaused = TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Paused;

            var lobby = matchmakingService.GetOwnLobby();

            if (TFGame.Instance.Scene is MainMenu
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
                && inputService.GetInputIndex(self) != 0
                && !lobby.IsEmpty)
            {
                return false; //Ignore input for other players in netplay
            }

            if (TFGame.Instance.Scene is MapScene && !matchmakingService.GetOwnLobby().IsEmpty)
            {
                return true;
            }

            if (isPaused)
            {
                return actualInput;
            }

            if (isReplayMode)
            {
                return true;
            }

            if (TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() != -1)
            {
                var isFinished = Traverse.Create((TFGame.Instance.Scene as TowerFall.Level).Get<VersusMatchResults>()).Field("finished").GetValue<bool>();

                if (isFinished)
                {
                    return actualInput;
                }
            }

            if (TFGame.Instance.Scene is MainMenu)
            {
                var state = Traverse.Create(TFGame.Instance.Scene as MainMenu).Field("state").GetValue<MainMenu.MenuState>();

                var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

                if (state == MainMenu.MenuState.Rollcall && currentMode.IsNetplay() && !lobby.IsEmpty)
                {
                    if (ServiceCollections.ResolveMatchmakingService().IsLobbyReady())
                    {
                        return true;
                    }

                    if (matchmakingService.IsSpectator())
                    {
                        return false;
                    }

                    var rollcallElement = (TFGame.Instance.Scene as MainMenu).GetAll<RollcallElement>().First(rc =>
                    {
                        var index = Traverse.Create(rc).Field("playerIndex").GetValue<int>();

                        return index == 0;
                    });

                    var rollcallState = Traverse.Create(rollcallElement).Field("state").GetValue<StateMachine>();
                    if (rollcallState.State == 0)
                    {
                        return actualInput;
                    }

                    return actualInput;
                }
            }

            if (isNetplayInit)
            {
                return true;
            }

            if (netplayManager.IsDisconnected())
            {
                return actualInput;
            }

            return actualInput;
        }

        private static bool InterceptStart(XGamepadInput self, bool actualResult)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (TFGame.Instance.Scene is MainMenu)
            {
                var state = Traverse.Create(TFGame.Instance.Scene as MainMenu).Field("state").GetValue<MainMenu.MenuState>();

                var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();
                var lobby = matchmakingService.GetOwnLobby();

                if (state == MainMenu.MenuState.Rollcall && currentMode.IsNetplay() && !lobby.IsEmpty)
                {
                    if (ServiceCollections.ResolveMatchmakingService().IsLobbyReady())
                    {
                        return true;
                    }

                    if (matchmakingService.IsSpectator())
                    {
                        return false;
                    }

                    var rollcallElement = (TFGame.Instance.Scene as MainMenu).GetAll<RollcallElement>().First(rc =>
                    {
                        var index = Traverse.Create(rc).Field("playerIndex").GetValue<int>();

                        return index == 0;
                    });

                    var rollcallState = Traverse.Create(rollcallElement).Field("state").GetValue<StateMachine>();
                    if (rollcallState.State == 0)
                    {
                        return actualResult;
                    }

                    return actualResult;
                }
            }

            return actualResult;
        }

        private static bool InterceptBack(XGamepadInput self, bool actualInput)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();

            var isNetplayInit = netplayManager.IsInit();
            var isPaused = TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Paused;

            if (TFGame.Instance.Scene is MainMenu
                && TowerFall.MainMenu.VersusMatchSettings != null
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
                && inputService.GetInputIndex(self) != 0)
            {
                return false; //Ignore input for other players in netplay
            }

            if (isPaused)
            {
                return actualInput;
            }

            if (TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() != -1)
            {
                var isFinished = Traverse.Create((TFGame.Instance.Scene as TowerFall.Level).Get<VersusMatchResults>()).Field("finished").GetValue<bool>();

                if (isFinished)
                {
                    return actualInput;
                }
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (TFGame.Instance.Scene is MapScene && matchmakingService.GetOwnLobby().IsEmpty)
            {
                return false;
            }

            if (isNetplayInit)
            {
                return true;
            }

            return actualInput;

        }

        private static bool InterceptLR(XGamepadInput self, bool actualInput)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            var isNetplayInit = netplayManager.IsInit();
            var isPaused = TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Paused;

            if (TFGame.Instance.Scene is MainMenu
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
                && inputService.GetInputIndex(self) != 0)
            {
                return false;
            }

            if (isPaused)
            {
                return actualInput;
            }

            if (TFGame.Instance.Scene is MainMenu)
            {
                var state = Traverse.Create(TFGame.Instance.Scene as MainMenu).Field("state").GetValue<MainMenu.MenuState>();
                var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

                var lobby = matchmakingService.GetOwnLobby();

                if (state == MainMenu.MenuState.Rollcall && currentMode.IsNetplay() && !lobby.IsEmpty)
                {
                    if (ServiceCollections.ResolveMatchmakingService().IsLobbyReady())
                    {
                        return true;
                    }

                    if (matchmakingService.IsSpectator())
                    {
                        return false;
                    }

                    var rollcallElement = (TFGame.Instance.Scene as MainMenu).GetAll<RollcallElement>().First(rc =>
                    {
                        var index = Traverse.Create(rc).Field("playerIndex").GetValue<int>();

                        return index == 0;
                    });

                    return actualInput;
                }
            }

            if (TFGame.Instance.Scene is MapScene && !matchmakingService.GetOwnLobby().IsEmpty)
            {
                return false;
            }

            if (isNetplayInit)
            {
                return true;
            }

            return actualInput;
        }

        private static bool IsLocalPlayerGamePad(XGamepadInput self, IInputService inputService)
        {
            return inputService.GetInputIndex(self) == 0;
        }
    }
}
