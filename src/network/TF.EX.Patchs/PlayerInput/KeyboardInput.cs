using HarmonyLib;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Patchs.PlayerInput
{
    //TODO: refactor
    [HarmonyPatch(typeof(KeyboardInput))]
    public class KeyboardInputPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("get_MenuConfirm")]
        public static void MenuConfirm_patch(ref bool __result, KeyboardInput __instance)
        {
            __result = Intercept(__instance, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_Name")]
        public static void Name_patch(ref string __result, KeyboardInput __instance)
        {
            if (PlayerInputPatch.TryGetNetplayName(__instance, out var name))
            {
                __result = name;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuStart")]
        public static void MenuStart_patch(ref bool __result, KeyboardInput __instance)
        {
            __result = InterceptStart(__instance, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuSkipReplay")]
        public static void MenuSkipReplay_patch(ref bool __result, KeyboardInput __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (netplayManager != null && netplayManager.IsInit() || netplayManager.IsReplayMode())
            {
                __result = true;
            }
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
        [HarmonyPatch("get_MenuLeft")]
        public static void MenuLeft_patch(ref bool __result, KeyboardInput __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (IsForeignSeat(__instance))
            {
                __result = false;
            }

            if (TFGame.Instance.Scene is MapScene && !matchmakingService.GetOwnLobby().IsEmpty)
            {
                __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuRight")]
        public static void MenuRight_patch(ref bool __result, KeyboardInput __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (IsForeignSeat(__instance))
            {
                __result = false;
            }

            if (TFGame.Instance.Scene is MapScene && !matchmakingService.GetOwnLobby().IsEmpty)
            {
                __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuUp")]
        public static void MenuUp_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuDown")]
        public static void MenuDown_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        private static bool IsForeignSeat(KeyboardInput self)
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
        public static void MenuAlt_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuAlt2")]
        public static void MenuAlt2_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuAlt2Check")]
        public static void MenuAlt2Check_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuBack")]
        public static void MenuBack_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuConfirmCheck")]
        public static void MenuConfirmCheck_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuStartCheck")]
        public static void MenuStartCheck_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuBackCheck")]
        public static void MenuBackCheck_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuAltCheck")]
        public static void MenuAltCheck_patch(ref bool __result, KeyboardInput __instance)
        {
            if (IsForeignSeat(__instance)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetState")]
        public static void KeyboardInput_GetState(ref InputState __result, KeyboardInput __instance)
        {
            var inputService = ServiceCollections.ResolveInputService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();

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

            var seat = inputService.GetInputIndex(__instance);

            if (seat == inputService.GetLocalPlayerInputIndex() && !netplayManager.IsReplayMode())
            {
                inputService.UpdatePolledInput(__result);
            }

            __result = inputService.GetCurrentInput(seat).ToTFInput();
        }

        private static bool InterceptStart(KeyboardInput self, bool actualResult)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.Instance.Scene is not MainMenu || TowerFall.MainMenu.VersusMatchSettings == null)
            {
                return actualResult;
            }

            var lobby = matchmakingService.GetOwnLobby();

            if (!TowerFall.MainMenu.VersusMatchSettings.Mode.IsNetplay() || lobby.IsEmpty)
            {
                return actualResult;
            }

            if (inputService.GetInputIndex(self) != inputService.GetLocalPlayerInputIndex())
            {
                return false;
            }

            var state = Traverse.Create(TFGame.Instance.Scene as MainMenu).Field<MainMenu.MenuState>("state").Value;

            if (state != MainMenu.MenuState.Rollcall)
            {
                return actualResult;
            }

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
                return DynamicData.For(rc).Get<int>("playerIndex") == inputService.GetLocalPlayerInputIndex();
            });

            var rollcallState = DynamicData.For(rollcallElement).Get<StateMachine>("state");

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

        //TODO: refactor to have a unique intercept for all inputs
        private static bool Intercept(KeyboardInput self, bool actualInput)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var inputService = ServiceCollections.ResolveInputService();

            var isReplayMode = netplayManager.IsReplayMode();
            var isPaused = TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Paused;

            var isNetplayInit = netplayManager.IsInit();

            var lobby = matchmakingService.GetOwnLobby();

            if (IsForeignSeat(self))
            {
                return false; //Ignore input for other players in netplay
            }

            if (isPaused)
            {
                return actualInput;
            }

            if (isReplayMode)
            {
                return true;
            }

            if (TFGame.Instance.Scene is Level && (TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() != -1)
            {
                var dynMacthResults = DynamicData.For((TFGame.Instance.Scene as TowerFall.Level).Get<VersusMatchResults>());
                var isFinished = dynMacthResults.Get<bool>("finished");

                if (isFinished)
                {
                    return actualInput;
                }
            }

            if (TFGame.Instance.Scene is MapScene && !matchmakingService.GetOwnLobby().IsEmpty)
            {
                return true;
            }

            if (TFGame.Instance.Scene is MainMenu && TowerFall.MainMenu.VersusMatchSettings != null)
            {
                var state = Traverse.Create(TFGame.Instance.Scene as MainMenu).Field<MainMenu.MenuState>("state").Value;
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
                        var dyn = DynamicData.For(rc);
                        var index = dyn.Get<int>("playerIndex");

                        return index == inputService.GetLocalPlayerInputIndex();
                    });

                    var dynRollcallElement = DynamicData.For(rollcallElement);
                    StateMachine rollcallState = dynRollcallElement.Get<StateMachine>("state");
                    if (rollcallState.State == 0)
                    {
                        return actualInput;
                    }


                    if (actualInput && matchmakingService.CanHostStart())
                    {
                        matchmakingService.RequestStart();
                    }

                    return ServiceCollections.ResolveMatchmakingService().IsLobbyReady();
                }
            }

            if (netplayManager.IsDisconnected())
            {
                return actualInput;
            }

            if (isNetplayInit)
            {
                return true;
            }
            else
            {
                return actualInput;
            }

        }

        private static bool IsLocalPlayerKeyboard(KeyboardInput self, IInputService inputService)
        {
            return inputService.GetInputIndex(self) == 0;
        }
    }
}
