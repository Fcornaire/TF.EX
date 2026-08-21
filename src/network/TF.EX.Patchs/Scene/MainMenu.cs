using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Common.Interop;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Patchs.Entity.MenuItem;
using TextCopy;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    [HarmonyPatch(typeof(MainMenu))]
    public class MainMenuPatch
    {
        private const string LOCAL_BANNER_TITLE = "LOCAL VERSUS";
        private const string ONLINE_BANNER_TITLE = "ONLINE PLAY";
        private const string QUICKPLAY_BANNER_TITLE = "QUICK PLAY";
        private const string LOBBIES_BANNER_TITLE = "LOBBIES";
        private const string PRIVATE_BANNER_TITLE = "PRIVATE";
        private const string PRIVATE_CREATE_BANNER_TITLE = "CREATE";
        private const string PRIVATE_JOIN_BANNER_TITLE = "JOIN";

        private const float WIDERSET_BUTTON_Y = 70f;
        private const float TOGGLE_Y = 135f;
        private const float BANNER_Y = 185f;

        private static readonly string[] LOCAL_BANNER_LINES = { "EVERYONE ON THIS MACHINE.", "GRAB A SECOND CONTROLLER." };
        private static readonly string[] ONLINE_BANNER_LINES = { "ROLLBACK NETCODE SOLO PLAY," };
        private static readonly string[] QUICKPLAY_BANNER_LINES = { "JUMP INTO A RANDOM MATCH.", "CLOSEST OPPONENTS FIRST." };
        private static readonly string[] LOBBIES_BANNER_LINES = { "BROWSE PUBLIC LOBBIES", "OR CREATE YOUR OWN." };
        private static readonly string[] PRIVATE_BANNER_LINES = { "LOCKED LOBBY, JOINABLE", "ONLY WITH A CODE." };
        private static readonly string[] PRIVATE_CREATE_BANNER_LINES = { "HOST A PRIVATE LOBBY AND", "SHARE ITS CODE WITH FRIENDS." };
        private static readonly string[] PRIVATE_JOIN_BANNER_LINES = { "ENTER A CODE TO JOIN", "A FRIEND'S PRIVATE LOBBY." };

        private static bool hasShowedWarning = false;



        private static List<LobbyInfos> lobbies = new List<LobbyInfos>();
        private static LobbyPanel lobbyPanel = null;
        private static Monocle.Entity spectateEntityButton = null;
        private static Monocle.Entity createEntityButton = null;
        private static Monocle.Entity copyCodeGuideEntity = null;

        private static LobbyVersusModeButton lobbyVersusModeButton = null;
        private static LobbyVersusCoinButton lobbyVersusCoinButton = null;
        private static LobbyVersusMapButton lobbyVersusMapButton = null;
        private static LobbyVersusPlayerCountButton lobbyVersusPlayerCountButton = null;
        private static LobbyVarianText lobbyVarianText = null;
        private static List<VariantItem> variants = new List<VariantItem>();

        private static DateTime nextServerPull = DateTime.UtcNow;
        private static DateTime nextJoinCodeAttempt = DateTime.UtcNow;

        private static Text noMsg;

        [HarmonyPrefix]
        [HarmonyPatch("CallStateFunc")]
        public static bool MainMenu_CallStateFunc(MainMenu __instance, string name, MainMenu.MenuState state)
        {
            switch (state.ToDomainModel())
            {
                case Domain.Models.MenuState.LobbyBrowser:
                    HandleLobbyBrowser(__instance, name);
                    return false;
                case Domain.Models.MenuState.LobbyBuilder:
                    HandleLobbyBuilder(__instance, name);
                    return false;
                case Domain.Models.MenuState.VersusSelect:
                    HandleVersusSelect(__instance, name);
                    return false;
                case Domain.Models.MenuState.NetplaySelect:
                    if (name == "Create")
                    {
                        CreateNetplaySelect(__instance);
                    }
                    return false;
                case Domain.Models.MenuState.PrivateSelect:
                    if (name == "Create")
                    {
                        CreatePrivateSelect(__instance);
                    }
                    return false;
                case Domain.Models.MenuState.PrivateJoinCode:
                    if (name == "Create")
                    {
                        CreatePrivateJoinCode(__instance);
                    }
                    return false;
                case Domain.Models.MenuState.QuickPlaySearch:
                    HandleQuickPlaySearch(__instance, name);
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
                default:
                    return true;
            }
        }

        private static void HandleVersusSelect(MainMenu self, string name)
        {
            if (name == "Create")
            {
                CreateVersusSelect(self);
            }
        }

        private static void CreateVersusSelect(MainMenu self)
        {
            self.BackState = MainMenu.MenuState.Main;

            var local = new LocalVersusButton(new Vector2(100f, 90f), new Vector2(-160f, 120f));
            var online = new OnlineVersusButton(new Vector2(220f, 90f), new Vector2(560f, 120f));

            local.RightItem = online;
            online.LeftItem = local;

            var banner = BuildVersusSelectBanner();
            banner.Track(() => local.Selected, LOCAL_BANNER_TITLE, LOCAL_BANNER_LINES);
            banner.Track(() => online.Selected, ONLINE_BANNER_TITLE, ONLINE_BANNER_LINES);

            self.Add(new TowerFall.MenuItem[] { local, online, banner });
            self.ToStartSelected = local;

            DynamicData.For(self).Invoke("TweenBGCameraToY", 1);
        }

        private static void CreateNetplaySelect(MainMenu self)
        {
            if (!MainMenu.VersusMatchSettings.ApplyNetplayMode())
            {
                ServiceCollections.ResolveLogger().LogError<MainMenuPatch>("Netplay game mode is not registered", null);
            }

            MatchVariantsPatchs.DisableUnauthorized(MainMenu.VersusMatchSettings.Variants);

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            matchmakingService.ResetLobby();
            matchmakingService.ResetPeer();
            ServiceCollections.ResolveInputService().EnableAllControllers();

            Domain.Context.MenuReturn.NetplayEntry = ResolveNetplayEntryState(self) ?? Domain.Context.MenuReturn.NetplayEntry;
            Domain.Context.LobbyBuilderContext.IsPrivate = false;
            self.BackState = Domain.Context.MenuReturn.NetplayEntry ?? MainMenu.MenuState.Main;

            var quickPlay = new NetplayMenuButton(new Vector2(65f, 90f), new Vector2(-160f, 120f), "QUICK PLAY", "2-4 ARCHERS", TFGame.MenuAtlas["GoldenArrow"], () =>
            {
                self.State = Domain.Models.MenuState.QuickPlaySearch.ToTFModel();
            });

            var lobbyList = new NetplayMenuButton(new Vector2(175f, 90f), new Vector2(160f, -80f), "LOBBIES", "BROWSE GAMES", TFGame.MenuAtlas["browseWorkshopButton"], () =>
            {
                self.State = Domain.Models.MenuState.LobbyBrowser.ToTFModel();
            });

            var privateMatch = new NetplayMenuButton(new Vector2(265f, 90f), new Vector2(560f, 120f), "PRIVATE", "INVITE ONLY", TFGame.MenuAtlas["map/towerLock"], () =>
            {
                self.State = Domain.Models.MenuState.PrivateSelect.ToTFModel();
            });

            quickPlay.RightItem = lobbyList;
            lobbyList.LeftItem = quickPlay;
            lobbyList.RightItem = privateMatch;
            privateMatch.LeftItem = lobbyList;

            var banner = BuildVersusSelectBanner();
            banner.Track(() => quickPlay.Selected, QUICKPLAY_BANNER_TITLE, QUICKPLAY_BANNER_LINES);
            banner.Track(() => lobbyList.Selected, LOBBIES_BANNER_TITLE, LOBBIES_BANNER_LINES);
            banner.Track(() => privateMatch.Selected, PRIVATE_BANNER_TITLE, PRIVATE_BANNER_LINES);

            self.Add(new TowerFall.MenuItem[] { quickPlay, lobbyList, privateMatch, banner });
            self.ToStartSelected = quickPlay;

            DynamicData.For(self).Invoke("TweenBGCameraToY", 1);
        }

        private static void CreatePrivateSelect(MainMenu self)
        {
            self.BackState = Domain.Models.MenuState.NetplaySelect.ToTFModel();

            var create = new NetplayMenuButton(new Vector2(100f, 90f), new Vector2(-160f, 120f), "CREATE", "HOST A LOBBY", TFGame.MenuAtlas["editorQuill"], () =>
            {
                Domain.Context.LobbyBuilderContext.IsPrivate = true;
                self.State = Domain.Models.MenuState.LobbyBuilder.ToTFModel();
            });

            var join = new NetplayMenuButton(new Vector2(220f, 90f), new Vector2(560f, 120f), "JOIN", "ENTER A CODE", TFGame.MenuAtlas["controls/keyboard/icon"], () =>
            {
                self.State = Domain.Models.MenuState.PrivateJoinCode.ToTFModel();
            });

            create.RightItem = join;
            join.LeftItem = create;

            var banner = BuildVersusSelectBanner();
            banner.Track(() => create.Selected, PRIVATE_CREATE_BANNER_TITLE, PRIVATE_CREATE_BANNER_LINES);
            banner.Track(() => join.Selected, PRIVATE_JOIN_BANNER_TITLE, PRIVATE_JOIN_BANNER_LINES);

            self.Add(new TowerFall.MenuItem[] { create, join, banner });
            self.ToStartSelected = create;

            DynamicData.For(self).Invoke("TweenBGCameraToY", 1);
        }

        private static void HandleQuickPlaySearch(MainMenu self, string name)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (name == "Destroy")
            {
                self.RemoveLoader();

                if (matchmakingService.IsSearchingQuickPlay())
                {
                    Task.Run(matchmakingService.ExitQuickPlay);
                }

                return;
            }

            self.BackState = Domain.Models.MenuState.NetplaySelect.ToTFModel();
            self.AddLoader("SEARCHING FOR OPPONENTS...");
            self.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "CANCEL");

            Task.Run(() => matchmakingService.EnterQuickPlay(
                _ => { },
                lobby => OnQuickPlayMatched(self, lobby),
                message =>
                {
                    self.RemoveLoader();
                    TowerFall.Sounds.ui_invalid.Play();
                    Notification.Create(self, message, 10, 300);
                    self.State = Domain.Models.MenuState.NetplaySelect.ToTFModel();
                }));
        }

        private static void OnQuickPlayMatched(MainMenu self, Lobby lobby)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();

            if (self.State.ToDomainModel() != Domain.Models.MenuState.QuickPlaySearch)
            {
                Task.Run(() => matchmakingService.LeaveLobby(matchmakingService.ResetLobby, matchmakingService.ResetLobby));
                return;
            }

            self.RemoveLoader();
            Sounds.ui_click.Play();
            Notification.Create(self, "OPPONENT FOUND", 10, 200);

            var localPeerId = matchmakingService.GetRoomPeerId();
            var isHost = lobby.Players.Any(pl => pl.IsHost && pl.RoomPeerId == localPeerId);
            var roomUrl = $"{Config.SERVER}/room/{lobby.RoomId}";

            netplayManager.SetRoomAndServerMode($"{roomUrl}?peer={localPeerId}", isHost);

            if (!isHost)
            {
                netplayManager.UpdatePlayer2Name(lobby.Players.First(pl => pl.IsHost).Name);
            }

            inputService.EnableAllControllers();
            inputService.DisableAllControllerExceptLocal();

            StateApi.Current.SetSeed(lobby.GameData.Seed);

            MainMenu.VersusMatchSettings.Variants.ApplyNetplayVariantRules();
            MainMenu.VersusMatchSettings.MatchLength = (MatchSettings.MatchLengths)lobby.GameData.MatchLength;

            self.State = MainMenu.MenuState.Rollcall;
        }

        private static void CreatePrivateJoinCode(MainMenu self)
        {
            self.BackState = Domain.Models.MenuState.PrivateSelect.ToTFModel();

            var entry = new JoinCodeEntry(new Vector2(160f, 105f), code => SubmitJoinCode(self, code), ReadClipboard);

            self.Add(entry);
            self.ToStartSelected = entry;

            self.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "JOIN");
            self.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "RETURN");
            self.ButtonGuideC.SetDetails(MenuButtonGuide.ButtonModes.Alt, "PASTE");

            DynamicData.For(self).Invoke("TweenBGCameraToY", 1);
        }

        private static string ReadClipboard()
        {
            try
            {
                return ClipboardService.GetText() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void SubmitJoinCode(MainMenu self, string code)
        {
            if (DateTime.UtcNow < nextJoinCodeAttempt)
            {
                TowerFall.Sounds.ui_invalid.Play();
                return;
            }

            nextJoinCodeAttempt = DateTime.UtcNow.AddSeconds(3);

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var inputService = ServiceCollections.ResolveInputService();

            inputService.DisableAllControllers();
            self.AddLoader("JOINING LOBBY");
            Sounds.ui_click.Play();

            matchmakingService.JoinPrivate(code,
                lobby =>
                {
                    var (canJoin, reason) = EvaluateLobbyCompatibility(lobby);

                    if (canJoin)
                    {
                        OnJoinSuccess(self, lobby, true);
                        return;
                    }

                    ServiceCollections.ResolveLogger().LogError<MainMenuPatch>($"Refusing private lobby: {reason}");

                    Action bail = () =>
                    {
                        matchmakingService.ResetLobby();
                        matchmakingService.ResetPeer();
                        inputService.EnableAllControllers();
                        self.RemoveLoader();
                        TowerFall.Sounds.ui_invalid.Play();
                        Notification.Create(self, reason, 10, 300);
                    };

                    Task.Run(() => matchmakingService.LeaveLobby(bail, bail));
                },
                () =>
                {
                    inputService.EnableAllControllers();
                    self.RemoveLoader();
                    TowerFall.Sounds.ui_invalid.Play();
                    Notification.Create(self, "Invalid or expired code", 10, 300);
                });
        }

        private static (bool canJoin, string reason) EvaluateLobbyCompatibility(Lobby newLobby)
        {
            var widerSetModApi = ServiceCollections.ResolveWiderSetModApi();

            if (newLobby.Mods.Count > 0)
            {
                foreach (var mod in newLobby.Mods)
                {
                    if (mod.Name == WiderSetModApiData.Name)
                    {
                        (bool canJoin, string reason) = WiderSetModApiData.CanUseWiderSet(mod.Data, widerSetModApi);

                        if (!canJoin)
                        {
                            return (false, reason);
                        }
                    }
                }
            }
            else if (widerSetModApi != null && widerSetModApi.IsWide)
            {
                return (false, "WIDERSET MOD ON OFF REQUIRED");
            }

            var availableVariants = MainMenu.VersusMatchSettings.Variants.Variants
                .Select(variant => variant.Title)
                .ToList();

            if (!newLobby.GameData.Variants.All(availableVariants.Contains))
            {
                return (false, "MISSING CUSTOM VARIANTS");
            }

            return (true, "");
        }

        [HarmonyPostfix]
        [HarmonyPatch("CallStateFunc")]
        public static void MainMenu_CallStateFunc_Postfix(MainMenu __instance, string name, MainMenu.MenuState state)
        {
            if (state == MainMenu.MenuState.Rollcall)
            {
                HandleCopyCodeGuide(__instance, name);
            }

            if (name != "Create" || !WiderSetMenu.IsSelectionState(state))
            {
                return;
            }

            AddOnlineToWiderSetSelection(__instance);
        }

        private static void HandleCopyCodeGuide(MainMenu self, string name)
        {
            if (copyCodeGuideEntity != null)
            {
                copyCodeGuideEntity.RemoveSelf();
                copyCodeGuideEntity = null;
            }

            if (name != "Create")
            {
                return;
            }

            var ownLobby = ServiceCollections.ResolveMatchmakingService().GetOwnLobby();

            if (!ownLobby.IsPrivate || string.IsNullOrEmpty(ownLobby.JoinCode))
            {
                return;
            }

            copyCodeGuideEntity = new Monocle.Entity(-1);
            var copyCodeGuide = new MenuButtonGuide(0);
            copyCodeGuide.SetDetails(MenuButtonGuide.ButtonModes.Alt2, "COPY CODE");
            copyCodeGuideEntity.Add(copyCodeGuide);
            self.Add(copyCodeGuideEntity);
        }

        private static void AddOnlineToWiderSetSelection(MainMenu self)
        {
            try
            {
                var existing = self.GetAllToBeSpawned<MainModeButton>().OrderBy(button => button.X).ToList();

                if (existing.Count == 0)
                {
                    return;
                }

                foreach (var button in existing)
                {
                    button.Position = new Vector2(button.X, WIDERSET_BUTTON_Y);
                    Traverse.Create(button).Field("tweenTo").SetValue(button.Position);
                }

                var toggle = new OnlinePlayToggle(new Vector2(160f, TOGGLE_Y), new Vector2(-160f, TOGGLE_Y));

                foreach (var button in existing)
                {
                    button.DownItem = toggle;
                }

                toggle.UpItem = existing[0];

                var banner = BuildVersusSelectBanner();
                banner.Track(() => OnlinePlayToggle.IsOn, ONLINE_BANNER_TITLE, ONLINE_BANNER_LINES);

                self.Add(new TowerFall.MenuItem[] { toggle, banner });
            }
            catch (Exception ex)
            {
                var logger = ServiceCollections.ResolveLogger();
                logger.LogError<MainMenuPatch>("Error when adding the online toggle to the WiderSet selection", ex);
            }
        }

        private static MainMenu.MenuState? ResolveNetplayEntryState(MainMenu self)
        {
            if (WiderSetMenu.IsSelectionState(self.OldState) || self.OldState.ToDomainModel() == Domain.Models.MenuState.VersusSelect)
            {
                return self.OldState;
            }

            return null;
        }

        private static VersusSelectBanner BuildVersusSelectBanner()
        {
            return new VersusSelectBanner(new Vector2(160f, BANNER_Y)) { Depth = -1000 };
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

                if (lobbyVersusPlayerCountButton != null)
                {
                    lobbyVersusPlayerCountButton.RemoveSelf();
                    lobbyVersusPlayerCountButton = null;
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
            self.BackState = Domain.Context.LobbyBuilderContext.IsPrivate
                ? Domain.Models.MenuState.PrivateSelect.ToTFModel()
                : Domain.Models.MenuState.LobbyBrowser.ToTFModel();

            if (variants.Count == 0)
            {
                variants = MainMenu.VersusMatchSettings.Variants.BuildMenu(self, out _, out self.MaxUICameraY);
            }

            lobbyVersusModeButton = new LobbyVersusModeButton(new Vector2(160f, 90f), new Vector2(-100f, 90f));
            self.Add(lobbyVersusModeButton);

            lobbyVersusMapButton = new LobbyVersusMapButton(new Vector2(160f, 130f), new Vector2(-100f, 210f));
            self.Add(lobbyVersusMapButton);

            lobbyVersusPlayerCountButton = new LobbyVersusPlayerCountButton(new Vector2(160f, 190f), new Vector2(420f, 105f));
            self.Add(lobbyVersusPlayerCountButton);

            lobbyVersusCoinButton = new LobbyVersusCoinButton(new Vector2(160f, 220f), new Vector2(420f, 135f));
            self.Add(lobbyVersusCoinButton);

            lobbyVarianText = new LobbyVarianText(new Vector2(160f, 250f), new Vector2(-120f, 135f));
            self.Add(lobbyVarianText);

            foreach (var variant in variants)
            {
                variant.Position.Y += 230;
            }

            self.MaxUICameraY += 260;

            var dynMainMenu = DynamicData.For(self);
            dynMainMenu.Invoke("TweenBGCameraToY", 3);
            dynMainMenu.Set("ToStartSelected", lobbyVersusModeButton);
            lobbyVersusModeButton.DownItem = lobbyVersusMapButton;
            lobbyVersusMapButton.UpItem = lobbyVersusModeButton;
            lobbyVersusMapButton.DownItem = lobbyVersusPlayerCountButton;
            lobbyVersusPlayerCountButton.UpItem = lobbyVersusMapButton;
            lobbyVersusPlayerCountButton.DownItem = lobbyVersusCoinButton;
            lobbyVersusCoinButton.UpItem = lobbyVersusPlayerCountButton;
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
                createButton.SetDetails(MenuButtonGuide.ButtonModes.Start,Domain.Context.LobbyBuilderContext.IsPrivate ? "CREATE PRIVATE LOBBY" : "CREATE LOBBY");
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

        private static void CreateLobbyBrowser(MainMenu self)
        {
            if (!MainMenu.VersusMatchSettings.ApplyNetplayMode())
            {
                ServiceCollections.ResolveLogger().LogError<MainMenuPatch>("Netplay game mode is not registered", null);
            }

            MatchVariantsPatchs.DisableUnauthorized(MainMenu.VersusMatchSettings.Variants);

            if (variants.Count == 0)
            {
                variants = MainMenu.VersusMatchSettings.Variants.BuildMenu(self, out _, out self.MaxUICameraY);
            }

            self.AddLoader("FINDING LOBBIES...");

            self.BackState = Domain.Models.MenuState.NetplaySelect.ToTFModel();

            Task.Run(async () =>
            {
                try
                {
                    var inputService = ServiceCollections.ResolveInputService();
                    var matchmakingService = ServiceCollections.ResolveMatchmakingService();
                    matchmakingService.ResetLobby();

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

        [HarmonyPostfix]
        [HarmonyPatch("CreateMain")]
        public static void MainMenu_CreateMain(MainMenu __instance)
        {
            OnlinePlayToggle.IsOn = false;

            var mode = TowerFall.MainMenu.VersusMatchSettings?.Mode;
            var lobby = ServiceCollections.ResolveMatchmakingService().GetOwnLobby();
            if ((mode != null && mode.Value.IsNetplay()) || !lobby.IsEmpty)
            {
                var widerSetModApi = ServiceCollections.ResolveWiderSetModApi();
                if (widerSetModApi != null && widerSetModApi.IsWide)
                {
                    widerSetModApi.IsWide = false;
                }
            }
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
            Monocle.Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, __instance.UILayer.Camera.Matrix);

            if (__instance.State == MainMenu.MenuState.Rollcall
                && TowerFall.MainMenu.VersusMatchSettings?.Mode.IsNetplay() == true)
            {
                var matchmakingService = ServiceCollections.ResolveMatchmakingService();

                var localPeerId = matchmakingService.GetRoomPeerId();
                var lobbyPlayers = matchmakingService.GetOwnLobby().Players.ToArray();
                var localSeat = matchmakingService.GetLocalSeat();

                foreach (var rollcallElement in __instance.GetAll<RollcallElement>())
                {
                    var seat = DynamicData.For(rollcallElement).Get<int>("playerIndex");

                    var seated = lobbyPlayers.FirstOrDefault(player => player.Seat == seat);

                    if (seated == null)
                    {
                        continue;
                    }

                    var isShown = matchmakingService.IsSpectator()
                        ? seated.IsHost
                        : seat != localSeat && seated.RoomPeerId != localPeerId;

                    if (!isShown)
                    {
                        continue;
                    }

                    var latency = matchmakingService.GetPingTo(seated);
                    var label = $"{latency} MS";

                    Monocle.Draw.OutlineTextCentered(TFGame.Font, label, RollcallLayout.PingAt(rollcallElement), GetPingColor(latency), Color.Black);
                }

                RenderWaitingForHost(matchmakingService);
                RenderQuickPlayStarting(matchmakingService);
            }

            if (__instance.State.ToDomainModel() == Domain.Models.MenuState.QuickPlaySearch)
            {
                var searching = ServiceCollections.ResolveMatchmakingService().GetSearchingCount();

                if (searching > 0)
                {
                    var label = searching == 1 ? "1 PLAYER SEARCHING" : $"{searching} PLAYERS SEARCHING";
                    Monocle.Draw.OutlineTextCentered(TFGame.Font, label, new Vector2(160f, 160f), Color.White, Color.Black);
                }
            }

            Monocle.Draw.SpriteBatch.End();
        }

        private static void RenderQuickPlayStarting(Domain.Ports.IMatchmakingService matchmakingService)
        {
            if (!matchmakingService.IsQuickPlayStarting())
            {
                return;
            }

            var position = new Vector2(160f, 225f);

            Monocle.Draw.OutlineTextureCentered(TFGame.MenuAtlas["portraits/readyBanner"], position, Color.White);
            Monocle.Draw.OutlineTextCentered(TFGame.Font, "MATCH STARTING...", position - Vector2.UnitY * 2f, Color.White, Color.Black);
        }

        private static bool Alt2Pressed()
        {
            if (MenuInput.Alt2)
            {
                return true;
            }

            foreach (var input in TFGame.PlayerInputs)
            {
                if (input != null && input.MenuAlt2)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CopyPrivateCodeToClipboard(MainMenu self, Domain.Ports.IMatchmakingService matchmakingService)
        {
            var ownLobby = matchmakingService.GetOwnLobby();
            var logger = ServiceCollections.ResolveLogger();

            if (!ownLobby.IsPrivate || string.IsNullOrEmpty(ownLobby.JoinCode))
            {
                return;
            }

            try
            {
                ClipboardService.SetText(ownLobby.JoinCode);
                Sounds.ui_click.Play();
                Notification.Create(self, "CODE COPIED");
                logger.LogDebug<MainMenuPatch>("Join code copied to clipboard");
            }
            catch (Exception ex)
            {
                logger.LogError<MainMenuPatch>("Failed to copy join code to clipboard", ex);
                TowerFall.Sounds.ui_invalid.Play();
            }
        }

        private static void RenderWaitingForHost(Domain.Ports.IMatchmakingService matchmakingService)
        {
            if (!matchmakingService.IsWaitingForHostStart())
            {
                return;
            }

            var position = new Vector2(160f, 225f);

            Monocle.Draw.OutlineTextureCentered(TFGame.MenuAtlas["portraits/readyBanner"], position, Color.White);
            Monocle.Draw.OutlineTextCentered(TFGame.Font, "WAITING FOR HOST", position - Vector2.UnitY * 2f, Color.White, Color.Black);
        }

        private static Color GetPingColor(int latency)
        {
            switch (latency)
            {
                case var n when (n < 60):
                    return Color.LightGreen;
                case var n when (n < 120):
                    return Color.GreenYellow;
                case var n when (n < 150):
                    return Color.OrangeRed;
                default:
                    return Color.Red;
            }
        }

        // [HarmonyPostfix]
        // [HarmonyPatch("Update")]
        // public static void MainMenu_Update_Prefix(MainMenu __instance)
        // {
        //     var netplayManager = ServiceCollections.ResolveNetplayManager();

        //     //if (TowerFall.MainMenu.VersusMatchSettings != null && __instance.State == TowerFall.MainMenu.MenuState.VersusOptions)
        //     //{
        //     //    if (MenuInput.Back)
        //     //    {
        //     //        __instance.State = TowerFall.MainMenu.MenuState.Main;
        //     //        return false;
        //     //    }
        //     //}
        // }

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
            var ownLobby = ServiceCollections.ResolveMatchmakingService().GetOwnLobby();
            if (!ownLobby.IsEmpty)
            {
                TowerFall.MainMenu.NoGamepadUpdates = true;
            }
            else if (TowerFall.MainMenu.VersusMatchSettings?.Mode.IsNetplay() == true)
            {
                TowerFall.MainMenu.NoGamepadUpdates = false;
            }

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var replayService = ServiceCollections.ResolveReplayService();
            var inputService = ServiceCollections.ResolveInputService();
            var logger = ServiceCollections.ResolveLogger();

            if (matchmakingService.GetOwnLobby().IsEmpty)
            {
                inputService.ObserveLocalDevice();
            }
            else
            {
                matchmakingService.ReconcileRollcallIfPending();
            }

            if (__instance.State == MainMenu.MenuState.Rollcall && Alt2Pressed())
            {
                CopyPrivateCodeToClipboard(__instance, matchmakingService);
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

                    if (string.IsNullOrEmpty(lobbyToSpectate.RoomId))
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

                    var roomUrl = $"{Config.SERVER}/room/{roomId}";

                    var lobby = matchmakingService.GetOwnLobby();
                    lobby.GameData.Variants = variantsToggle.ToArray();
                    lobby.Name = netplayManager.GetNetplayMeta().Name;
                    lobby.RoomId = roomId;
                    lobby.Kind = (Domain.Context.LobbyBuilderContext.IsPrivate ? LobbyKind.Private : LobbyKind.Standard).ToString();
                    lobby.Players.Add(new Domain.Models.WebSocket.Player
                    {
                        Name = netplayManager.GetNetplayMeta().Name,
                        Addr = string.Empty,
                        IsHost = true
                    });
                    var widerSetModApi = ServiceCollections.ResolveWiderSetModApi();
                    if (widerSetModApi != null)
                    {
                        lobby.Mods.Add(new Domain.Models.WebSocket.CustomMod
                        {
                            Name = WiderSetModApiData.Name,
                            Data = new Dictionary<string, string>
                            {
                                { "IsWide", widerSetModApi.IsWide.ToString() }
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
                        __instance.BackState = Domain.Models.MenuState.NetplaySelect.ToTFModel();
                        netplayManager.SetRoomAndServerMode($"{roomUrl}?peer={matchmakingService.GetRoomPeerId()}", true);

                        StateApi.Current.SetSeed(matchmakingService.GetOwnLobby().GameData.Seed);

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
                        __instance.State = Domain.Context.LobbyBuilderContext.IsPrivate
                            ? Domain.Models.MenuState.PrivateSelect.ToTFModel()
                            : Domain.Models.MenuState.LobbyBrowser.ToTFModel();
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
                    else
                    {
                        var widerSetModApi = ServiceCollections.ResolveWiderSetModApi();
                        if (widerSetModApi != null && widerSetModApi.IsWide)
                        {
                            newLobby.CanNotJoinReason = "WIDERSET MOD ON OFF REQUIRED";
                            newLobby.CanJoin = false;
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

                        if (string.IsNullOrEmpty(newLobby.RoomId))
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

            self.RemoveLoader();
            Sounds.ui_click.Play();

            var roomUrl = $"{Config.SERVER}/room/{newLobby.RoomId}";

            if (isPlayer)
            {
                netplayManager.SetRoomAndServerMode($"{roomUrl}?peer={matchmakingService.GetRoomPeerId()}", false);
                netplayManager.UpdatePlayer2Name(newLobby.Players.First(pl => pl.IsHost).Name);
            }

            matchmakingService.UpdateOwnLobby(newLobby);
            inputService.EnableAllControllers();
            inputService.DisableAllControllerExceptLocal();

            StateApi.Current.SetSeed(newLobby.GameData.Seed);

            MainMenu.VersusMatchSettings.Variants.ApplyVariants(newLobby.GameData.Variants);

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
    }


}
