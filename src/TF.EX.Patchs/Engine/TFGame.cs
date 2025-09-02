using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
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
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Calc;
using TF.EX.TowerFallExtensions;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.Patchs.Engine
{
    [HarmonyPatch(typeof(TowerFall.TFGame))]
    public class TFGamePatch
    {
        public static InputRenderer[] CustomInputRenderers;

        private static bool _shouldShowUpdateNotif = true;

        private static DateTime LastUpdate;
        private static TimeSpan Accumulator;

        private const double FPS = 60;
        private const double SLOW_RATIO = 1.1;
        private static readonly MethodInfo _mInputUpdate = AccessTools.Method(typeof(MInput), "Update"); //Minput Update is an internal static method...

        private static bool frameByFrame { get; set; } = false;

        [HarmonyPrefix]
        [HarmonyPatch("OnExiting")]
        public static void TFGame_OnExiting(TFGame __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();
            var autoUpdater = ServiceCollections.ResolveAutoUpdater();

            if (__instance.Scene is Level && netplayManager.IsServerMode())
            {
                replayService.Export();
            }

            if (autoUpdater.IsUpdateAvailable())
            {
                //TODO: logger
                //if (!FortRise.RiseCore.DebugMode)
                //{
                //    FortRise.Logger.AttachConsole(new FortRise.WindowConsole());
                //}

                var logger = ServiceCollections.ResolveLogger();
                var stopWatch = new Stopwatch();

                stopWatch.Start();
                logger.LogDebug("TF.EX mod updating...");
                autoUpdater.Update();
                stopWatch.Stop();

                var time = stopWatch.ElapsedMilliseconds / 1000 == 0 ? $"{stopWatch.ElapsedMilliseconds}ms" : $"{stopWatch.ElapsedMilliseconds / 1000}s";

                logger.LogDebug($"TF.EX mod updated in {time}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnSceneTransition")]
        public static void TFGame_OnSceneTransition(TFGame __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (__instance.PreviousScene is Level && __instance.Scene is TowerFall.MainMenu)
            {
                CalcPatch.Reset();

                CustomInputRenderers = null;

                if (!netplayManager.IsReplayMode())
                {
                    ServiceCollections.PurgeCache();
                }

                if (!netplayManager.IsTestMode())
                {
                    netplayManager.Reset();
                    //TowerFall.PlayerInput.AssignInputs(); //Reset inputs
                }

                netplayManager.ResetMode();

                TFGameExtensions.ResetVersusChoices();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Load")]
        public static void TFGame_Load()
        {
            LastUpdate = DateTime.Now;
            Accumulator = TimeSpan.Zero;

            if (Config.SERVER.Contains(".scw.cloud")) //TODO: find a better way to detect local / production
            {
                Task.Run(() => AutoUpdateIfNeeded()).GetAwaiter().GetResult();
            }
        }

        private static async Task AutoUpdateIfNeeded()
        {
            var autoUpdate = ServiceCollections.ServiceProvider.GetRequiredService<IAutoUpdater>();
            await autoUpdate.CheckForUpdate();
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

                if (gameLoaded && __instance.Scene is Level && !(__instance.Scene as Level).Paused)
                {
                    replayService.RunFrame();
                    //(self.Scene as Level).LoadState(_replayService.GetCurrentRecord().GameState);
                }
                TFGame_Update_orig(__instance, gameTime);
                //self.Screen.Offset = Vector2.Zero; //Ignore camera offset on replay mode (used by some orb)

                return false;
            }

            if (!netplayManager.IsSynchronized() && netplayManager.GetNetplayMode() != NetplayMode.Test)
            {
                LastUpdate = DateTime.Now;
                Accumulator = TimeSpan.Zero;
            }

            if (!CanRunNetplayFrames(__instance.Scene) || (!netplayManager.IsInit() && (__instance.Scene as Level).Session.RoundLogic is LastManStandingRoundLogic))
            {
                TFGame_Update_orig(__instance, gameTime);
                return false;
            }

            if (netplayManager.IsDisconnected())
            {
                TFGame_Update_orig(__instance, gameTime);
                return false;
            }

            if (netplayManager.HasFailedInitialConnection())
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
                //ArtificialSlow(); //Only useful to test choppy/freezing condition

                double fpsDelta = 1.0 / FPS;

                if (netplayManager.IsFramesAhead())
                {
                    fpsDelta *= SLOW_RATIO;
                }

                var delta = DateTime.Now - LastUpdate;
                Accumulator = Accumulator.Add(delta);
                LastUpdate = DateTime.Now;

                while (Accumulator.TotalSeconds > fpsDelta)
                {
                    Accumulator = Accumulator.Subtract(TimeSpan.FromSeconds(fpsDelta));

                    if (netplayManager.IsSynchronized() || netplayManager.GetNetplayMode().Equals(NetplayMode.Test))
                    {
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
            var scene = instance.Scene as TowerFall.MainMenu;
            switch (scene.State)
            {
                case TowerFall.MainMenu.MenuState.PressStart:
                    UpdateClipped(instance.Commands);

                    if (_shouldShowUpdateNotif)
                    {
                        _shouldShowUpdateNotif = false;

                        if (autoUpdater.IsUpdateAvailable())
                        {
                            var notif = Notification.Create(instance.Scene, $"EX mod new update! Applying version {autoUpdater.GetLatestVersion()} ...", stayingDuration: 500);
                            Sounds.ui_clickSpecial.Play(160, 5);
                            inputService.DisableAllControllers();

                            Alarm alarm = Alarm.Create(Alarm.AlarmMode.Oneshot, null, 550, true);
                            alarm.OnComplete = instance.Exit;

                            notif.Add(alarm);

                            instance.Scene.Add(notif);
                        }
                    }

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

        public static bool CanRunNetplayFrames(Monocle.Scene scene)
        {
            return scene is Level;
        }

        private static bool NetplayLogic(Level level, INetplayManager netplayManager, IInputService inputService, IReplayService replayService, ISyncTestUtilsService syncTestUtilsService)
        {
            if (!netplayManager.HaveRequestToHandle())
            {
                var playerInput = inputService.GetPolledInput();

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
                        var gameState = level.GetState();

                        netplayManager.SaveGameState(gameState);

                        if (!netplayManager.IsReplayMode())
                        {
                            replayService.AddRecord(gameState, netplayManager.ShouldSwapPlayer());
                        }

                        if (netplayManager.IsTestMode())
                        {
                            syncTestUtilsService.AddFrame(gameState.Frame, gameState);
                        }

                        break;
                    case NetplayRequest.LoadGameState:
                        netplayManager.SetIsRollbackFrame(true);
                        var gameStateToLoad = netplayManager.LoadGameState();

                        netplayManager.SetIsUpdating(true);
                        level.LoadState(gameStateToLoad);
                        netplayManager.SetIsUpdating(false);
                        replayService.RemovePredictedRecords(gameStateToLoad.Frame);

                        if (netplayManager.IsTestMode())
                        {
                            syncTestUtilsService.Remove(gameStateToLoad.Frame);
                        }

                        break;
                    case NetplayRequest.AdvanceFrame:
                        netplayManager.AdvanceGameState();

                        _mInputUpdate.Invoke(null, null);
                        level.Update();
                        break;
                }
            }

            return true;
        }

        public static void SetupCustomInputRenderer(int numberInputs)
        {
            CustomInputRenderers = new InputRenderer[numberInputs];

            float num = 0f;
            for (int i = 0; i < numberInputs; i++)
            {
                CustomInputRenderers[i] = new InputRenderer(i, num);
                num += CustomInputRenderers[i].Width;
            }
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
            switch (self.Scene)
            {
                case Level _:
                case LevelLoaderXML _:
                    if (!self.IsFixedTimeStep)
                    {
                        self.IsFixedTimeStep = true;
                        return;
                    }
                    break;
                case TowerFall.MainMenu _:
                    if (self.IsFixedTimeStep)
                    {
                        self.IsFixedTimeStep = false;
                        return;
                    }
                    return;
            }
        }
    }

}
