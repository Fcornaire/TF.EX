﻿using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.WebSocket;
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
        private readonly IRngService rngService;
        private readonly IAPIManager _apiManager;
        private readonly ILogger _logger;

        private bool hasShowedWarning = false;

        private OptionsButton netplayName;

        private List<ReplayInfos> replays = new List<ReplayInfos>();
        private ReplaysPanel _replaysPanel = null;

        private List<LobbyInfos> lobbies = new List<LobbyInfos>();
        private LobbyPanel lobbyPanel = null;
        private Monocle.Entity spectateEntityButton = null;
        private Monocle.Entity createEntityButton = null;

        private LobbyVersusModeButton lobbyVersusModeButton = null;
        private LobbyVersusCoinButton lobbyVersusCoinButton = null;
        private LobbyVersusMapButton lobbyVersusMapButton = null;
        private LobbyVarianText lobbyVarianText = null;
        private List<VariantItem> variants = new List<VariantItem>();

        private DateTime nextServerPull = DateTime.UtcNow;

        private Text noMsg;

        public MainMenuPatch(IMatchmakingService matchmakingService,
            INetplayManager netplayManager,
            IReplayService replayService,
            IInputService inputService,
            IRngService rngService,
            IAPIManager apiManager,
            ILogger logger)
        {
            _matchmakingService = matchmakingService;
            _netplayManager = netplayManager;
            _replayService = replayService;
            _inputService = inputService;
            this.rngService = rngService;
            _apiManager = apiManager;
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
            switch (state.ToDomainModel())
            {
                case Domain.Models.MenuState.ReplaysBrowser:
                    HandleReplayBrowser(self, name);
                    break;
                case Domain.Models.MenuState.LobbyBrowser:
                    HandleLobbyBrowser(self, name);
                    break;
                case Domain.Models.MenuState.LobbyBuilder:
                    HandleLobbyBuilder(self, name);
                    break;
                case MenuState.PressStart:
                    if (!hasShowedWarning)
                    {
                        bool hasUnsafeMod = false;
                        var otherMods = FortRise.RiseCore.Modules.Where(module => module.ID != "dshad.tf.ex" && module.ID != "com.fortrise.adventure");

                        foreach (var mod in otherMods)
                        {
                            if (!_apiManager.IsModuleSafe(mod.ID))
                            {
                                hasUnsafeMod = true;
                                break;
                            }
                        }

                        if (hasUnsafeMod)
                        {
                            Sounds.ui_clickSpecial.Play(160, 4);
                            Notification.Create(TFGame.Instance.Scene, "You have mods that might break with TF.EX! Expect bugs!", 10, 600);
                            hasShowedWarning = true;
                        }
                    }

                    orig(self, name, state);
                    break;
                default:
                    orig(self, name, state);
                    break;
            }
        }

        private void HandleLobbyBuilder(MainMenu self, string name)
        {
            if (name == "Destroy")
            {
                if (lobbyVersusModeButton != null)
                {
                    lobbyVersusModeButton.RemoveSelf();
                    lobbyVersusModeButton = null;
                }

                if (lobbyVersusCoinButton != null)
                {
                    lobbyVersusCoinButton.RemoveSelf();
                    lobbyVersusCoinButton = null;
                }

                if (lobbyVersusMapButton != null)
                {
                    lobbyVersusMapButton.RemoveSelf();
                    lobbyVersusMapButton = null;
                }

                if (lobbyVarianText != null)
                {
                    lobbyVarianText.RemoveSelf();
                    lobbyVarianText = null;
                }

                if (variants != null)
                {
                    foreach (var variant in variants)
                    {
                        variant.RemoveSelf();
                    }

                    variants.Clear();
                }

                if (createEntityButton != null)
                {
                    createEntityButton.RemoveSelf();
                    createEntityButton = null;
                }
            }
            else
            {
                CreateLobbyBuilder(self);
            }
        }

        private void CreateLobbyBuilder(MainMenu self)
        {
            self.BackState = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();

            lobbyVersusModeButton = new LobbyVersusModeButton(new Vector2(160f, 90f), new Vector2(-100f, 90f));
            self.Add(lobbyVersusModeButton);

            lobbyVersusMapButton = new LobbyVersusMapButton(new Vector2(160f, 130f), new Vector2(-100f, 210f));
            self.Add(lobbyVersusMapButton);

            lobbyVersusCoinButton = new LobbyVersusCoinButton(new Vector2(160f, 190f), new Vector2(420f, 135f));
            self.Add(lobbyVersusCoinButton);

            lobbyVarianText = new LobbyVarianText(new Vector2(160f, 220f), new Vector2(-120f, 135f));
            self.Add(lobbyVarianText);

            foreach (var variant in variants)
            {
                variant.Position.Y += 200;
            }

            self.MaxUICameraY += 230;

            var dynMainMenu = DynamicData.For(self);
            dynMainMenu.Invoke("TweenBGCameraToY", 3);
            dynMainMenu.Set("ToStartSelected", lobbyVersusModeButton);
            lobbyVersusModeButton.DownItem = lobbyVersusMapButton;
            lobbyVersusMapButton.UpItem = lobbyVersusModeButton;
            lobbyVersusMapButton.DownItem = lobbyVersusCoinButton;
            lobbyVersusCoinButton.UpItem = lobbyVersusMapButton;
            lobbyVersusCoinButton.DownItem = variants[0];
            variants[0].UpItem = lobbyVersusCoinButton;
            variants[1].UpItem = lobbyVersusCoinButton;
            variants[2].UpItem = lobbyVersusCoinButton;
            variants[3].UpItem = lobbyVersusCoinButton;

            self.Add(variants);

            try
            {
                createEntityButton = new Monocle.Entity();
                var createButton = new MenuButtonGuide(4);
                createButton.SetDetails(MenuButtonGuide.ButtonModes.Start, "CREATE LOBBY");
                createEntityButton.Add(createButton);
                self.Add(createEntityButton);

                self.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "VARIANT SELECT");
                self.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "CANCEL");
            }
            catch (System.Exception ex)
            {
                _logger.LogError<MainMenuPatch>("Error when creating lobby builder menu", ex);
            }

            Sounds.ui_startGame.Play();
        }

        private void HandleLobbyBrowser(MainMenu self, string name)
        {
            if (name == "Destroy")
            {
                self.DeleteAll<LobbyInfos>();

                if (lobbyPanel != null)
                {
                    lobbyPanel.RemoveSelf();
                    lobbyPanel = null;
                }

                if (lobbies != null)
                {
                    foreach (var lobby in lobbies.ToArray())
                    {
                        lobby.RemoveSelf();
                    }
                    lobbies.Clear();
                }

                if (noMsg != null)
                {
                    noMsg.RemoveSelf();
                    noMsg = null;
                }

                if (spectateEntityButton != null)
                {
                    spectateEntityButton.RemoveSelf();
                    spectateEntityButton = null;
                }
            }
            else
            {
                TFGameExtensions.ResetVersusChoices();
                CreateLobbyBrowser(self);
            }
        }

        private void HandleReplayBrowser(MainMenu self, string name)
        {
            if (name == "Destroy")
            {
                self.DeleteAll<ReplayInfos>();

                if (_replaysPanel != null)
                {
                    _replaysPanel.RemoveSelf();
                }

                if (replays != null)
                {
                    RemoveReplays();
                }

                if (noMsg != null)
                {
                    noMsg.RemoveSelf();
                    noMsg = null;
                }
            }
            else
            {
                CreateReplay(self);
            }
        }

        private void CreateLobbyBrowser(MainMenu self)
        {
            if (variants.Count == 0)
            {
                variants = MainMenu.VersusMatchSettings.Variants.BuildMenu(self, out _, out self.MaxUICameraY);
            }

            self.AddLoader("FINDING LOBBIES...");

            self.BackState = TowerFall.MainMenu.MenuState.VersusOptions;
            MainMenu.VersusMatchSettings.Mode = TF.EX.Domain.Models.Modes.Netplay.ToTF();

            Task.Run(async () =>
            {
                try
                {
                    _inputService.DisableAllControllers();

                    _matchmakingService.ResetLobbies();
                    await _matchmakingService.GetLobbies(OnRetrieveSuccess, () =>
                    {
                        self.RemoveLoader();
                        Notification.Create(self, "Failed to retrieve lobbies...");
                        Sounds.ui_invalid.Play();
                        _inputService.EnableAllControllers();
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError<MainMenuPatch>("Error when creating lobby browser menu", ex);
                }
            });
        }

        private void CreateReplay(MainMenu self)
        {
            _inputService.DisableAllControllers();

            self.AddLoader("LOADING REPLAYS...");

            self.BackState = TowerFall.MainMenu.MenuState.Main;

            Task.Run(async () =>
            {
                try
                {
                    var dynMainMenu = DynamicData.For(self);
                    var dynCamera = DynamicData.For(self.UILayer);

                    var maxY = 50.0f;

                    var replays = (await _replayService.LoadAndGetReplays()).ToArray();
                    if (replays.Length == 0)
                    {
                        _logger.LogError<MainMenuPatch>("No replay found");
                        _inputService.EnableAllControllers();
                        self.RemoveLoader();

                        noMsg = new Text("PLAY SOME ONLINE GAMES FIRST!");
                        noMsg.Position = new Vector2(100, 100);
                        self.Add(noMsg);

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

                    dynMainMenu.Invoke("TweenBGCameraToY", 1);

                    _inputService.EnableAllControllers();

                    self.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "LAUNCH");
                    self.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "EXIT");
                    Loader.Message = "";
                    self.RemoveLoader();

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
                RemoveReplays();
                Monocle.Music.Stop();
                Sounds.ui_mapZoom.Play();

                Task.Run(async () =>
                {
                    _inputService.DisableAllControllers();

                    TFGame.Instance.Scene.AddLoader("LOADING REPLAY...");

                    await _replayService.LoadAndStart(toLaunch.OriginalName);

                    TFGame.Instance.Scene.RemoveLoader();

                    _inputService.EnableAllControllers();
                    _inputService.EnsureFakeControllers();
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

            if (self.State == MainMenu.MenuState.Rollcall
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel() == Domain.Models.Modes.Netplay)
            {
                var opponents = _matchmakingService.IsSpectator()
                    ? _matchmakingService.GetOwnLobby().Players.Where(pl => pl.IsHost).ToArray()
                    : _matchmakingService.GetOwnLobby().Players.Where(p => p.RoomChatPeerId != _matchmakingService.GetRoomChatPeerId()).ToArray();
                int playerIndex = 1;

                foreach (var opponent in opponents)
                {
                    var latency = _matchmakingService.GetPingToOpponent();
                    if (latency > 0)
                    {
                        var color = Color.White;

                        switch (latency)
                        {
                            case var n when (n >= 0 && n < 60):
                                color = Color.LightGreen;
                                break;
                            case var n when (n >= 60 && n < 120):
                                color = Color.GreenYellow;
                                break;
                            case var n when (n >= 120 && n < 150):
                                color = Color.OrangeRed;
                                break;
                            case var n when (n >= 150):
                                color = Color.Red;
                                break;
                            default:
                                break;
                        }

                        var rollcallElement = self.GetAll<RollcallElement>().First(rc =>
                        {
                            var dyn = DynamicData.For(rc);
                            var index = dyn.Get<int>("playerIndex");

                            return index == playerIndex;
                        });

                        var posPing = rollcallElement.Position;
                        var dynRollcall = DynamicData.For(rollcallElement);

                        var controlIconPos = dynRollcall.Get<Vector2>("ControlIconPos");
                        var posName = rollcallElement.Position + controlIconPos + Vector2.UnitY * 15f;
                        posPing.Y -= 66f;

                        Monocle.Draw.OutlineTextCentered(TFGame.Font, $"{latency} MS", posPing, color, 1.5f);

                        var state = dynRollcall.Get<Monocle.StateMachine>("state");
                        var nameColor = ((state.State == 1) ? ArcherData.Archers[playerIndex].ColorB : ArcherData.Archers[playerIndex].ColorA);
                        Monocle.Draw.OutlineTextCentered(TFGame.Font, opponent.Name, posName, nameColor, Color.Black);
                    }

                    playerIndex++;
                }
            }

            Monocle.Draw.SpriteBatch.End();
        }

        private void MainMenu_InitOptions(On.TowerFall.MainMenu.orig_InitOptions orig, TowerFall.MainMenu self, List<OptionsButton> buttons)
        {
            OptionsButton inputDelay = new OptionsButton(Constants.NETPLAY_INPUT_DELAY_TITLE);
            inputDelay.SetCallbacks(inputDelay.InputDelayState, InputDelayRightCallback, InputDelayLeftCallback, null);
            buttons.Insert(0, inputDelay);

            netplayName = new OptionsButton(Constants.NETPLAY_USERNAME_TITLE);
            netplayName.SetCallbacks(netplayName.NameState, null, null, null);
            buttons.Insert(1, netplayName);

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
            if (netplayName != null && netplayName.State != _netplayManager.GetNetplayMeta().Name)
            {
                netplayName.State = _netplayManager.GetNetplayMeta().Name;
            }

            if (TowerFall.MainMenu.VersusMatchSettings != null && self.State == TowerFall.MainMenu.MenuState.VersusOptions)
            {
                if (MenuInput.Back)
                {
                    self.State = TowerFall.MainMenu.MenuState.Main;
                    return;
                }
            }

            orig(self);

            if (self.State.ToDomainModel() == Domain.Models.MenuState.LobbyBrowser)
            {
                if (MenuInput.Start)
                {
                    self.State = TF.EX.Domain.Models.MenuState.LobbyBuilder.ToTFModel();
                    return;
                }

                if (MenuInput.Alt && DateTime.UtcNow >= nextServerPull)
                {
                    _inputService.DisableAllControllers();

                    nextServerPull = DateTime.UtcNow.AddSeconds(3);
                    Sounds.ui_altCostumeShift.Play();
                    self.AddLoader("FINDING LOBBIES...");

                    foreach (var lobby in lobbies.ToArray())
                    {
                        lobby.RemoveSelf();
                    }

                    lobbies.Clear();

                    if (lobbyPanel != null)
                    {
                        lobbyPanel.RemoveSelf();
                        lobbyPanel = null;
                    }

                    _matchmakingService.ResetLobbies();
                    _matchmakingService.GetLobbies(OnRetrieveSuccess, () =>
                    {
                        self.RemoveLoader();
                        Notification.Create(self, "Failed to retrieve lobbies...");
                        Sounds.ui_invalid.Play();
                        _inputService.EnableAllControllers();
                    });
                    return;
                }

                if (MenuInput.Alt2)
                {
                    if (!lobbies.SingleOrDefault(lobby => lobby.Selected))
                    {
                        Notification.Create(self, "You must select a lobby to spectate!");
                        Sounds.ui_invalid.Play();
                        return;
                    }

                    var lobbyToSpectate = lobbies.Single(lobby => lobby.Selected).Lobby;

                    _inputService.DisableAllControllers();

                    var variantsToggle = variants
                        .Where(v => v is VariantToggle)
                        .Select(v => (v as VariantToggle).Variant.Title)
                    .ToList();

                    bool canJoin = lobbyToSpectate.GameData.Variants.All(str => variantsToggle.Contains(str));

                    if (!canJoin)
                    {
                        _logger.LogError<MainMenuPatch>("Can't join lobby because of custom variants");

                        _inputService.EnableAllControllers();

                        TowerFall.Sounds.ui_invalid.Play();
                        return;
                    }

                    if (string.IsNullOrEmpty(lobbyToSpectate.RoomId) || string.IsNullOrEmpty(lobbyToSpectate.RoomChatId))
                    {
                        _logger.LogError<MainMenuPatch>("Lobby to spectate is null?");

                        return;
                    }

                    self.AddLoader("JOINING LOBBY AS SPECTATOR");

                    _matchmakingService.JoinLobby(lobbyToSpectate.RoomId, false, () => OnJoinSuccess(self, lobbyToSpectate, false), () => OnFailedToJoinLobby(self));
                }
            }

            if (self.State.ToDomainModel() == Domain.Models.MenuState.LobbyBuilder)
            {
                if (MenuInput.Start)
                {
                    var variantsToggle = variants
                        .Where(v => v is VariantToggle && (v as VariantToggle).Variant.Value)
                        .Select(v => (v as VariantToggle).Variant.Title)
                        .ToList();

                    var roomId = Guid.NewGuid().ToString();
                    var roomChatId = Guid.NewGuid().ToString();

                    var roomUrl = $"{Config.SERVER}/room/{roomId}";
                    var roomChatUrl = $"{Config.SERVER}/room/{roomChatId}";

                    var lobby = _matchmakingService.GetOwnLobby();
                    lobby.GameData.Variants = variantsToggle.ToArray();
                    lobby.Name = _netplayManager.GetNetplayMeta().Name;
                    lobby.RoomChatId = roomChatId;
                    lobby.RoomId = roomId;
                    lobby.Players.Add(new Domain.Models.WebSocket.Player
                    {
                        Name = _netplayManager.GetNetplayMeta().Name,
                        Addr = string.Empty,
                        IsHost = true
                    });

                    self.AddLoader("CREATING LOBBY...");
                    Sounds.ui_click.Play();

                    _inputService.DisableAllControllers();

                    Action onSucess = () =>
                    {
                        _inputService.EnableAllControllers();
                        _inputService.DisableAllControllerExceptLocal();
                        self.RemoveLoader();
                        Sounds.ui_click.Play();
                        self.State = MainMenu.MenuState.Rollcall;
                        self.BackState = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                        _netplayManager.SetRoomAndServerMode(roomUrl, true);
                        _matchmakingService.ConnectAndListenToLobby(roomChatUrl);

                        rngService.SetSeed(_matchmakingService.GetOwnLobby().GameData.Seed);

                        //Apply length
                        MainMenu.VersusMatchSettings.MatchLength = (MatchSettings.MatchLengths)lobby.GameData.MatchLength;

                        if (MainMenu.VersusMatchSettings.Variants.ContainsCustomVariant(lobby.GameData.Variants))
                        {
                            Sounds.ui_clickSpecial.Play(160, 4);
                            Notification.Create(self, $"Be CAREFUL! Custom variants might not work properly", 15, 500);
                        }
                    };

                    Action onFail = () =>
                    {
                        Notification.Create(self, $"Failed to create lobby", 10, 120);
                        _inputService.EnableAllControllers();
                        self.RemoveLoader();
                        TowerFall.Sounds.ui_invalid.Play();
                        self.State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                    };

                    _matchmakingService.CreateLobby(onSucess, onFail);
                }
            }
        }

        private void OnRetrieveSuccess()
        {
            _inputService.EnableAllControllers();

            var self = TFGame.Instance.Scene as TowerFall.MainMenu;

            self.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "JOIN");
            self.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "RETURN");
            self.ButtonGuideC.SetDetails(MenuButtonGuide.ButtonModes.Start, "CREATE");
            self.ButtonGuideD.SetDetails(MenuButtonGuide.ButtonModes.Alt, "REFRESH");

            if (spectateEntityButton == null)
            {
                spectateEntityButton = new Monocle.Entity(-1);
                var spectateButton = new MenuButtonGuide(4);
                spectateButton.SetDetails(MenuButtonGuide.ButtonModes.Alt2, "SPECTATE");
                spectateEntityButton.Add(spectateButton);
                self.Add(spectateEntityButton);
            }

            self.RemoveLoader();

            var reslobbies = _matchmakingService.GetLobbies();

            if (lobbies.Count == 0 && reslobbies.Count() > 0 || lobbies.Count != reslobbies.Count())
            {
                var dynMainMenu = DynamicData.For(self);
                var dynCamera = DynamicData.For(self.UILayer);

                lobbyPanel = new LobbyPanel(225, 110);
                var maxY = 50.0f;

                var lobb = reslobbies.ToArray();
                for (int i = 0; i < lobb.Length; i++)
                {
                    var newLobby = lobb[i];

                    if (newLobby == null)
                    {
                        _logger.LogError<MainMenuPatch>("Lobby is null?");

                        continue;
                    }

                    Action onClick = () =>
                    {
                        _inputService.DisableAllControllers();

                        var variantsToggle = variants
                            .Where(v => v is VariantToggle)
                            .Select(v => (v as VariantToggle).Variant.Title)
                        .ToList();

                        bool canJoin = newLobby.GameData.Variants.All(str => variantsToggle.Contains(str));

                        if (!canJoin)
                        {
                            _logger.LogError<MainMenuPatch>("Can't join lobby because of custom variants");

                            _inputService.EnableAllControllers();

                            TowerFall.Sounds.ui_invalid.Play();
                            return;
                        }

                        if (string.IsNullOrEmpty(newLobby.RoomId) || string.IsNullOrEmpty(newLobby.RoomChatId))
                        {
                            _logger.LogError<MainMenuPatch>("Lobby to join is null?");
                            return;
                        }

                        self.AddLoader("JOINING LOBBY");

                        _matchmakingService.JoinLobby(newLobby.RoomId, true, () => OnJoinSuccess(self, newLobby, true), () => OnFailedToJoinLobby(self));
                    };

                    var but = new LobbyInfos(self, new Vector2(5.0f, maxY), newLobby, onClick, lobbyPanel);

                    this.lobbies.Add(but);
                    if (i != 0)
                    {
                        this.lobbies[i - 1].DownItem = but;
                        but.UpItem = this.lobbies[i - 1];
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

                dynCamera.Invoke("Add", lobbyPanel, false);
                dynMainMenu.Invoke("TweenBGCameraToY", 1);

                _inputService.EnableAllControllers();
                return;
            }

            foreach (var lobby in reslobbies.ToArray())
            {
                var lob = lobbies.Where(l => l.Lobby.RoomId == lobby.RoomId).FirstOrDefault();
                if (lob != null)
                {
                    lob.UpdateLobby(lobby);
                }
            }

            if (lobbies.Count == 0)
            {
                if (noMsg == null)
                {
                    noMsg = new Text("NO LOBBIES FOUND");

                    noMsg.Position = new Vector2(100, 100);
                    self.Add(noMsg);

                    _inputService.EnableAllControllers();
                }
            }
        }

        private void OnJoinSuccess(MainMenu self, Lobby newLobby, bool isPlayer)
        {
            self.RemoveLoader();
            Sounds.ui_click.Play();

            var roomUrl = $"{Config.SERVER}/room/{newLobby.RoomId}";
            var roomChatUrl = $"{Config.SERVER}/room/{newLobby.RoomChatId}";

            if (isPlayer)
            {
                _netplayManager.SetRoomAndServerMode(roomUrl, false);
                _netplayManager.UpdatePlayer2Name(newLobby.Players.First(pl => pl.IsHost).Name);
                _matchmakingService.ConnectAndListenToLobby(roomChatUrl);
            }

            _matchmakingService.UpdateOwnLobby(newLobby);
            _inputService.EnableAllControllers();
            _inputService.DisableAllControllerExceptLocal();

            rngService.SetSeed(newLobby.GameData.Seed);

            //Apply variant
            MainMenu.VersusMatchSettings.Variants.ApplyVariants(newLobby.GameData.Variants);

            //Apply length
            MainMenu.VersusMatchSettings.MatchLength = (MatchSettings.MatchLengths)newLobby.GameData.MatchLength;

            self.State = MainMenu.MenuState.Rollcall;

            if (MainMenu.VersusMatchSettings.Variants.ContainsCustomVariant(newLobby.GameData.Variants))
            {
                Sounds.ui_clickSpecial.Play(160, 4);
                Notification.Create(self, $"Be CAREFUL! Custom variants might not work properly", 15, 500);
            }
        }

        private void OnFailedToJoinLobby(MainMenu self)
        {
            Notification.Create(self, $"Failed to join lobby: either full, gone, or already started", 15, 450);

            _inputService.EnableAllControllers();
            self.RemoveLoader();
            TowerFall.Sounds.ui_invalid.Play();

            foreach (var lobby in lobbies.ToArray())
            {
                lobby.RemoveSelf();
            }

            lobbies.Clear();
            if (lobbyPanel != null)
            {
                lobbyPanel.RemoveSelf();
                lobbyPanel = null;
            }
        }

        private void RemoveReplays()
        {
            //foreach (var replay in replays.ToArray())
            //{
            //    replay.RemoveSelf();
            //}

            replays.Clear();
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
