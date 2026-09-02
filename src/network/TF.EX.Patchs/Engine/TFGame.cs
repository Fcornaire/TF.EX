using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Diagnostics;
using System.Reflection;
using TextCopy;
using TF.EX.Common;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.Context;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Patchs.Engine
{
    [HarmonyPatch(typeof(TowerFall.TFGame))]
    public class TFGamePatch
    {
        private static bool _updateFlowStarted;
        private static volatile bool _restartWhenReady;
        private static volatile bool _updateFailed;
        private static volatile bool _checkDone;
        private static Action _pendingNetplayEntry;
        private static UpdaterDialog _updaterDialog;

        private static readonly Stopwatch UpdateClock = Stopwatch.StartNew();
        private static TimeSpan LastUpdate;
        private static TimeSpan Accumulator;

        private const double SLOW_RATIO = 1.1;

        private const double DELAYED_CATCHUP_RATIO = 1.25;
        private const double LIVE_CATCHUP_RATIO = 8.0;
        private const int DELAYED_CATCHUP_THRESHOLD = Domain.Models.Constants.NETPLAY_FPS / 2;
        private const float MAX_NOTIFICATION_TEXT_WIDTH = 290f; //vanilla screen minus some margin
        private static bool? _preSessionFixedStep;

        private static readonly MethodInfo _mInputUpdate = AccessTools.Method(typeof(MInput), "Update"); //Minput Update is an internal static method...

        private static bool frameByFrame { get; set; } = false;

        [HarmonyPrefix]
        [HarmonyPatch("OnExiting")]
        public static void TFGame_OnExiting(TFGame __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();

            if (__instance.Scene is Level && netplayManager.IsServerMode())
            {
                replayService.Export();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnSceneTransition")]
        public static void TFGame_OnSceneTransition(TFGame __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (__instance.PreviousScene is Level && __instance.Scene is TowerFall.MainMenu)
            {
                var mode = TowerFall.MainMenu.VersusMatchSettings?.Mode;
                var wasExDriven = (mode != null && mode.Value.IsNetplay())
                    || netplayManager.IsReplayMode()
                    || netplayManager.IsTestMode()
                    || netplayManager.IsInit();

                StateApi.Current.ResetRngOverride();
                SpectatorInputDisplay.Stop();
                Domain.CustomComponent.LightPauseMenu.ForceClose();

                if (!ScenarioSweeper.IsRunning)
                {
                    StateApi.Current.SetVersusLevels(null, null);
                }

                if (!netplayManager.IsReplayMode())
                {
                    StateApi.Current.PurgeCache();
                }

                netplayManager.Reset();

                var isReturningToLobby = (__instance.Scene as TowerFall.MainMenu).State == TowerFall.MainMenu.MenuState.Rollcall && !ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty;

                if (!isReturningToLobby)
                {
                    netplayManager.ResetMode();
                    ServiceCollections.ResolveInputService().RemoveFakeControllers();
                }

                if (wasExDriven)
                {
                    TF.EX.Domain.Extensions.TFGameExtensions.ResetVersusChoices();
                }

                if (!isReturningToLobby)
                {
                    NetplayOptions.Restore();
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Load")]
        public static void TFGame_Load()
        {
            LastUpdate = UpdateClock.Elapsed;
            Accumulator = TimeSpan.Zero;
        }

        [HarmonyReversePatch]
        [HarmonyPatch("Update")]
        public static void TFGame_Update_orig(TFGame __instance, GameTime gameTime)
        {
            throw new NotImplementedException("This method should be patched by Harmony, not called directly.");
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool TFGameUpdate_Patch(TowerFall.TFGame __instance, GameTime gameTime)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();
            var inputService = ServiceCollections.ResolveInputService();
            var autoUpdater = ServiceCollections.ResolveAutoUpdater();
            var syncTestUtilsService = ServiceCollections.ResolveSyncTestUtilsService();
            var logger = ServiceCollections.ResolveLogger();

            ServiceCollections.ResolveMatchmakingService().DrainGameThreadActions();

            netplayManager.PublishCaptureFlag();

            if (ScenarioSweeper.IsRunning)
            {
                ScenarioSweeper.Update();
            }

            if (__instance.Scene is TowerFall.MainMenu)
            {
                HandleMenuAction(__instance, autoUpdater, inputService);
            }

            ManageTimeStep(__instance);

            if (!handleFrameByFrame(netplayManager))
            {
                return false;
            }

            if (netplayManager.IsReplayMode())
            {
                var dynTFGame = DynamicData.For(__instance);
                var gameLoaded = dynTFGame.Get<bool>("GameLoaded");

                var advance = ReplayApi.Current?.UpdatePlaybackControls() ?? true;

                HandleHurtboxToggle(netplayManager); //must come after UpdatePlaybackControls:

                if (advance && gameLoaded && __instance.Scene is Level && !(__instance.Scene as Level).Paused)
                {
                    replayService.RunFrame();
                    //(self.Scene as Level).LoadState(_replayService.GetCurrentRecord().GameState);
                }

                if (!advance)
                {
                    return false;
                }

                TFGame_Update_orig(__instance, gameTime);

                return false;
            }

            if (!netplayManager.IsSynchronized() && netplayManager.GetNetplayMode() != NetplayMode.Test)
            {
                LastUpdate = UpdateClock.Elapsed;
                Accumulator = TimeSpan.Zero;
            }

            netplayManager.AbortIfSynchronizationFailed();

            if (netplayManager.HasFailedInitialConnection())
            {
                if (netplayManager.ConsumeAbortToVersusOptions() && __instance.Scene is TowerFall.Level failedLevel)
                {
                    TowerFall.Sounds.ui_invalid.Play();

                    var matchmakingService = ServiceCollections.ResolveMatchmakingService();
                    matchmakingService.DisconnectFromServer();
                    matchmakingService.ResetPeer();
                    matchmakingService.UpdateOwnLobby(new Domain.Models.WebSocket.Lobby());
                    replayService.Reset();

                    var menu = Domain.Extensions.LevelExtensions.GoToNetplayEntryMenu(failedLevel);

                    inputService.EnableAllControllers();
                    inputService.RebindLocalInput();

                    Notification.Create(menu, "Connection to the other players failed", 10, 450);
                }

                TFGame_Update_orig(__instance, gameTime);
                return false;
            }

            if (netplayManager.IsDisconnected())
            {
                TFGame_Update_orig(__instance, gameTime);
                return false;
            }

            if (!CanRunNetplayFrames(__instance.Scene, netplayManager))
            {
                TFGame_Update_orig(__instance, gameTime);
                return false;
            }


            if (!netplayManager.IsSynchronized() && !netplayManager.IsInit())
            {
                if (TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() != -1)
                {
                    var vs = (TFGame.Instance.Scene as TowerFall.Level).Get<VersusMatchResults>();
                    if (vs != null)
                    {
                        TFGame_Update_orig(__instance, gameTime);
                    }
                }

                return false;
            }

            if (!netplayManager.GetNetplayMode().Equals(NetplayMode.Test))
            {
                netplayManager.Poll();
            }

            if (!netplayManager.IsDisconnected())
            {
                if (netplayManager.IsSpectatorMode())
                {
                    ServiceCollections.ResolveMatchmakingService().ShowPendingSpectatorNoticeIfAny();
                }

                //ArtificialSlow(); //Only useful to test choppy/freezing condition

                double fpsDelta = __instance.IsFixedTimeStep ? __instance.TargetElapsedTime.TotalSeconds : 1.0 / Domain.Models.Constants.VANILLA_FPS;

                if (netplayManager.IsFramesAhead())
                {
                    fpsDelta *= SLOW_RATIO;
                }
                else if (netplayManager.IsSpectatorMode() && GGRSFFI.netplay_frames_behind() > DELAYED_CATCHUP_THRESHOLD)
                {
                    fpsDelta /= netplayManager.IsSpectatorCatchupEnabled() ? LIVE_CATCHUP_RATIO : DELAYED_CATCHUP_RATIO;
                }

                var now = UpdateClock.Elapsed;
                Accumulator = Accumulator.Add(now - LastUpdate);
                LastUpdate = now;

                while (Accumulator.TotalSeconds > fpsDelta)
                {
                    Accumulator = Accumulator.Subtract(TimeSpan.FromSeconds(fpsDelta));

                    if (netplayManager.IsSynchronized() || netplayManager.GetNetplayMode().Equals(NetplayMode.Test))
                    {
                        netplayManager.EstablishSessionIfSynchronized();

                        if (ScenarioSweeper.IsRunning && netplayManager.IsTestMode())
                        {
                            ScenarioSweeper.Tick(__instance.Scene as Level);

                            if (!netplayManager.IsTestMode())
                            {
                                break;
                            }
                        }

                        var canAdvance = NetplayLogic(__instance.Scene as Level, netplayManager, inputService, replayService, syncTestUtilsService);

                        if (canAdvance)
                        {
                            if (netplayManager.CanAdvanceFrame())
                            {
                                netplayManager.ConsumeNetplayRequest(); //We should had one last advance Frame request to consume if no rollback if ggrs estime we can advance

                                if (!netplayManager.HaveRequestToHandle())
                                {
                                    netplayManager.UpdateFramesToReSimulate(0);
                                }

                                netplayManager.AdvanceGameState();
                                var dynScene = DynamicData.For(__instance.Scene);
                                dynScene.Set("FrameCounter", (float)GGRSFFI.netplay_current_frame());

                                if (netplayManager.IsSpectatorMode() && !netplayManager.IsReplayMode())
                                {
                                    SpectatorInputDisplay.Feed(inputService.GetCurrentInputs(), GGRSFFI.netplay_current_frame());
                                }

                                TFGame_Update_orig(__instance, gameTime);
                            }
                        }
                    }
                    else
                    {
                        logger.LogWarning($"Not syncrhonized {GGRSFFI.netplay_current_frame()}");
                    }
                }
            }

            return false;
        }

        internal static bool ShowHurtboxes { get; private set; }


        private static void HandleHurtboxToggle(INetplayManager netplayManager)
        {
            if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.F1) && netplayManager.IsReplayMode())
            {
                ShowHurtboxes = !ShowHurtboxes;
            }
        }

        private static bool handleFrameByFrame(INetplayManager netplayManager)
        {
            if (!netplayManager.IsReplayMode())
            {
                return true;
            }

            if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.F2))
            {
                frameByFrame = !frameByFrame;
            }

            if (frameByFrame)
            {
                _mInputUpdate.Invoke(null, null);

                if (!MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.F3) && !MInput.Keyboard.Check(Microsoft.Xna.Framework.Input.Keys.F4))
                {
                    return false;
                }

                if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.F5))
                {
                    frameByFrame = false;
                }
            }

            return true;
        }

        private static void HandleMenuAction(TFGame instance, IAutoUpdater autoUpdater, IInputService inputService)
        {
            HandleAutoUpdate(instance, autoUpdater, inputService);

            var scene = instance.Scene as TowerFall.MainMenu;
            switch (scene.State)
            {
                case TowerFall.MainMenu.MenuState.PressStart:
                    UpdateClipped(instance.Commands);
                    break;
                case TowerFall.MainMenu.MenuState.VersusOptions:
                    var dynCommands = DynamicData.For(instance.Commands);
                    dynCommands.Set("currentText", string.Empty);
                    break;
                case TowerFall.MainMenu.MenuState.Main:
                default:
                    break;
            }
        }

        private static void HandleAutoUpdate(TFGame instance, IAutoUpdater autoUpdater, IInputService inputService)
        {
            if (_restartWhenReady)
            {
                _restartWhenReady = false;
                FortRise.RiseCore.WillRestart = true;
                instance.Exit();
                return;
            }

            if (_updateFailed)
            {
                _updateFailed = false;
                _updateFlowStarted = false;
                _updaterDialog?.RemoveSelf();
                _updaterDialog = null;
                inputService.EnableAllControllers();
                Sounds.ui_invalid.Play();
                Notification.Create(instance.Scene, "Update failed! Online play requires the latest version", 10, 500);
                return;
            }

            if (_checkDone)
            {
                _checkDone = false;

                var enter = _pendingNetplayEntry;
                _pendingNetplayEntry = null;

                instance.Scene.RemoveLoader();
                inputService.EnableAllControllers();

                ResolveNetplayEntry(instance.Scene as TowerFall.MainMenu, autoUpdater, inputService, enter);
            }
        }

        public static void RequestNetplayEntry(TowerFall.MainMenu menu, Action enter)
        {
            var autoUpdater = ServiceCollections.ResolveAutoUpdater();
            var inputService = ServiceCollections.ResolveInputService();

            if (!NetplayPreferences.IsOfficialServer || autoUpdater.IsStatusFresh())
            {
                ResolveNetplayEntry(menu, autoUpdater, inputService, enter);
                return;
            }

            _pendingNetplayEntry = enter;
            menu.AddLoader("CHECKING VERSION", withFade: true);
            inputService.DisableAllControllers();

            Task.Run(async () =>
            {
                await autoUpdater.CheckForUpdate();
                _checkDone = true;
            });
        }

        private static void ResolveNetplayEntry(TowerFall.MainMenu menu, IAutoUpdater autoUpdater, IInputService inputService, Action enter)
        {
            if (menu == null)
            {
                return;
            }

            if (NetplayPreferences.IsOfficialServer && autoUpdater.GetStatus() == UpdateStatus.UpdateAvailable)
            {
                if (NetplayPreferences.AutoUpdate && !_updateFlowStarted)
                {
                    StartUpdate(menu, autoUpdater, inputService);
                    return;
                }

                Sounds.ui_invalid.Play();
                Notification.Create(menu, $"Online play requires version {autoUpdater.GetLatestVersion()} (current version : {autoUpdater.GetCurrentVersion()})", 10, 400);
                return;
            }

            WarnAboutDesyncRiskMods(menu);

            enter?.Invoke();
        }

        private static void WarnAboutDesyncRiskMods(TowerFall.MainMenu menu)
        {
            var riskyMods = ServiceCollections.ResolveModCollections()?.GetDesyncRiskMods()
                ?.Select(name => name.ToUpperInvariant())
                .OrderBy(name => name)
                .ToList();

            if (riskyMods == null || riskyMods.Count == 0)
            {
                return;
            }

            ServiceCollections.ResolveLogger().LogWarning($"Mods with desync risk in netplay: {string.Join(", ", riskyMods)}");

            var shown = 1;
            while (shown < riskyMods.Count && TFGame.Font.MeasureString(GetDesyncRiskModsMessage(riskyMods, shown + 1)).X <= MAX_NOTIFICATION_TEXT_WIDTH)
            {
                shown++;
            }

            Sounds.ui_invalid.Play();
            Notification.Create(menu, "NON-COSMETIC MODS CAN DESYNC NETPLAY", 10, 500);
            Notification.Create(menu, GetDesyncRiskModsMessage(riskyMods, shown), 10, 500);
        }

        private static string GetDesyncRiskModsMessage(List<string> riskyMods, int count)
        {
            var extra = count < riskyMods.Count ? $" +{riskyMods.Count - count} MORE" : "";

            return $"DETECTED: {string.Join(", ", riskyMods.Take(count))}{extra}";
        }

        private static void StartUpdate(TowerFall.MainMenu menu, IAutoUpdater autoUpdater, IInputService inputService)
        {
            _updateFlowStarted = true;

            var dialog = new UpdaterDialog($"NEW VERSION V{autoUpdater.GetLatestVersion()}");
            _updaterDialog = dialog;
            menu.Add(dialog);
            Sounds.ui_clickSpecial.Play(160, 5);
            inputService.DisableAllControllers();

            Task.Run(async () =>
            {
                var ok = await autoUpdater.DownloadAndApply(dialog.SetPhase, dialog.Report);

                if (ok)
                {
                    dialog.SetPhase("RESTARTING");
                    _restartWhenReady = true;
                }
                else
                {
                    _updateFailed = true;
                }
            });
        }

        public static bool CanRunNetplayFrames(Monocle.Scene scene, INetplayManager netplayManager)
        {
            return scene is Level
                && (netplayManager.IsSynchronized() || netplayManager.GetNetplayMode().Equals(NetplayMode.Test));
        }

        private static bool NetplayLogic(Level level, INetplayManager netplayManager, IInputService inputService, IReplayService replayService, ISyncTestUtilsService syncTestUtilsService)
        {
            if (!netplayManager.HaveRequestToHandle())
            {
                var playerInput = Domain.CustomComponent.LightPauseMenu.IsOpen
                    ? new Domain.Models.Input()
                    : inputService.GetPolledInput();

                var status = netplayManager.AdvanceFrame(playerInput);

                if (!status.IsOk)
                {
                    return false;
                }

                inputService.ResetPolledInput();
                netplayManager.UpdateNetplayRequests();
            }

            if (!netplayManager.HaveRequestToHandle())
            {
                return false;
            }

            while (netplayManager.HaveRequestToHandle() && !netplayManager.CanAdvanceFrame())
            {
                var request = netplayManager.ConsumeNetplayRequest();

                level = TFGame.Instance.Scene as Level; //We need to get the level again because it can be changed by Level Update (LevelLoader)

                switch (request)
                {
                    case NetplayRequest.SaveGameState:
                        ExFlags.CurrentFrame = GGRSFFI.netplay_current_frame();
                        StateApi.Current.SetCurrentFrame(ExFlags.CurrentFrame);

                        var captured = StateApi.Current.CaptureGameState();
                        ExFlags.LastCapturedState = captured;

                        netplayManager.SaveGameState(captured);

                        if (!netplayManager.IsReplayMode())
                        {
                            replayService.AddRecord(captured, ExFlags.CurrentFrame);
                        }

                        if (netplayManager.IsTestMode())
                        {
                            syncTestUtilsService.AddFrame(ExFlags.CurrentFrame, captured);
                        }

                        break;
                    case NetplayRequest.LoadGameState:
                        netplayManager.SetIsRollbackFrame(true);
                        var stateToLoad = netplayManager.LoadGameState();

                        netplayManager.SetIsUpdating(true);
                        int loadedFrame;
                        try
                        {
                            loadedFrame = StateApi.Current.RestoreGameStateBytes(stateToLoad);
                        }
                        finally
                        {
                            netplayManager.SetIsUpdating(false);
                        }
                        replayService.RemovePredictedRecords(loadedFrame);

                        if (netplayManager.IsTestMode())
                        {
                            syncTestUtilsService.Remove(loadedFrame);
                        }

                        break;
                    case NetplayRequest.AdvanceFrame:
                        netplayManager.AdvanceGameState();

                        DynamicData.For(TFGame.Instance.Scene).Set("FrameCounter", (float)GGRSFFI.netplay_current_frame());

                        _mInputUpdate.Invoke(null, null);
                        level.Update();
                        break;
                }
            }

            return true;
        }

        private static void ArtificialSlow()
        {
            Random random = new Random();

            if (random.Next(0, 9) > 1)
            {
                Console.WriteLine("Sleepy !");

                int delay = random.Next(100, 300);
                Thread.Sleep(delay);
            }
        }

        private static void UpdateClipped(Monocle.Commands commands)
        {
            if (!commands.Open)
            {
                return;
            }

            try
            {
                var dynCommands = DynamicData.For(commands);
                var currentText = dynCommands.Get<string>("currentText");

                var clipped = ClipboardService.GetText()?.Trim();

                if (!string.IsNullOrEmpty(clipped) && currentText != clipped)
                {
                    dynCommands.Set("currentText", clipped);
                    ClipboardService.SetText("");
                }
            }
            catch
            {
                //Cannot access clipboard because :/
            }
        }

        private static void ManageTimeStep(TowerFall.TFGame self)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var sessionActive = netplayManager.IsInit() || netplayManager.IsReplayMode() || netplayManager.IsTestMode();

            if (!sessionActive)
            {
                if (self.Scene is not Level)
                {
                    RestoreTimeStep(self);
                }

                return;
            }

            switch (self.Scene)
            {
                case Level _:
                case LevelLoaderXML _:
                    if (!self.IsFixedTimeStep)
                    {
                        _preSessionFixedStep ??= false;
                        self.IsFixedTimeStep = true;
                    }

                    var sessionFps = netplayManager.GetSessionFps();
                    if (!netplayManager.IsReplayMode() && sessionFps != Domain.Models.Constants.NETPLAY_FPS)
                    {
                        var ticks = (long)Math.Round(TimeSpan.TicksPerSecond / (double)sessionFps);
                        if (self.TargetElapsedTime.Ticks != ticks)
                        {
                            self.TargetElapsedTime = TimeSpan.FromTicks(ticks);
                        }
                    }
                    break;
                case TowerFall.MainMenu _:
                    RestoreTimeStep(self);
                    break;
            }
        }

        private static void RestoreTimeStep(TowerFall.TFGame self)
        {
            if (_preSessionFixedStep is bool previous)
            {
                _preSessionFixedStep = null;
                self.IsFixedTimeStep = previous;
            }
        }

    }

}
