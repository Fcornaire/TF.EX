using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.Replay.Domain;
using TF.Replay.Domain.CustomComponent;
using TF.Replay.Domain.Extensions;
using TF.Replay.Domain.Models;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    [HarmonyPatch(typeof(MainMenu))]
    public class MainMenuPatch
    {
        private static List<ReplayInfos> _replays = new List<ReplayInfos>();
        private static ReplaysPanel _replaysPanel = null;
        private static Text _noMsg;
        private static Text _loadingMsg;
        private static Text _monthMsg;
        private static bool _loading;

        private static Action _pendingBuild;
        private static MainMenu _owner;
        private static int _generation;

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void MainMenu_Update(MainMenu __instance)
        {
            if ((int)__instance.State == ReplayMenuState.ReplaysBrowser)
            {
                Interlocked.Exchange(ref _pendingBuild, null)?.Invoke();
            }

            if ((int)__instance.State != ReplayMenuState.ReplaysBrowser || _loading)
            {
                return;
            }

            if (_replays.Count > 0 && !_replays.Any(replay => replay.Selected))
            {
                _replays[0].Selected = true;
                DynamicData.For(__instance).Set("ToStartSelected", _replays[0]);
            }

            var step = MenuInput.Alt ? -1 : MenuInput.Alt2 ? 1 : 0;

            if (step == 0)
            {
                return;
            }

            var service = ServiceCollections.ResolveReplayService();
            var months = service.GetMonths();

            if (months.Length < 2)
            {
                return;
            }

            var index = Array.IndexOf(months, service.CurrentMonth);
            var next = ((index < 0 ? 0 : index) + step + months.Length) % months.Length;

            service.SetMonth(months[next]);
            Sounds.ui_click.Play();

            HandleReplayBrowser(__instance, "Destroy");
            CreateReplay(__instance);
        }

        private static string MonthLabel(string month)
            => string.IsNullOrEmpty(month) ? "UNSORTED" : month;

        private static void SetGuides(MainMenu self, bool canSwitchMonth)
        {
            self.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "LAUNCH");
            self.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "EXIT");

            if (canSwitchMonth)
            {
                self.ButtonGuideC.SetDetails(MenuButtonGuide.ButtonModes.Alt, "PREV MONTH");
                self.ButtonGuideD.SetDetails(MenuButtonGuide.ButtonModes.Alt2, "NEXT MONTH");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("CallStateFunc")]
        public static bool MainMenu_CallStateFunc(MainMenu __instance, string name, MainMenu.MenuState state)
        {
            if ((int)state != ReplayMenuState.ReplaysBrowser)
            {
                return true;
            }

            HandleReplayBrowser(__instance, name);
            return false;
        }

        private static void ShowLoading(Monocle.Scene scene, string message)
        {
            HideLoading();

            _loadingMsg = new Text(message);
            _loadingMsg.Position = new Vector2(100, 80);
            scene.Add(_loadingMsg);
        }

        private static void HideLoading()
        {
            if (_loadingMsg == null)
            {
                return;
            }

            if (_loadingMsg.Scene != null)
            {
                _loadingMsg.RemoveSelf();
            }

            _loadingMsg = null;
        }

        private static void HandleReplayBrowser(MainMenu self, string name)
        {
            if (name == "Destroy")
            {
                Interlocked.Increment(ref _generation);
                Interlocked.Exchange(ref _pendingBuild, null);

                self.DeleteAll<ReplayInfos>();

                if (_replaysPanel != null)
                {
                    _replaysPanel.RemoveSelf();
                    _replaysPanel = null;
                }

                _replays.Clear();

                if (_noMsg != null)
                {
                    _noMsg.RemoveSelf();
                    _noMsg = null;
                }

                if (_monthMsg != null)
                {
                    _monthMsg.RemoveSelf();
                    _monthMsg = null;
                }

                HideLoading();
                _loading = false;

                return;
            }

            CreateReplay(self);
        }

        private static void ForgetDeadBrowser(MainMenu self)
        {
            if (_owner == self)
            {
                return;
            }

            _owner = self;

            Interlocked.Increment(ref _generation);
            Interlocked.Exchange(ref _pendingBuild, null);

            _replays.Clear();
            _replaysPanel = null;
            _noMsg = null;
            _monthMsg = null;
            _loadingMsg = null;
            _loading = false;
        }

        private static void CreateReplay(MainMenu self)
        {
            ForgetDeadBrowser(self);

            var replayService = ServiceCollections.ResolveReplayService();
            var logger = ServiceCollections.ResolveLogger();
            ServiceCollections.SetInputEnabled(false);
            _loading = true;

            ShowLoading(self, "LOADING REPLAYS...");

            self.BackState = MainMenu.MenuState.Main;

            var months = replayService.GetMonths();

            if (months.Length > 0 && Array.IndexOf(months, replayService.CurrentMonth) < 0)
            {
                replayService.SetMonth(months[0]);
            }

            _monthMsg = new Text($"< {MonthLabel(replayService.CurrentMonth)} >");
            _monthMsg.Position = new Vector2(-70f, -36f);
            self.Add(_monthMsg);

            var generation = Interlocked.Increment(ref _generation);

            Task.Run(async () =>
            {
                try
                {
                    var replays = (await replayService.LoadAndGetReplays()).ToArray();

                    Publish(generation, () => BuildBrowser(self, replays, months.Length > 1, logger));
                }
                catch (Exception ex)
                {
                    logger.LogError("Error when loading replays: {error}", ex);

                    Publish(generation, GiveUpLoading);
                }
            });
        }

        private static void Publish(int generation, Action build)
        {
            if (Volatile.Read(ref _generation) != generation)
            {
                return;
            }

            Interlocked.Exchange(ref _pendingBuild, build);
        }

        private static void BuildBrowser(MainMenu self, TF.Replay.Domain.Models.Replay[] replays,
                                         bool severalMonths, Microsoft.Extensions.Logging.ILogger logger)
        {
            var dynMainMenu = DynamicData.For(self);
            var dynCamera = DynamicData.For(self.UILayer);

            _replays.Clear();

            var maxY = 50.0f;

            if (replays.Length == 0)
            {
                _noMsg = new Text(severalMonths
                    ? "NOTHING THIS MONTH - ALT TO SWITCH"
                    : "PLAY SOME GAMES FIRST!");
                _noMsg.Position = new Vector2(100, 100);
                self.Add(_noMsg);

                GiveUpLoading();
                SetGuides(self, severalMonths);

                return;
            }

            _replaysPanel = new ReplaysPanel(225, 88);

            for (int i = 0; i < replays.Length; i++)
            {
                if (replays[i] == null)
                {
                    logger.LogError("Replay is null ?");

                    continue;
                }

                var but = new ReplayInfos(self, new Vector2(5.0f, maxY), replays[i], LaunchRecord, _replaysPanel);

                _replays.Add(but);

                if (i != 0)
                {
                    _replays[i - 1].DownItem = but;
                    but.UpItem = _replays[i - 1];
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

            GiveUpLoading();
            SetGuides(self, severalMonths);

            Sounds.ui_startGame.Play();
        }

        private static void GiveUpLoading()
        {
            HideLoading();
            ServiceCollections.SetInputEnabled(true);
            _loading = false;
        }

        private static void LaunchRecord()
        {
            var toLaunch = _replays.ToArray().FirstOrDefault(replay => replay.Selected);

            if (toLaunch == null)
            {
                return;
            }

            var currentSong = Monocle.Music.CurrentSong;
            Monocle.Music.Stop();
            Sounds.ui_mapZoom.Play();

            Task.Run(async () =>
            {
                ServiceCollections.SetInputEnabled(false);

                ShowLoading(TFGame.Instance.Scene, "LOADING REPLAY...");

                string failure;

                try
                {
                    failure = ServiceCollections.LaunchReplay(toLaunch.OriginalName, currentSong);
                }
                catch (Exception e)
                {
                    ServiceCollections.ResolveLogger().LogError("Could not start {name}: {error}", toLaunch.OriginalName, e);

                    failure = "SEE THE LOG";
                }

                HideLoading();

                if (failure != null)
                {
                    ServiceCollections.Notify($"CANNOT START REPLAY: {failure}".ToUpperInvariant());
                    ServiceCollections.SetInputEnabled(true);
                    toLaunch.Reenable();
                    return;
                }

                ServiceCollections.SetInputEnabled(true);
                ServiceCollections.EnsureFakeControllers();
            });
        }

        [HarmonyPostfix]
        [HarmonyPatch("CreateMain")]
        public static void MainMenu_CreateMain(MainMenu __instance)
        {
            var trials = __instance.GetToBeSpawned<TrialsButton>();
            var archivesButton = __instance.GetToBeSpawned<ArchivesButton>();
            var workshopButton = __instance.GetToBeSpawned<WorkshopButton>();
            var fightButton = __instance.GetToBeSpawned<FightButton>();
            var coopButton = __instance.GetToBeSpawned<CoOpButton>();
            var bladesButton = __instance.GetAllToBeSpawned<BladeButton>();
            var modButton = bladesButton.FirstOrDefault(blade =>
            {
                return Traverse.Create(blade).Field<string>("name").Value == "MODS";
            });

            trials.Position.X += 40;
            Traverse.Create(trials).Field("tweenTo").SetValue(trials.Position);

            archivesButton.Position.X += 30;
            Traverse.Create(archivesButton).Field("tweenTo").SetValue(archivesButton.Position);

            workshopButton.Position.X += 25;
            Traverse.Create(workshopButton).Field("tweenTo").SetValue(workshopButton.Position);

            var replayButton = new ReplayButton(new Vector2(105f, trials.Position.Y), new Vector2(100f, 300f), "REPLAYS", "");
            replayButton.RightItem = trials;
            replayButton.UpItem = fightButton;
            if (modButton != null)
            {
                replayButton.LeftItem = modButton;
            }

            foreach (var blade in bladesButton)
            {
                blade.RightItem = replayButton;
            }

            trials.LeftItem = replayButton;

            if (coopButton != null)
            {
                trials.UpItem = coopButton;
                archivesButton.UpItem = coopButton;
                workshopButton.UpItem = coopButton;
                coopButton.DownItem = archivesButton;
            }

            __instance.Add(replayButton);
        }
    }
}
