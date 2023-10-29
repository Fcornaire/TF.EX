using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
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
    public class TFGamePatch : IHookable
    {
        public static InputRenderer[] CustomInputRenderers;

        private bool _shouldShowUpdateNotif = true;

        private readonly INetplayManager _netplayManager;
        private readonly IInputService _inputService;
        private readonly IReplayService _replayService;
        private readonly IMatchmakingService _matchmakingService;
        private readonly IAutoUpdater autoUpdater;
        private readonly ISyncTestUtilsService _syncTestUtilsService;
        private readonly ILogger _logger;

        private DateTime LastUpdate;
        private TimeSpan Accumulator;

        private const double FPS = 60;
        private const double SLOW_RATIO = 1.1;
        private readonly MethodInfo _mInputUpdate = typeof(MInput).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Static); //Minput Update is an internal static method...

        private bool frameByFrame { get; set; } = false;

        public TFGamePatch(
            INetplayManager netplayManager,
            IInputService inputService,
            IReplayService replayService,
            IMatchmakingService matchmakingService,
            IAutoUpdater autoUpdater,
            ISyncTestUtilsService syncTestUtilsService,
            ILogger logger
            )
        {
            _netplayManager = netplayManager;
            _inputService = inputService;
            _replayService = replayService;
            _matchmakingService = matchmakingService;
            this.autoUpdater = autoUpdater;
            _logger = logger;
            _syncTestUtilsService = syncTestUtilsService;
        }

        public void Load()
        {
            On.TowerFall.TFGame.Update += TFGameUpdate_Patch;
            On.TowerFall.TFGame.Load += TFGame_Load;
            On.TowerFall.TFGame.OnSceneTransition += TFGame_OnSceneTransition;
            On.TowerFall.TFGame.OnExiting += TFGame_OnExiting;
        }

        public void Unload()
        {
            On.TowerFall.TFGame.Update -= TFGameUpdate_Patch;
            On.TowerFall.TFGame.Load -= TFGame_Load;
            On.TowerFall.TFGame.OnSceneTransition -= TFGame_OnSceneTransition;
            On.TowerFall.TFGame.OnExiting -= TFGame_OnExiting;
        }

        private void TFGame_OnExiting(On.TowerFall.TFGame.orig_OnExiting orig, TFGame self, object sender, EventArgs args)
        {
            if (self.Scene is Level && _netplayManager.IsServerMode())
            {
                _replayService.Export();
            }

            if (autoUpdater.IsUpdateAvailable())
            {
                if (!FortRise.RiseCore.DebugMode)
                {
                    FortRise.Logger.AttachConsole(new FortRise.WindowConsole());
                }

                var logger = ServiceCollections.ResolveLogger();
                var stopWatch = new Stopwatch();

                stopWatch.Start();
                logger.LogDebug("TF.EX mod updating...");
                autoUpdater.Update();
                stopWatch.Stop();

                var time = stopWatch.ElapsedMilliseconds / 1000 == 0 ? $"{stopWatch.ElapsedMilliseconds}ms" : $"{stopWatch.ElapsedMilliseconds / 1000}s";

                logger.LogDebug($"TF.EX mod updated in {time}");
            }

            orig(self, sender, args);
        }

        private void TFGame_OnSceneTransition(On.TowerFall.TFGame.orig_OnSceneTransition orig, TFGame self)
        {
            orig(self);

            if (self.PreviousScene is Level && self.Scene is TowerFall.MainMenu)
            {
                CalcPatch.Reset();

                CustomInputRenderers = null;

                if (!_netplayManager.IsReplayMode())
                {
                    ServiceCollections.PurgeCache();
                }

                if (!_netplayManager.IsTestMode())
                {
                    _netplayManager.Reset();
                    //TowerFall.PlayerInput.AssignInputs(); //Reset inputs
                }

                _netplayManager.ResetMode();

                TFGameExtensions.ResetVersusChoices();
            }
        }

        private void TFGame_Load(On.TowerFall.TFGame.orig_Load orig)
        {
            orig();

            LastUpdate = DateTime.Now;
            Accumulator = TimeSpan.Zero;

            if (Config.SERVER.Contains(".com")) //TODO: find a better way to detect local / production
            {
                Task.Run(() => AutoUpdateIfNeeded()).GetAwaiter().GetResult();
            }
        }

        private async Task AutoUpdateIfNeeded()
        {
            var autoUpdate = ServiceCollections.ServiceProvider.GetRequiredService<IAutoUpdater>();
            await autoUpdate.CheckForUpdate();
        }

        private void TFGameUpdate_Patch(On.TowerFall.TFGame.orig_Update orig, TowerFall.TFGame self, GameTime gameTime)
        {
            if (self.Scene is TowerFall.MainMenu)
            {
                HandleMenuAction(self);
            }

            ManageTimeStep(self);

            if (!HandleFrameByFrame())
            {
                return;
            }

            if (_netplayManager.IsReplayMode())
            {
                var dynTFGame = DynamicData.For(self);
                var gameLoaded = dynTFGame.Get<bool>("GameLoaded");

                if (gameLoaded && self.Scene is Level && !(self.Scene as Level).Paused)
                {
                    _replayService.RunFrame();
                }
                orig(self, gameTime);
                //self.Screen.Offset = Vector2.Zero; //Ignore camera offset on replay mode (used by some orb)

                return;
            }

            if (!_netplayManager.IsSynchronized() && _netplayManager.GetNetplayMode() != NetplayMode.Test)
            {
                LastUpdate = DateTime.Now;
                Accumulator = TimeSpan.Zero;
            }

            if (!CanRunNetplayFrames(self.Scene) || (!_netplayManager.IsInit() && (self.Scene as Level).Session.RoundLogic is LastManStandingRoundLogic))
            {
                orig(self, gameTime);
                return;
            }

            if (_netplayManager.IsDisconnected())
            {
                if (self.Scene is Level)
                {
                    var dialog = (self.Scene as Level).Get<Dialog>();
                    if (dialog != null)
                    {
                        (self.Scene as Level).Delete<Dialog>(); //Fix
                    }
                }

                orig(self, gameTime);
                return;
            }

            if (_netplayManager.HasFailedInitialConnection())
            {
                orig(self, gameTime);
                return;
            }


            if (!_netplayManager.IsSynchronized() && !_netplayManager.IsInit())
            {
                if (TFGame.Instance.Scene is TowerFall.Level && (TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() != -1)
                {
                    var vs = (TFGame.Instance.Scene as TowerFall.Level).Get<VersusMatchResults>();
                    if (vs != null)
                    {
                        orig(self, gameTime);
                    }
                }

                return;
            }

            if (!_netplayManager.GetNetplayMode().Equals(NetplayMode.Test))
            {
                _netplayManager.Poll();
            }

            if (!_netplayManager.IsDisconnected())
            {
                //ArtificialSlow(); //Only useful to test choppy/freezing condition

                double fpsDelta = 1.0 / FPS;

                if (_netplayManager.IsFramesAhead())
                {
                    fpsDelta *= SLOW_RATIO;
                }

                var delta = DateTime.Now - LastUpdate;
                Accumulator = Accumulator.Add(delta);
                LastUpdate = DateTime.Now;

                while (Accumulator.TotalSeconds > fpsDelta)
                {
                    Accumulator = Accumulator.Subtract(TimeSpan.FromSeconds(fpsDelta));

                    if (_netplayManager.IsSynchronized() || _netplayManager.GetNetplayMode().Equals(NetplayMode.Test))
                    {
                        var canAdvance = NetplayLogic(self.Scene as Level);

                        if (canAdvance)
                        {
                            if (_netplayManager.CanAdvanceFrame())
                            {
                                _netplayManager.ConsumeNetplayRequest(); //We should had one last advance Frame request to consume if no rollback
                            }

                            if (!_netplayManager.HaveRequestToHandle())
                            {
                                _netplayManager.UpdateFramesToReSimulate(0);
                            }

                            _netplayManager.AdvanceGameState();
                            var dynScene = DynamicData.For(self.Scene);
                            dynScene.Set("FrameCounter", GGRSFFI.netplay_current_frame());

                            orig(self, gameTime);
                        }
                    }
                    else
                    {
                        _logger.LogDebug<TFGame>($"Not syncrhonized {GGRSFFI.netplay_current_frame()}");
                    }
                }
            }

        }

        private bool HandleFrameByFrame()
        {
            if (!_netplayManager.IsReplayMode())
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

        private void HandleMenuAction(TFGame self)
        {
            var scene = self.Scene as TowerFall.MainMenu;
            switch (scene.State)
            {
                case TowerFall.MainMenu.MenuState.PressStart:
                    UpdateClipped(self.Commands);

                    if (_shouldShowUpdateNotif)
                    {
                        _shouldShowUpdateNotif = false;

                        if (autoUpdater.IsUpdateAvailable())
                        {
                            var notif = Notification.Create(self.Scene, $"EX mod update version {autoUpdater.GetLatestVersion()} ...");
                            Sounds.ui_clickSpecial.Play(160, 5);
                            _inputService.DisableAllControllers();

                            Alarm alarm = Alarm.Create(Alarm.AlarmMode.Oneshot, null, 150, true);
                            alarm.OnComplete = self.Exit;

                            notif.Add(alarm);

                            self.Scene.Add(notif);
                        }
                    }

                    break;
                case TowerFall.MainMenu.MenuState.VersusOptions:
                    var dynCommands = DynamicData.For(self.Commands);
                    dynCommands.Set("currentText", string.Empty);
                    break;
                case TowerFall.MainMenu.MenuState.Main:
                default:
                    break;
            }
        }

        public bool CanRunNetplayFrames(Monocle.Scene scene)
        {
            return scene is Level;
        }

        private bool NetplayLogic(Level level)
        {
            if (!_netplayManager.HaveRequestToHandle())
            {
                var playerInput = _inputService.GetPolledInput();

                var input = playerInput.ToModel();

                var status = _netplayManager.AdvanceFrame(input);

                if (!status.IsOk)
                {
                    return false;
                }

                _inputService.ResetPolledInput();
                _netplayManager.UpdateNetplayRequests();
            }

            if (!_netplayManager.HaveRequestToHandle())
            {
                return false;
            }

            while (_netplayManager.HaveRequestToHandle() && !_netplayManager.CanAdvanceFrame())
            {
                var request = _netplayManager.ConsumeNetplayRequest();

                level = TFGame.Instance.Scene as Level; //We need to get the level again because it can be changed by Level Update (LevelLoader)

                switch (request)
                {
                    case NetplayRequest.SaveGameState:
                        var gameState = level.GetState();

                        _netplayManager.SaveGameState(gameState);

                        if (!_netplayManager.IsReplayMode())
                        {
                            _replayService.AddRecord(gameState, _netplayManager.ShouldSwapPlayer());
                        }

                        if (_netplayManager.IsTestMode())
                        {
                            _syncTestUtilsService.AddFrame(gameState.Frame, gameState);
                        }

                        break;
                    case NetplayRequest.LoadGameState:
                        _netplayManager.SetIsRollbackFrame(true);
                        var gameStateToLoad = _netplayManager.LoadGameState();

                        _netplayManager.SetIsUpdating(true);
                        level.LoadState(gameStateToLoad);
                        _netplayManager.SetIsUpdating(false);
                        _replayService.RemovePredictedRecords(gameStateToLoad.Frame);

                        if (_netplayManager.IsTestMode())
                        {
                            _syncTestUtilsService.Remove(gameStateToLoad.Frame);
                        }

                        break;
                    case NetplayRequest.AdvanceFrame:
                        _netplayManager.AdvanceGameState();

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

        private void ArtificialSlow()
        {
            Random random = new Random();

            if (random.Next(0, 9) > 1)
            {
                Console.WriteLine("Sleepy !");

                int delay = random.Next(200, 500);
                Thread.Sleep(delay);
            }
        }

        private void UpdateClipped(Monocle.Commands commands)
        {
            var dynCommands = DynamicData.For(commands);
            var currentText = dynCommands.Get<string>("currentText");

            var clipped = Clipboard.GetText().Trim();

            if (!string.IsNullOrEmpty(clipped) && currentText != clipped)
            {
                dynCommands.Set("currentText", clipped);
                Clipboard.Clear();
            }
        }

        private void ManageTimeStep(TowerFall.TFGame self)
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
