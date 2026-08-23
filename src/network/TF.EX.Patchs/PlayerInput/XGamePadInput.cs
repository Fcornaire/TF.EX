using HarmonyLib;
using TF.EX.Domain.Interop;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports.TF;
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
        [HarmonyPatch("get_Name")]
        public static void Name_patch(ref string __result, XGamepadInput __instance)
        {
            if (PlayerInputPatch.TryGetNetplayName(__instance, out var name))
            {
                __result = name;
            }
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
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuDown")]
        public static void MenuDown_patch(ref bool __result, XGamepadInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
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

        private static bool IsForeignSeat(XGamepadInput self)
        {
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.Instance.Scene is not MainMenu
                || TowerFall.MainMenu.VersusMatchSettings == null
                || !TowerFall.MainMenu.VersusMatchSettings.Mode.IsNetplay()
                || ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty)
            {
                return false;
            }

            return inputService.IsInputLocked()
                || inputService.GetInputIndex(self) != inputService.GetLocalPlayerInputIndex();
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuAlt")]
        public static void MenuAlt_patch(ref bool __result, XGamepadInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuAlt2")]
        public static void MenuAlt2_patch(ref bool __result, XGamepadInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuAlt2Check")]
        public static void MenuAlt2Check_patch(ref bool __result, XGamepadInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuConfirmCheck")]
        public static void MenuConfirmCheck_patch(ref bool __result, XGamepadInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuStartCheck")]
        public static void MenuStartCheck_patch(ref bool __result, XGamepadInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuBackCheck")]
        public static void MenuBackCheck_patch(ref bool __result, XGamepadInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuAltCheck")]
        public static void MenuAltCheck_patch(ref bool __result, XGamepadInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
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

            if (!roundStarted && !netplayManager.IsReplayMode())
            {
                inputService.ResetPolledInput();
                __result = new InputState();
                return;
            }

            if (netplayManager.IsInit() || netplayManager.IsReplayMode())
            {
                var seat = inputService.GetInputIndex(__instance);

                if (seat == inputService.GetLocalPlayerInputIndex() && !netplayManager.IsReplayMode())
                {
                    inputService.UpdatePolledInput(__result, __instance.GetRightStick());
                }

                if (netplayManager.IsReplayMode() && seat >= 0 && seat == (ReplayApi.Current?.GetTakeoverSeat() ?? -1))
                {
                    if (ReplayApi.Current.IsTakeoverCapturing())
                    {
                        return;
                    }

                    var live = ReplayApi.Current.GetTakeoverInputFlat();

                    if (live != null)
                    {
                        __result = live.ToInputs()[0].ToTFInput();
                    }

                    return;
                }

                __result = inputService.GetCurrentInput(seat).ToTFInput();
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

            var currentModeIsNetplay = TowerFall.MainMenu.VersusMatchSettings?.Mode.IsNetplay() == true;
            if (!currentModeIsNetplay && !isNetplayInit && !isReplayMode && lobby.IsEmpty)
            {
                return actualInput;
            }

            if (IsForeignSeat(self))
            {
                return false; //Ignore input for other players in netplay
            }

            if (TFGame.Instance.Scene is MapScene && currentModeIsNetplay && !matchmakingService.GetOwnLobby().IsEmpty)
            {
                return true;
            }

            if (isPaused)
            {
                return actualInput;
            }

            if (isReplayMode)
            {
                if (TFGame.Instance.Scene is TowerFall.Level replayLevel && replayLevel.Session?.RoundLogic?.RoundStarted == false)
                {
                    return ReplayIntroPacing.ConfirmLatched;
                }

                return true;
            }

            if (TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() != -1)
            {
                var matchResults = (TFGame.Instance.Scene as TowerFall.Level).Get<VersusMatchResults>();

                if (matchResults != null && Traverse.Create(matchResults).Field("finished").GetValue<bool>())
                {
                    return actualInput;
                }
            }

            if (TFGame.Instance.Scene is MainMenu && TowerFall.MainMenu.VersusMatchSettings != null)
            {
                var state = Traverse.Create(TFGame.Instance.Scene as MainMenu).Field("state").GetValue<MainMenu.MenuState>();

                var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode;

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

                        return index == inputService.GetLocalPlayerInputIndex();
                    });

                    var rollcallState = Traverse.Create(rollcallElement).Field("state").GetValue<StateMachine>();
                    if (rollcallState.State == 0)
                    {
                        return actualInput;
                    }

                    if (actualInput && matchmakingService.CanHostStart())
                    {
                        matchmakingService.RequestStart();
                    }

                    return false;
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
            var inputService = ServiceCollections.ResolveInputService();

            var lobby = matchmakingService.GetOwnLobby();

            if (IsForeignSeat(self))
            {
                return false; //Ignore input for other players in netplay
            }

            if (TFGame.Instance.Scene is MainMenu && TowerFall.MainMenu.VersusMatchSettings != null)
            {
                var state = Traverse.Create(TFGame.Instance.Scene as MainMenu).Field("state").GetValue<MainMenu.MenuState>();

                var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode;

                if (state == MainMenu.MenuState.Rollcall && currentMode.IsNetplay() && !lobby.IsEmpty)
                {
                    if (matchmakingService.IsLobbyReady())
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

                        return index == inputService.GetLocalPlayerInputIndex();
                    });

                    var rollcallState = Traverse.Create(rollcallElement).Field("state").GetValue<StateMachine>();

                    if (rollcallState.State == 0)
                    {
                        return actualResult;
                    }


                    if (actualResult && matchmakingService.CanHostStart())
                    {
                        matchmakingService.RequestStart();
                    }

                    return false;
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

            if (IsForeignSeat(self))
            {
                return false; //Ignore input for other players in netplay
            }

            if (isPaused)
            {
                return actualInput;
            }

            if (TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() != -1)
            {
                var matchResults = (TFGame.Instance.Scene as TowerFall.Level).Get<VersusMatchResults>();

                if (matchResults != null && Traverse.Create(matchResults).Field("finished").GetValue<bool>())
                {
                    return actualInput;
                }
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (TFGame.Instance.Scene is MapScene
                && TowerFall.MainMenu.VersusMatchSettings?.Mode.IsNetplay() == true
                && !matchmakingService.GetOwnLobby().IsEmpty)
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

            if (IsForeignSeat(self))
            {
                return false;
            }

            if (isPaused)
            {
                return actualInput;
            }

            if (TFGame.Instance.Scene is MainMenu && TowerFall.MainMenu.VersusMatchSettings != null)
            {
                var state = Traverse.Create(TFGame.Instance.Scene as MainMenu).Field("state").GetValue<MainMenu.MenuState>();
                var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode;

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

                        return index == inputService.GetLocalPlayerInputIndex();
                    });

                    return actualInput;
                }
            }

            if (TFGame.Instance.Scene is MapScene
                && TowerFall.MainMenu.VersusMatchSettings?.Mode.IsNetplay() == true
                && !matchmakingService.GetOwnLobby().IsEmpty)
            {
                return false;
            }

            if (isNetplayInit)
            {
                return true;
            }

            return actualInput;
        }

    }
}
