using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Reflection;
using TF.EX.Domain;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Scene;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Engine
{
    public class TFGamePatch : IHookable
    {
        public static bool HasExported = false;
        public static InputRenderer[] ReplayInputRenderers = new InputRenderer[4];

        public bool HasStarted = false;
        private bool IsFirstUpdate = true;

        private readonly INetplayManager _netplayManager;
        private readonly IInputService _inputService;
        private readonly ITFActionService _actionServices;
        private readonly IReplayService _replayService;
        private LevelPatch _levelPatch;

        private DateTime LastUpdate;
        private TimeSpan Accumulator;

        private bool loaded = false;
        private const double FPS = 60;
        private const double SLOW_RATIO = 1.1;
        private readonly MethodInfo _mInputUpdate = typeof(MInput).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Static); //Minput Update is an internal static method...

        public TFGamePatch(INetplayManager netplayManager, IInputService inputService, ISessionService sessionService, ITFActionService actionServices, IReplayService replayService)
        {
            _netplayManager = netplayManager;
            _inputService = inputService;
            _actionServices = actionServices;
            _replayService = replayService;
        }

        public void Load()
        {
            On.TowerFall.TFGame.Update += TFGameUpdate_Patch;

        }

        public void Unload()
        {
            On.TowerFall.TFGame.Update -= TFGameUpdate_Patch;
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            if (ServiceCollections.ResolveNetplayManager().IsInit() && !TFGamePatch.HasExported)
            {
                ServiceCollections.ResolveReplayService().Export();
            }
        }


        private void CheckAndStartTest(bool gameLoaded)
        {
            if (_netplayManager.IsTestMode() && !loaded && gameLoaded)
            {
                var rngService = ServiceCollections.ResolveRngService();
                rngService.SetSeed(42);
                loaded = true;
                _netplayManager.Init();
                _replayService.Initialize();
                _actionServices.StartTest(TowerFall.Modes.LastManStanding);
                //Commands.ExecuteCommand("test", null);
            }
        }

        private void TFGameUpdate_Patch(On.TowerFall.TFGame.orig_Update orig, TowerFall.TFGame self, GameTime gameTime)
        {
            var dynTFGame = DynamicData.For(self);

            if (IsFirstUpdate)
            {
                IsFirstUpdate = false;
                self.IsFixedTimeStep = true;

                LastUpdate = DateTime.Now;
                Accumulator = TimeSpan.Zero;

                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            }

            // if (Engine.Instance != null && Instance.Scene != null && Instance.Scene is MainMenu && (Instance.Scene as MainMenuHook).PublicState == MainMenu.MenuState.Main)
            // {
            var gameLoaded = dynTFGame.Get<bool>("GameLoaded");
            CheckAndStartTest(gameLoaded);
            // }

            if (!HasStarted)
            {
                LastUpdate = DateTime.Now;
                Accumulator = TimeSpan.Zero;
                HasStarted = self.Scene != null && self.Scene[GameTags.Player] != null && (self.Scene[GameTags.Player].Count > 0);
            }

            if (_netplayManager.IsReplayMode() && !loaded && gameLoaded)
            {
                loaded = true;

                string[] arguments = Environment.GetCommandLineArgs();

                _replayService.LoadAndStart(arguments[2]);
                SetupReplayInputRenderer();
            }

            if (gameLoaded && !loaded)
            {
                loaded = true;
            }

            if (_netplayManager.IsDisconnected() && !HasExported)
            {
                _replayService.Export();
                HasExported = true;
            }

            if (_netplayManager.IsReplayMode())
            {
                if (loaded && gameLoaded && CanRunFrames(self.Scene) && !(self.Scene as Level).Paused)
                {
                    _replayService.RunFrame();
                }
                orig(self, gameTime);
            }
            else
            {
                if (_netplayManager.IsInit() && HasStarted && CanRunFrames(self.Scene))
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
                                var canAdvance = NetplayLogic(self.Scene as TowerFall.Level);

                                if (canAdvance)
                                {
                                    if (_netplayManager.CanAdvanceFrame())
                                    {
                                        _netplayManager.ConsumeNetplayRequest(); //We should had one last advance Frame request to consume
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
                                //Console.WriteLine($"Level loader / not syncrhonized");
                            }
                        }
                    }
                }
                else
                {
                    if (_netplayManager.IsInit())
                    {
                        _netplayManager.Poll();
                    }
                    orig(self, gameTime);
                }
            }
        }

        /// <summary>
        /// We run Netplay logic only if we can actually move the player (aka not flashing and not frozen)
        /// </summary>
        /// <returns></returns>
        public bool CanRunFrames(Monocle.Scene scene)
        {
            return scene is TowerFall.Level;
        }

        private bool NetplayLogic(TowerFall.Level level)
        {
            if (_levelPatch == null)
            {
                _levelPatch = ServiceCollections.ServiceProvider.GetLevelPatch();
            }

            var canAdvance = true;
            if (!_netplayManager.HaveRequestToHandle())
            {
                var playerInput = _inputService.GetPolledInput();

                var input = playerInput.ToModel();

                var status = _netplayManager.AdvanceFrame(input);

                if (status.IsOk)
                {
                    _inputService.ResetPolledInput();
                    _netplayManager.UpdateNetplayRequests();
                }
                else
                {
                    canAdvance = false;
                }
            }

            while (_netplayManager.HaveRequestToHandle() && !_netplayManager.CanAdvanceFrame())
            {
                var request = _netplayManager.ConsumeNetplayRequest();

                switch (request)
                {
                    case NetplayRequest.SaveGameState:
                        var gameState = LevelPatch._isLoaded ? _levelPatch.GetState(level) : new GameState();

                        _netplayManager.SaveGameState(gameState);
                        _replayService.AddRecord(gameState, _netplayManager.ShouldSwapPlayer());
                        break;
                    case NetplayRequest.LoadGameState:
                        _netplayManager.SetIsRollbackFrame(true);
                        var gameStateToLoad = _netplayManager.LoadGameState();

                        _netplayManager.SetIsUpdating(true);
                        _levelPatch.LoadState(gameStateToLoad, level);
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

            return canAdvance;
        }

        private bool IsLocalPlayerFrozen(Monocle.Scene scene) //TODO: remove this
        {
            if (scene == null) //TODO: Check if this is still needed since scene should be a level
            {
                return false;
            }

            var players = scene[GameTags.Player];
            if (players != null && players.Count > 0)
            {
                var localPlayer = (TowerFall.Player)scene[GameTags.Player][0];

                if (localPlayer != null && localPlayer.State.Equals(TowerFall.Player.PlayerStates.Frozen))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetupReplayInputRenderer()
        {
            var replay = _replayService.GetReplay();

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
    }

}
