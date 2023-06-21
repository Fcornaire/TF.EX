using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Reflection;
using System.Windows.Forms;
using TF.EX.Common;
using TF.EX.Domain;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.Patchs.Engine
{
    public class TFGamePatch : IHookable
    {
        public static bool HasExported = false;
        public static InputRenderer[] ReplayInputRenderers = new InputRenderer[4];

        private bool IsFirstUpdate = true;
        private bool _shouldShowUpdateDialog = true;

        private readonly INetplayManager _netplayManager;
        private readonly IInputService _inputService;
        private readonly IReplayService _replayService;

        private DateTime LastUpdate;
        private TimeSpan Accumulator;

        private const double FPS = 60;
        private const double SLOW_RATIO = 1.1;
        private readonly MethodInfo _mInputUpdate = typeof(MInput).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Static); //Minput Update is an internal static method...

        public TFGamePatch(INetplayManager netplayManager, IInputService inputService, ISessionService sessionService, IReplayService replayService)
        {
            _netplayManager = netplayManager;
            _inputService = inputService;
            _replayService = replayService;
        }

        public void Load()
        {
            On.TowerFall.TFGame.Update += TFGameUpdate_Patch;
            On.TowerFall.TFGame.Load += TFGame_Load;
        }



        public void Unload()
        {
            On.TowerFall.TFGame.Update -= TFGameUpdate_Patch;
            On.TowerFall.TFGame.Load -= TFGame_Load;
        }

        private void TFGame_Load(On.TowerFall.TFGame.orig_Load orig)
        {
            orig();

            Task.Run(() => AutoUpdateIfNeeded()).GetAwaiter().GetResult();
        }

        private async Task AutoUpdateIfNeeded()
        {
            var autoUpdate = ServiceCollections.ServiceProvider.GetRequiredService<IAutoUpdater>();
            await autoUpdate.CheckForUpdate();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            if (ServiceCollections.ResolveNetplayManager().IsInit() && !TFGamePatch.HasExported)
            {
                ServiceCollections.ResolveReplayService().Export();
            }

            var autoUpdater = ServiceCollections.ServiceProvider.GetRequiredService<IAutoUpdater>();
            if (autoUpdater.IsUpdateAvailable())
            {
                autoUpdater.Update();
            }
        }

        private void TFGameUpdate_Patch(On.TowerFall.TFGame.orig_Update orig, TowerFall.TFGame self, GameTime gameTime)
        {
            var dynTFGame = DynamicData.For(self);


            if (self.Scene is TowerFall.MainMenu && (self.Scene as TowerFall.MainMenu).State == TowerFall.MainMenu.MenuState.PressStart)
            {
                UpdateClipped(self.Commands);
            }

            if (IsFirstUpdate)
            {
                IsFirstUpdate = false;
                self.IsFixedTimeStep = true;

                LastUpdate = DateTime.Now;
                Accumulator = TimeSpan.Zero;

                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            }

            if (self.Scene is TowerFall.MainMenu && (self.Scene as TowerFall.MainMenu).State == TowerFall.MainMenu.MenuState.Main && _shouldShowUpdateDialog)
            {
                _shouldShowUpdateDialog = false;

                if (ServiceCollections.ServiceProvider.GetRequiredService<IAutoUpdater>().IsUpdateAvailable())
                {
                    var dialog = new Dialog("TF EX Update", "A TF EX mod update is available \n \nPlease close and restart the game", new Vector2(160f, 120f), () => { Environment.Exit(0); }, new Dictionary<string, Action>());
                    var dynLayer = DynamicData.For((TFGame.Instance.Scene as TowerFall.MainMenu).GetMainLayer());
                    dynLayer.Invoke("Add", dialog, false);
                }
            }

            var gameLoaded = dynTFGame.Get<bool>("GameLoaded");

            if (_netplayManager.IsDisconnected() && !HasExported)
            {
                _replayService.Export();
                HasExported = true;
            }

            if (!CanRunNetplayFrames(self.Scene) || (!_netplayManager.IsInit() && (self.Scene as Level).Session.RoundLogic is LastManStandingRoundLogic))
            {
                if (!_netplayManager.IsInit())
                {
                    LastUpdate = DateTime.Now;
                    Accumulator = TimeSpan.Zero;
                }

                orig(self, gameTime);
                return;
            }

            if (_netplayManager.IsReplayMode())
            {
                if (gameLoaded && !(self.Scene as Level).Paused)
                {
                    _replayService.RunFrame();
                }
                orig(self, gameTime);
                return;
            }

            if (_netplayManager.IsInit())
            {
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
                            Console.WriteLine($"Not syncrhonized {GGRSFFI.netplay_current_frame()}");
                        }
                    }
                }
            }
            else
            {
                //TODO: Make it work with the menu
                if (_netplayManager.IsDisconnected())
                {
                    orig(self, gameTime);
                }
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
                        _replayService.AddRecord(gameState, _netplayManager.ShouldSwapPlayer());
                        break;
                    case NetplayRequest.LoadGameState:
                        _netplayManager.SetIsRollbackFrame(true);
                        var gameStateToLoad = _netplayManager.LoadGameState();

                        _netplayManager.SetIsUpdating(true);
                        level.LoadState(gameStateToLoad);
                        _netplayManager.SetIsUpdating(false);
                        _replayService.RemovePredictedRecords(gameStateToLoad.Frame);
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

        public static void SetupReplayInputRenderer()
        {
            var replayService = ServiceCollections.ResolveReplayService();
            var replay = replayService.GetReplay();

            if (replay.Record.ToArray().Length > 0)
            {
                float num = 0f;
                for (int i = 0; i < replay.Record[0].Inputs.Count; i++)
                {
                    ReplayInputRenderers[i] = new InputRenderer(i, num);
                    num += (float)ReplayInputRenderers[i].Width;
                }
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
                dynCommands.Set("currentText", $"{currentText} {clipped}");
                Clipboard.Clear();
            }
        }
    }

}
