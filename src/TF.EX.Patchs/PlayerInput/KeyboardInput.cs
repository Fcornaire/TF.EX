using HarmonyLib;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions.Scene;
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
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.Instance.Scene is MainMenu
               && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
               && inputService.GetInputIndex(__instance) != 0)
            {
                __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuRight")]
        public static void MenuRight_patch(ref bool __result, KeyboardInput __instance)
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
        [HarmonyPatch("get_MenuUp")]
        public static void MenuUp_patch(ref bool __result, KeyboardInput __instance)
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
        public static void MenuDown_patch(ref bool __result, KeyboardInput __instance)
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

            if (!roundStarted) //Ignore if round not started to prevent useless rollback
            {
                inputService.ResetPolledInput();
                __result = new InputState();
                return;
            }

            if (IsLocalPlayerKeyboard(__instance, inputService))
            {
                if (!netplayManager.IsReplayMode())
                {
                    inputService.UpdatePolledInput(__result);
                }
                __result = inputService.GetCurrentInput(inputService.GetLocalPlayerInputIndex()).ToTFInput(); //TODO: get by player instead of index
                return;
            }

            __result = inputService.GetCurrentInput(inputService.GetRemotePlayerInputIndex()).ToTFInput();
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

            if (TFGame.Instance.Scene is MainMenu
               && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
               && inputService.GetInputIndex(self) != 0)
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
                var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

                if (state == MainMenu.MenuState.Rollcall && currentMode.IsNetplay())
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

                        return index == 0;
                    });

                    var dynRollcallElement = DynamicData.For(rollcallElement);
                    StateMachine rollcallState = dynRollcallElement.Get<StateMachine>("state");
                    if (rollcallState.State == 0)
                    {
                        return actualInput;
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
