using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    public class MainMenuPatch : IHookable
    {
        private readonly IMatchmakingService _matchmakingService;
        private readonly INetplayManager _netplayManager;
        private readonly IReplayService _replayService;
        private readonly IInputService _inputService;
        private readonly ILogger _logger;

        private OptionsButton _netplayName;

        private List<ReplayInfos> replays = new List<ReplayInfos>();
        private ReplaysPanel _replaysPanel = null;
        private Text _noReplayTitle;

        public MainMenuPatch(IMatchmakingService matchmakingService,
            INetplayManager netplayManager,
            IReplayService replayService,
            IInputService inputService,
            ILogger logger)
        {
            _matchmakingService = matchmakingService;
            _netplayManager = netplayManager;
            _replayService = replayService;
            _inputService = inputService;
            _logger = logger;
        }

        public void Load()
        {
            On.TowerFall.MainMenu.Update += MainMenu_Update;
            On.TowerFall.MainMenu.InitOptions += MainMenu_InitOptions;
            On.TowerFall.MainMenu.Render += MainMenu_Render;
            On.TowerFall.MainMenu.ctor += MainMenu_ctor;
            On.TowerFall.MainMenu.CreateMain += MainMenu_CreateMain;
            On.TowerFall.MainMenu.CallStateFunc += MainMenu_CallStateFunc;
        }

        public void Unload()
        {
            On.TowerFall.MainMenu.Update -= MainMenu_Update;
            On.TowerFall.MainMenu.InitOptions -= MainMenu_InitOptions;
            On.TowerFall.MainMenu.Render -= MainMenu_Render;
            On.TowerFall.MainMenu.ctor -= MainMenu_ctor;
            On.TowerFall.MainMenu.CreateMain -= MainMenu_CreateMain;
            On.TowerFall.MainMenu.CallStateFunc -= MainMenu_CallStateFunc;
        }

        private void MainMenu_CallStateFunc(On.TowerFall.MainMenu.orig_CallStateFunc orig, MainMenu self, string name, MainMenu.MenuState state)
        {
            switch (state)
            {
                case (MainMenu.MenuState)18:
                    if (name == "Destroy")
                    {
                        self.DeleteAll<ReplayInfos>();

                        if (_replaysPanel != null)
                        {
                            _replaysPanel.RemoveSelf();
                        }

                        if (replays != null)
                        {
                            replays.Clear();
                        }

                        if (_noReplayTitle != null)
                        {
                            _noReplayTitle.RemoveSelf();
                            _noReplayTitle = null;
                        }
                    }
                    else
                    {
                        CreateReplay(self);
                    }
                    break;
                default:
                    orig(self, name, state);
                    break;
            }
        }

        private void CreateReplay(MainMenu self)
        {
            _inputService.DisableAllController();

            var loader = new Loader(true);
            Loader.Message = "LOADING REPLAYS...";
            self.Add(loader);

            Task.Run(() =>
            {
                try
                {
                    var dynMainMenu = DynamicData.For(self);
                    var dynCamera = DynamicData.For(self.UILayer);

                    var maxY = 50.0f;

                    var replays = _replayService.LoadAndGetReplays().ToArray();

                    self.BackState = TowerFall.MainMenu.MenuState.Main;

                    if (replays.Length == 0)
                    {
                        _logger.LogError<MainMenuPatch>("No replay found");
                        _inputService.EnableAllController();
                        self.Remove(loader);

                        _noReplayTitle = new Text("PLAY SOME ONLINE GAME FIRST");
                        _noReplayTitle.Position = new Vector2(100, 100);
                        self.Add(_noReplayTitle);

                        return;
                    }

                    _replaysPanel = new ReplaysPanel(225, 110);

                    for (int i = 0; i < replays.Length; i++)
                    {
                        if (replays[i] == null)
                        {
                            _logger.LogError<MainMenuPatch>("Replay is null ?");

                            continue;
                        }

                        var but = new ReplayInfos(self, new Vector2(5.0f, maxY), replays[i], LaunchRecord, _replaysPanel);

                        this.replays.Add(but);
                        if (i != 0)
                        {
                            this.replays[i - 1].DownItem = but;
                            but.UpItem = this.replays[i - 1];
                        }

                        if (i == 0)
                        {
                            dynMainMenu.Set("ToStartSelected", but);
                            but.Selected = true;
                        }

                        dynCamera.Invoke("Add", but, false);

                        maxY += 35.0f;
                    }

                    self.MaxUICameraY = maxY;

                    dynCamera.Invoke("Add", _replaysPanel, false);

                    self.BackState = TowerFall.MainMenu.MenuState.Main;

                    dynMainMenu.Invoke("TweenBGCameraToY", 1);

                    _inputService.EnableAllController();

                    self.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "LAUNCH");
                    self.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "EXIT");
                    Loader.Message = "";
                    self.Remove(loader);

                    Sounds.ui_startGame.Play();
                }
                catch (Exception ex)
                {
                    _logger.LogError<MainMenuPatch>("Error when creating replay menu", ex);
                }
            });
        }

        private void LaunchRecord()
        {
            ReplayInfos toLaunch = null;

            foreach (var replay in replays.ToArray())
            {
                if (replay.Selected)
                {
                    toLaunch = replay;
                }
            }

            if (toLaunch != null)
            {
                replays.Clear();
                Monocle.Music.Stop();
                Sounds.ui_mapZoom.Play();

                Task.Run(async () =>
                {
                    _inputService.DisableAllController();

                    var loader = new Loader(true);
                    loader.Position.Y = toLaunch.Position.Y;
                    Loader.Message = "LOADING REPLAY...";
                    TFGame.Instance.Scene.Add(new Fader());
                    TFGame.Instance.Scene.Add(loader);

                    await _replayService.LoadAndStart(toLaunch.OriginalName);

                    TFGame.Instance.Scene.Remove(loader);
                    Loader.Message = "";

                    _netplayManager.EnableReplayMode();
                    _inputService.EnableAllController();
                });
            }
        }

        private void MainMenu_CreateMain(On.TowerFall.MainMenu.orig_CreateMain orig, MainMenu self)
        {
            orig(self);

            var trials = self.GetToBeSpawned<TrialsButton>();
            var archivesButton = self.GetToBeSpawned<ArchivesButton>();
            var workshopButton = self.GetToBeSpawned<WorkshopButton>();
            var fightButton = self.GetToBeSpawned<FightButton>();
            var coopButton = self.GetToBeSpawned<CoOpButton>();
            var bladesButton = self.GetAllToBeSpawned<BladeButton>();
            var modButton = bladesButton.Single(blade =>
            {
                var dynBlade = DynamicData.For(blade);
                return dynBlade.Get<string>("name") == "MODS";
            });


            trials.Position.X += 40;
            var dynTrials = DynamicData.For(trials);
            dynTrials.Set("tweenTo", trials.Position);

            archivesButton.Position.X += 30;
            var dynArchives = DynamicData.For(archivesButton);
            dynArchives.Set("tweenTo", archivesButton.Position);

            workshopButton.Position.X += 25;
            var dynWorkshop = DynamicData.For(workshopButton);
            dynWorkshop.Set("tweenTo", workshopButton.Position);

            var replayButton = new ReplayButton(new Vector2(105f, 210f), new Vector2(100f, 300f), "REPLAYS", "");
            replayButton.RightItem = trials;
            replayButton.UpItem = fightButton;
            replayButton.LeftItem = modButton;

            foreach (var blade in bladesButton)
            {
                blade.RightItem = replayButton;
            }

            trials.LeftItem = replayButton;

            trials.UpItem = coopButton;
            archivesButton.UpItem = coopButton;
            workshopButton.UpItem = coopButton;
            coopButton.DownItem = archivesButton;

            self.Add(replayButton);
        }

        private void MainMenu_ctor(On.TowerFall.MainMenu.orig_ctor orig, MainMenu self, MainMenu.MenuState state)
        {
            orig(self, state);

            TowerFall.TFGame.ConsoleEnabled = true;
            SaveData.Instance.WithNetplayOptions();
        }

        private void MainMenu_Render(On.TowerFall.MainMenu.orig_Render orig, TowerFall.MainMenu self)
        {
            orig(self);

            Monocle.Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            if (self.State == TowerFall.MainMenu.MenuState.VersusOptions)
            {
                var mode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

                if (TowerFall.MainMenu.VersusMatchSettings != null
                && mode == Domain.Models.Modes.Netplay1v1QuickPlay
                && _matchmakingService.IsConnectedToServer())
                {
                    Monocle.Draw.OutlineTextCentered(TFGame.Font, $"{_matchmakingService.GetTotalAvailablePlayersInQuickPlayQueue()} PLAYERS", new Vector2(35f, 8f), Color.Aqua, 1f);
                }
            }

            Monocle.Draw.SpriteBatch.End();
        }

        private void MainMenu_InitOptions(On.TowerFall.MainMenu.orig_InitOptions orig, TowerFall.MainMenu self, List<OptionsButton> buttons)
        {
            OptionsButton inputDelay = new OptionsButton("NETPLAY INPUT DELAY");
            inputDelay.SetCallbacks(inputDelay.InputDelayState, InputDelayRightCallback, InputDelayLeftCallback, null);
            buttons.Insert(0, inputDelay);

            _netplayName = new OptionsButton("NETPLAY NAME");
            _netplayName.SetCallbacks(_netplayName.NameState, null, null, null);
            buttons.Insert(1, _netplayName);

            orig(self, buttons);
        }

        public void InputDelayRightCallback()
        {
            var config = _netplayManager.GetNetplayMeta();
            config.InputDelay--;
            _netplayManager.UpdateMeta(config);
            _netplayManager.SaveConfig();
        }

        public void InputDelayLeftCallback()
        {
            var config = _netplayManager.GetNetplayMeta();
            config.InputDelay++;
            _netplayManager.UpdateMeta(config);
            _netplayManager.SaveConfig();
        }

        private void MainMenu_Update(On.TowerFall.MainMenu.orig_Update orig, TowerFall.MainMenu self)
        {
            if (_netplayName != null && _netplayName.State != _netplayManager.GetNetplayMeta().Name)
            {
                _netplayName.State = _netplayManager.GetNetplayMeta().Name;
            }

            if (TowerFall.MainMenu.VersusMatchSettings != null && self.State == TowerFall.MainMenu.MenuState.VersusOptions)
            {
                if (TFGame.PlayerInputs[0] != null && TFGame.PlayerInputs[0].MenuBack)
                {
                    if (!_matchmakingService.HasRegisteredForQuickPlay())
                    {
                        self.State = TowerFall.MainMenu.MenuState.Main;
                    }
                    return;
                }
            }

            orig(self);
        }
    }

    public static class OptionsButtonExtensions
    {
        public static void InputDelayState(this OptionsButton optionsButton)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            optionsButton.State = netplayManager.GetNetplayMeta().InputDelay.ToString();
            optionsButton.CanLeft = netplayManager.GetNetplayMeta().InputDelay > 1;
            optionsButton.CanRight = netplayManager.GetNetplayMeta().InputDelay < 20;
        }

        public static void NameState(this OptionsButton optionsButton)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            optionsButton.State = netplayManager.GetNetplayMeta().Name.ToString();
        }
    }

}
