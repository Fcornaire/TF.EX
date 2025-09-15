using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Common.Interop;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.TowerFallExtensions;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    [HarmonyPatch(typeof(MainMenu))]
    public class MainMenuPatch
    {
        private static bool hasShowedWarning = false;

        private static OptionsButton netplayName;

        private static List<ReplayInfos> replays = new List<ReplayInfos>();
        private static ReplaysPanel _replaysPanel = null;

        private static List<LobbyInfos> lobbies = new List<LobbyInfos>();
        private static LobbyPanel lobbyPanel = null;
        private static Monocle.Entity spectateEntityButton = null;
        private static Monocle.Entity createEntityButton = null;

        private static LobbyVersusModeButton lobbyVersusModeButton = null;
        private static LobbyVersusCoinButton lobbyVersusCoinButton = null;
        private static LobbyVersusMapButton lobbyVersusMapButton = null;
        private static LobbyVarianText lobbyVarianText = null;
        private static List<VariantItem> variants = new List<VariantItem>();

        private static DateTime nextServerPull = DateTime.UtcNow;

        private static Text noMsg;

        [HarmonyPrefix]
        [HarmonyPatch("CallStateFunc")]
        public static bool MainMenu_CallStateFunc(MainMenu __instance, string name, MainMenu.MenuState state)
        {
            switch (state.ToDomainModel())
            {
                case Domain.Models.MenuState.ReplaysBrowser:
                    HandleReplayBrowser(__instance, name);
                    return false;
                case Domain.Models.MenuState.LobbyBrowser:
                    HandleLobbyBrowser(__instance, name);
                    return false;
                case Domain.Models.MenuState.LobbyBuilder:
                    HandleLobbyBuilder(__instance, name);
                    return false;
                case MenuState.PressStart:
                    if (!hasShowedWarning)
                    {
                        //bool hasUnsafeMod = false;
                        //var context = ServiceCollections.ResolveContext();

                        ////TODO: Check if we can use FortRise to get the modules
                        //var otherMods = context.Interop.LoadedMods.Where(module => module.Metadata.Name != "dshad.tf.ex" && module.Metadata.Name != "com.fortrise.adventure");

                        //foreach (var mod in otherMods)
                        //{
                        //    if (!_apiManager.IsModuleSafe(mod.ID))
                        //    {
                        //        hasUnsafeMod = true;
                        //        break;
                        //    }
                        //}

                        //if (hasUnsafeMod)
                        //{
                        //    Sounds.ui_clickSpecial.Play(160, 4);
                        //    Notification.Create(TFGame.Instance.Scene, "You have mods that might break with TF.EX! Expect bugs!", 10, 600);
                        //    hasShowedWarning = true;
                        //}
                    }

                    return true;
                case MenuState.Unknown:
                    return true;
                default:
                    return true;
            }
        }

        private static void HandleLobbyBuilder(MainMenu self, string name)
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

        private static void CreateLobbyBuilder(MainMenu self)
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
                var logger = ServiceCollections.ResolveLogger();
                logger.LogError<MainMenuPatch>("Error when creating lobby builder menu", ex);
            }

            Sounds.ui_startGame.Play();
        }

        private static void HandleLobbyBrowser(MainMenu self, string name)
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

        private static void HandleReplayBrowser(MainMenu self, string name)
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

        private static void CreateLobbyBrowser(MainMenu self)
        {
            if (variants.Count == 0)
            {
                variants = MainMenu.VersusMatchSettings.Variants.BuildMenu(self, out _, out self.MaxUICameraY);
            }

            self.AddLoader("FINDING LOBBIES...");

            self.BackState = TowerFall.MainMenu.MenuState.Rollcall;
            MainMenu.VersusMatchSettings.Mode = TF.EX.Domain.Models.Modes.Netplay.ToTF();

            Task.Run(async () =>
            {
                try
                {
                    var inputService = ServiceCollections.ResolveInputService();
                    var matchmakingService = ServiceCollections.ResolveMatchmakingService();

                    inputService.DisableAllControllers();

                    matchmakingService.ResetLobbies();
                    await matchmakingService.GetLobbies(OnRetrieveSuccess, () =>
                    {
                        self.RemoveLoader();
                        Notification.Create(self, "Failed to retrieve lobbies...");
                        Sounds.ui_invalid.Play();
                        inputService.EnableAllControllers();
                    });
                }
                catch (Exception ex)
                {
                    var logger = ServiceCollections.ResolveLogger();
                    logger.LogError<MainMenuPatch>("Error when creating lobby browser menu", ex);
                }
            });
        }

        private static void CreateReplay(MainMenu self)
        {
            var inputService = ServiceCollections.ResolveInputService();
            var replayService = ServiceCollections.ResolveReplayService();
            var logger = ServiceCollections.ResolveLogger();

            inputService.DisableAllControllers();

            self.AddLoader("LOADING REPLAYS...");

            self.BackState = TowerFall.MainMenu.MenuState.Main;

            Task.Run(async () =>
            {
                try
                {
                    var dynMainMenu = DynamicData.For(self);
                    var dynCamera = DynamicData.For(self.UILayer);

                    var maxY = 50.0f;

                    var replays = (await replayService.LoadAndGetReplays()).ToArray();
                    if (replays.Length == 0)
                    {
                        logger.LogError<MainMenuPatch>("No replay found");
                        inputService.EnableAllControllers();
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
                            logger.LogError<MainMenuPatch>("Replay is null ?");

                            continue;
                        }

                        var but = new ReplayInfos(self, new Vector2(5.0f, maxY), replays[i], LaunchRecord, _replaysPanel);

                        MainMenuPatch.replays.Add(but);
                        if (i != 0)
                        {
                            MainMenuPatch.replays[i - 1].DownItem = but;
                            but.UpItem = MainMenuPatch.replays[i - 1];
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

                    inputService.EnableAllControllers();

                    self.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "LAUNCH");
                    self.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "EXIT");
                    Loader.Message = "";
                    self.RemoveLoader();

                    Sounds.ui_startGame.Play();
                }
                catch (Exception ex)
                {
                    logger.LogError<MainMenuPatch>("Error when creating replay menu", ex);
                }
            });
        }

        private static void LaunchRecord()
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
                var currentSong = Monocle.Music.CurrentSong;
                Monocle.Music.Stop();
                Sounds.ui_mapZoom.Play();

                Task.Run(async () =>
                {
                    var inputService = ServiceCollections.ResolveInputService();
                    var replayService = ServiceCollections.ResolveReplayService();
                    inputService.DisableAllControllers();

                    TFGame.Instance.Scene.AddLoader("LOADING REPLAY...");

                    await replayService.LoadAndStart(toLaunch.OriginalName, currentSong);

                    TFGame.Instance.Scene.RemoveLoader();

                    inputService.EnableAllControllers();
                    inputService.EnsureFakeControllers();
                });
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("CreateMain")]
        public static void MainMenu_CreateMain(MainMenu __instance)
        {
            var widerSetModApi = ServiceCollections.ResolveWiderSetModApi();
            if (widerSetModApi != null && widerSetModApi.IsWide)
            {
                widerSetModApi.IsWide = false;
            }

            var trials = __instance.GetToBeSpawned<TrialsButton>();
            var archivesButton = __instance.GetToBeSpawned<ArchivesButton>();
            var workshopButton = __instance.GetToBeSpawned<WorkshopButton>();
            var fightButton = __instance.GetToBeSpawned<FightButton>();
            var coopButton = __instance.GetToBeSpawned<CoOpButton>();
            var bladesButton = __instance.GetAllToBeSpawned<BladeButton>();
            var modButton = bladesButton.Single(blade =>
            {
                return Traverse.Create(blade).Field<string>("name").Value == "MODS";
            });


            trials.Position.X += 40;
            Traverse.Create(trials).Field("tweenTo").SetValue(trials.Position);

            archivesButton.Position.X += 30;
            Traverse.Create(archivesButton).Field("tweenTo").SetValue(archivesButton.Position);

            workshopButton.Position.X += 25;
            Traverse.Create(workshopButton).Field("tweenTo").SetValue(workshopButton.Position);

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

            __instance.Add(replayButton);
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(MainMenu.MenuState)])]
        public static void MainMenu_ctor()
        {
            TowerFall.TFGame.ConsoleEnabled = true;
            SaveData.Instance.WithNetplayOptions();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Render")]
        public static void MainMenu_Render(MainMenu __instance)
        {
            Monocle.Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            if (__instance.State == MainMenu.MenuState.Rollcall
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel() == Domain.Models.Modes.Netplay)
            {
                var matchmakingService = ServiceCollections.ResolveMatchmakingService();

                var opponents = matchmakingService.IsSpectator()
                    ? matchmakingService.GetOwnLobby().Players.Where(pl => pl.IsHost).ToArray()
                    : matchmakingService.GetOwnLobby().Players.Where(p => p.RoomChatPeerId != matchmakingService.GetRoomChatPeerId()).ToArray();
                int playerIndex = 1;

                foreach (var opponent in opponents)
                {
                    var latency = matchmakingService.GetPingToOpponent();
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

                        var rollcallElement = __instance.GetAll<RollcallElement>().First(rc =>
                        {
                            var dyn = DynamicData.For(rc);
                            var index = dyn.Get<int>("playerIndex");

                            return index == playerIndex;
                        });

                        var posPing = rollcallElement.Position;
                        var dynRollcall = DynamicData.For(rollcallElement);

                        //var controlIconPos = dynRollcall.Get<Vector2>("ControlIconPos");
                        //var posName = rollcallElement.Position + controlIconPos + Vector2.UnitY * 15f;
                        posPing.Y -= 66f;

                        Monocle.Draw.OutlineTextCentered(TFGame.Font, $"{latency} MS", posPing, color, 1.5f);

                        //var state = dynRollcall.Get<Monocle.StateMachine>("state");
                        //var nameColor = ((state.State == 1) ? ArcherData.Archers[playerIndex].ColorB : ArcherData.Archers[playerIndex].ColorA);
                        //Monocle.Draw.OutlineTextCentered(TFGame.Font, opponent.Name, posName, nameColor, Color.Black);
                    }

                    playerIndex++;
                }
            }

            Monocle.Draw.SpriteBatch.End();
        }

        [HarmonyPrefix]
        [HarmonyPatch("InitOptions")]
        public static void MainMenu_InitOptions(List<OptionsButton> buttons)
        {
            OptionsButton inputDelay = new OptionsButton(Constants.NETPLAY_INPUT_DELAY_TITLE);
            inputDelay.SetCallbacks(inputDelay.InputDelayState, InputDelayRightCallback, InputDelayLeftCallback, null);
            buttons.Insert(0, inputDelay);

            netplayName = new OptionsButton(Constants.NETPLAY_USERNAME_TITLE);
            netplayName.SetCallbacks(netplayName.NameState, null, null, null);
            buttons.Insert(1, netplayName);
        }

        public static void InputDelayRightCallback()
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            var config = netplayManager.GetNetplayMeta();
            config.InputDelay--;
            netplayManager.UpdateMeta(config);
            netplayManager.SaveConfig();
        }

        public static void InputDelayLeftCallback()
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            var config = netplayManager.GetNetplayMeta();
            config.InputDelay++;
            netplayManager.UpdateMeta(config);
            netplayManager.SaveConfig();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool MainMenu_Update_Prefix(MainMenu __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (netplayName != null && netplayName.State != netplayManager.GetNetplayMeta().Name)
            {
                netplayName.State = netplayManager.GetNetplayMeta().Name;
            }

            if (TowerFall.MainMenu.VersusMatchSettings != null && __instance.State == TowerFall.MainMenu.MenuState.VersusOptions)
            {
                if (MenuInput.Back)
                {
                    __instance.State = TowerFall.MainMenu.MenuState.Main;
                    return false;
                }
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("CreateVersusOptions")]
        public static void MainMenu_CreateVersusOptions(MainMenu __instance)
        {
            if (MainMenu.VersusMatchSettings.Mode == TowerFall.Modes.LastManStanding)
            {
                var button = TFGame.Instance.Scene.GetToBeSpawned<VersusModeButton>();
                __instance.ToStartSelected = button;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void MainMenu_Update_Postfix(MainMenu __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var rngService = ServiceCollections.ResolveRngService();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var replayService = ServiceCollections.ResolveReplayService();
            var inputService = ServiceCollections.ResolveInputService();
            var logger = ServiceCollections.ResolveLogger();

            if (__instance.State == MainMenu.MenuState.Rollcall)
            {
                if (matchmakingService.GetOwnLobby().IsEmpty && TFGame.PlayerAmount == 0)
                {
                    __instance.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.SaveReplay, "PLAY EX (SOLO)");
                }
                else
                {
                    __instance.ButtonGuideA.Clear();
                }

                if (MenuInput.SaveReplay && TFGame.PlayerAmount == 0)
                {
                    __instance.State = MainMenu.MenuState.VersusOptions;
                }
            }

            if (__instance.State.ToDomainModel() == Domain.Models.MenuState.LobbyBrowser)
            {
                if (MenuInput.Start)
                {
                    __instance.State = TF.EX.Domain.Models.MenuState.LobbyBuilder.ToTFModel();
                    return;
                }

                if (MenuInput.Alt && DateTime.UtcNow >= nextServerPull)
                {
                    inputService.DisableAllControllers();

                    nextServerPull = DateTime.UtcNow.AddSeconds(3);
                    Sounds.ui_altCostumeShift.Play();
                    __instance.AddLoader("FINDING LOBBIES...");

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

                    matchmakingService.ResetLobbies();
                    matchmakingService.GetLobbies(OnRetrieveSuccess, () =>
                    {
                        __instance.RemoveLoader();
                        Notification.Create(__instance, "Failed to retrieve lobbies...");
                        Sounds.ui_invalid.Play();
                        inputService.EnableAllControllers();
                    });
                    return;
                }

                if (MenuInput.Alt2)
                {
                    if (!lobbies.SingleOrDefault(lobby => lobby.Selected))
                    {
                        Notification.Create(__instance, "You must select a lobby to spectate!");
                        Sounds.ui_invalid.Play();
                        return;
                    }

                    var lobbyToSpectate = lobbies.Single(lobby => lobby.Selected).Lobby;
                    var (canSpectate, message) = (lobbyToSpectate.CanJoin, lobbyToSpectate.CanNotJoinReason);

                    if (!canSpectate)
                    {
                        logger.LogError<MainMenuPatch>($"Can't join lobby because of custom mod: {message}");

                        TowerFall.Sounds.ui_invalid.Play();
                        return;
                    }

                    if (string.IsNullOrEmpty(lobbyToSpectate.RoomId) || string.IsNullOrEmpty(lobbyToSpectate.RoomChatId))
                    {
                        logger.LogError<MainMenuPatch>("Lobby to spectate is null?");

                        return;
                    }

                    inputService.DisableAllControllers();
                    __instance.AddLoader("JOINING LOBBY AS SPECTATOR");

                    matchmakingService.JoinLobby(lobbyToSpectate.RoomId, false, () => OnJoinSuccess(__instance, lobbyToSpectate, false), () => OnFailedToJoinLobby(__instance));
                }
            }

            if (__instance.State.ToDomainModel() == Domain.Models.MenuState.LobbyBuilder)
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

                    var lobby = matchmakingService.GetOwnLobby();
                    lobby.GameData.Variants = variantsToggle.ToArray();
                    lobby.Name = netplayManager.GetNetplayMeta().Name;
                    lobby.RoomChatId = roomChatId;
                    lobby.RoomId = roomId;
                    lobby.Players.Add(new Domain.Models.WebSocket.Player
                    {
                        Name = netplayManager.GetNetplayMeta().Name,
                        Addr = string.Empty,
                        IsHost = true
                    });
                    var widerSetModApi = ServiceCollections.ResolveWiderSetModApi();
                    if (widerSetModApi != null && widerSetModApi.IsWide)
                    {
                        lobby.Mods.Add(new Domain.Models.WebSocket.CustomMod
                        {
                            Name = WiderSetModApiData.Name,
                            Data = new Dictionary<string, string>
                            {
                                { "IsWide", "true" }
                            }
                        });
                    }

                    __instance.AddLoader("CREATING LOBBY...");
                    Sounds.ui_click.Play();

                    inputService.DisableAllControllers();

                    Action onSucess = () =>
                    {
                        inputService.EnableAllControllers();
                        inputService.DisableAllControllerExceptLocal();
                        __instance.RemoveLoader();
                        Sounds.ui_click.Play();
                        __instance.State = MainMenu.MenuState.Rollcall;
                        __instance.BackState = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                        netplayManager.SetRoomAndServerMode(roomUrl, true);
                        matchmakingService.ConnectAndListenToLobby(roomChatUrl);

                        rngService.SetSeed(matchmakingService.GetOwnLobby().GameData.Seed);

                        //Apply length
                        MainMenu.VersusMatchSettings.MatchLength = (MatchSettings.MatchLengths)lobby.GameData.MatchLength;

                        //if (MainMenu.VersusMatchSettings.Variants.ContainsCustomVariant(lobby.GameData.Variants))
                        //{
                        //    Sounds.ui_clickSpecial.Play(160, 4);
                        //    Notification.Create(__instance, $"Be CAREFUL! Custom variants might not work properly", 15, 500);
                        //}
                    };

                    Action onFail = () =>
                    {
                        Notification.Create(__instance, $"Failed to create lobby", 10, 120);
                        inputService.EnableAllControllers();
                        __instance.RemoveLoader();
                        TowerFall.Sounds.ui_invalid.Play();
                        __instance.State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                    };

                    matchmakingService.CreateLobby(onSucess, onFail);
                }
            }
        }

        private static void OnRetrieveSuccess()
        {
            var inputService = ServiceCollections.ResolveInputService();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var logger = ServiceCollections.ResolveLogger();

            inputService.EnableAllControllers();

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

            var reslobbies = matchmakingService.GetLobbies();

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
                        logger.LogError<MainMenuPatch>("Lobby is null?");

                        continue;
                    }

                    if (newLobby.Mods.Count > 0)
                    {
                        foreach (var mod in newLobby.Mods)
                        {
                            if (mod.Name == WiderSetModApiData.Name)
                            {
                                (bool canJoin, string reason) = WiderSetModApiData.CanUseWiderSet(mod.Data, ServiceCollections.ResolveWiderSetModApi());

                                newLobby.CanNotJoinReason = reason;
                                newLobby.CanJoin = canJoin;
                            }
                        }
                    }

                    if (newLobby.CanJoin)
                    {
                        var variantsToggle = variants
                           .Where(v => v is VariantToggle)
                           .Select(v => (v as VariantToggle).Variant.Title)
                       .ToList();

                        bool doesHaveAllVariant = newLobby.GameData.Variants.All(str => variantsToggle.Contains(str));

                        if (!doesHaveAllVariant)
                        {
                            logger.LogError<MainMenuPatch>("Can't join lobby because of custom variants");
                            newLobby.CanNotJoinReason = "MISSING CUSTOM VARIANTS";
                            newLobby.CanJoin = false;
                        }
                    }

                    Action onClick = () =>
                    {
                        if (!newLobby.CanJoin)
                        {
                            logger.LogError<MainMenuPatch>($"Can't join lobby because of custom mod: {newLobby.CanNotJoinReason}");

                            TowerFall.Sounds.ui_invalid.Play();
                            return;
                        }

                        if (string.IsNullOrEmpty(newLobby.RoomId) || string.IsNullOrEmpty(newLobby.RoomChatId))
                        {
                            logger.LogError<MainMenuPatch>("Lobby to join is null?");
                            return;
                        }

                        inputService.DisableAllControllers();

                        self.AddLoader("JOINING LOBBY");

                        matchmakingService.JoinLobby(newLobby.RoomId, true, () => OnJoinSuccess(self, newLobby, true), () => OnFailedToJoinLobby(self));
                    };

                    var but = new LobbyInfos(self, new Vector2(5.0f, maxY), newLobby, onClick, lobbyPanel);

                    lobbies.Add(but);
                    if (i != 0)
                    {
                        lobbies[i - 1].DownItem = but;
                        but.UpItem = lobbies[i - 1];
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

                inputService.EnableAllControllers();
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

                    inputService.EnableAllControllers();
                }
            }
        }

        private static void OnJoinSuccess(MainMenu self, Lobby newLobby, bool isPlayer)
        {
            var inputService = ServiceCollections.ResolveInputService();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var rngService = ServiceCollections.ResolveRngService();

            self.RemoveLoader();
            Sounds.ui_click.Play();

            var roomUrl = $"{Config.SERVER}/room/{newLobby.RoomId}";
            var roomChatUrl = $"{Config.SERVER}/room/{newLobby.RoomChatId}";

            if (isPlayer)
            {
                netplayManager.SetRoomAndServerMode(roomUrl, false);
                netplayManager.UpdatePlayer2Name(newLobby.Players.First(pl => pl.IsHost).Name);
                matchmakingService.ConnectAndListenToLobby(roomChatUrl);
            }

            matchmakingService.UpdateOwnLobby(newLobby);
            inputService.EnableAllControllers();
            inputService.DisableAllControllerExceptLocal();

            rngService.SetSeed(newLobby.GameData.Seed);

            //Apply variant
            MainMenu.VersusMatchSettings.Variants.ApplyVariants(newLobby.GameData.Variants);

            //Apply length
            MainMenu.VersusMatchSettings.MatchLength = (MatchSettings.MatchLengths)newLobby.GameData.MatchLength;

            self.State = MainMenu.MenuState.Rollcall;

            //if (MainMenu.VersusMatchSettings.Variants.ContainsCustomVariant(newLobby.GameData.Variants))
            //{
            //    Sounds.ui_clickSpecial.Play(160, 4);
            //    Notification.Create(self, $"Be CAREFUL! Custom variants might not work properly", 15, 500);
            //}
        }

        private static void OnFailedToJoinLobby(MainMenu self)
        {
            var inputService = ServiceCollections.ResolveInputService();

            Notification.Create(self, $"Failed to join lobby: either full, gone, or already started", 15, 450);

            inputService.EnableAllControllers();
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

        private static void RemoveReplays()
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
