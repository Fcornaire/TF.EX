using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using System.Diagnostics;
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
        private static readonly TimeSpan FolderGraceWindow = TimeSpan.FromMilliseconds(120);

        private static List<ReplayInfos> _replays = [];
        private static ReplaysPanel _replaysPanel = null;
        private static ReplayBrowserHud _hud;
        private static ConfirmDialog _deleteDialog;
        private static ReplayInfos _deleteTarget;
        private static Text _noMsg;
        private static LoadingGauge _loadingMsg;
        private static Text _monthMsg;
        private static bool _loading;
        private static string _lastLaunched;
        private static long _totalSize;
        private static int _pendingFolderStep;
        private static long _pendingFolderStepAt;

        private static Task<Action> _loadTask;
        private static MainMenu _owner;

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void MainMenu_Update(MainMenu __instance)
        {
            if ((int)__instance.State == ReplayMenuState.ReplaysBrowser && _loadTask is { IsCompleted: true } loaded)
            {
                _loadTask = null;
                loaded.Result.Invoke();
            }

            if ((int)__instance.State != ReplayMenuState.ReplaysBrowser || _loading)
            {
                return;
            }

            if (_deleteDialog != null)
            {
                UpdateDeleteDialog(__instance);
                return;
            }

            if (_replays.Count > 0 && !_replays.Any(replay => replay.Selected))
            {
                _replays[0].Selected = true;
                DynamicData.For(__instance).Set("ToStartSelected", _replays[0]);
            }

            if (!CanUseBrowserKeys(__instance))
            {
                return;
            }

            if (ReplayBrowserKeys.IsFavoritePressed())
            {
                ToggleFavorite(__instance);
                return;
            }

            if (ReplayBrowserKeys.IsDeletePressed())
            {
                OpenDeleteDialog(__instance);
                return;
            }

            UpdateFolderSwitch(__instance);
        }

        private static bool CanUseBrowserKeys(MainMenu self)
            => !self.Transitioning
               && MenuInput.MenuInputs.Any(input => input != null)
               && !MenuInput.Confirm
               && !MenuInput.Back;

        private static void UpdateFolderSwitch(MainMenu self)
        {
            var service = ServiceCollections.ResolveReplayService();

            if ((MenuInput.Alt || MenuInput.Alt2) && MenuInput.AltCheck && MenuInput.Alt2Check)
            {
                _pendingFolderStep = 0;

                if (!service.IsInFavorites())
                {
                    service.OpenFavorites();
                    Reload(self);
                }

                return;
            }

            if (MenuInput.Alt || MenuInput.Alt2)
            {
                _pendingFolderStep = MenuInput.Alt ? -1 : 1;
                _pendingFolderStepAt = Stopwatch.GetTimestamp();
                return;
            }

            if (_pendingFolderStep == 0 || Stopwatch.GetElapsedTime(_pendingFolderStepAt) < FolderGraceWindow)
            {
                return;
            }

            var step = _pendingFolderStep;
            _pendingFolderStep = 0;

            var months = service.GetMonths();

            if (!months.Any(month => month != service.CurrentMonth))
            {
                return;
            }

            var index = Array.IndexOf(months, service.CurrentMonth);
            var next = ((index < 0 ? 0 : index) + step + months.Length) % months.Length;

            service.SetMonth(months[next]);
            Reload(self);
        }

        private static void Reload(MainMenu self)
        {
            Sounds.ui_click.Play();

            HandleReplayBrowser(self, "Destroy");
            CreateReplay(self);
        }

        private static void ToggleFavorite(MainMenu self)
        {
            _pendingFolderStep = 0;

            var selected = _replays.FirstOrDefault(replay => replay.Selected);

            if (selected == null)
            {
                return;
            }

            var service = ServiceCollections.ResolveReplayService();
            var wasInFavorites = service.IsInFavorites();

            if (!service.ToggleFavorite(selected.Replay))
            {
                Sounds.ui_invalid.Play();
                ServiceCollections.Notify("COULD NOT MOVE THE REPLAY");
                return;
            }

            Sounds.ui_clickSpecial.Play();
            ServiceCollections.Notify(wasInFavorites ? "REMOVED FROM FAVORITES" : "ADDED TO FAVORITES");

            RemoveFromBrowser(self, selected);
        }

        private static void OpenDeleteDialog(MainMenu self)
        {
            _pendingFolderStep = 0;
            _deleteTarget = _replays.FirstOrDefault(replay => replay.Selected);

            if (_deleteTarget == null)
            {
                return;
            }

            Sounds.ui_click.Play();

            _deleteDialog = new ConfirmDialog("DELETE?", _deleteTarget.DisplayName, "DELETE");
            self.Add(_deleteDialog);
        }

        private static void UpdateDeleteDialog(MainMenu self)
        {
            if (_deleteDialog.IsCancelled)
            {
                Sounds.ui_clickBack.Play();
                CloseDeleteDialog();
                return;
            }

            if (!_deleteDialog.IsConfirmed)
            {
                return;
            }

            var target = _deleteTarget;
            CloseDeleteDialog();

            if (!ServiceCollections.ResolveReplayService().DeleteReplay(target.Replay))
            {
                Sounds.ui_invalid.Play();
                ServiceCollections.Notify("COULD NOT DELETE THE REPLAY");
                return;
            }

            Sounds.ui_clickSpecial.Play();
            ServiceCollections.Notify("REPLAY DELETED");

            _totalSize -= target.Replay.FileSize;
            RemoveFromBrowser(self, target);
        }

        private static void CloseDeleteDialog()
        {
            _deleteDialog?.Close();
            _deleteDialog = null;
            _deleteTarget = null;
        }

        private static void RemoveFromBrowser(MainMenu self, ReplayInfos removed)
        {
            var index = _replays.IndexOf(removed);
            var neighbor = _replays.ElementAtOrDefault(index + 1) ?? _replays.ElementAtOrDefault(index - 1);
            var remaining = _replays.Where(replay => replay != removed).Select(replay => replay.Replay).ToArray();
            var service = ServiceCollections.ResolveReplayService();
            var canSwitchFolder = service.GetMonths().Any(month => month != service.CurrentMonth);

            ClearBrowser(self);
            PopulateBrowser(self, remaining, canSwitchFolder, neighbor?.FileName, ServiceCollections.ResolveLogger());
            SetGuides(self, canSwitchFolder);
        }

        private static string MonthLabel(string month) => string.IsNullOrEmpty(month) ? "UNSORTED" : month;

        private static void SetGuides(MainMenu self, bool canSwitchFolder)
        {
            self.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "LAUNCH");
            self.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "EXIT");

            if (canSwitchFolder)
            {
                self.ButtonGuideC.SetDetails(MenuButtonGuide.ButtonModes.Alt, "PREV MONTH");
                self.ButtonGuideD.SetDetails(MenuButtonGuide.ButtonModes.Alt2, "NEXT MONTH");
                return;
            }

            self.ButtonGuideC.Clear();
            self.ButtonGuideD.Clear();
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

            _loadingMsg = new LoadingGauge(message);
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
                _loadTask = null;

                CloseDeleteDialog();
                ClearBrowser(self);

                _monthMsg?.RemoveSelf();
                _monthMsg = null;

                HideLoading();
                _loading = false;
                _pendingFolderStep = 0;

                return;
            }

            CreateReplay(self);
        }

        private static void ClearBrowser(MainMenu self)
        {
            self.DeleteAll<ReplayInfos>();

            _replaysPanel?.RemoveSelf();
            _replaysPanel = null;

            _replays.Clear();

            _noMsg?.RemoveSelf();
            _noMsg = null;

            _hud?.RemoveSelf();
            _hud = null;
        }

        private static void ResetBrowser(MainMenu self)
        {
            if (_owner == self)
            {
                return;
            }

            _owner = self;
            _loadTask = null;

            CloseDeleteDialog();
            _replays.Clear();
            _replaysPanel = null;
            _hud = null;
            _noMsg = null;
            _monthMsg = null;
            _loadingMsg = null;
            _loading = false;
            _pendingFolderStep = 0;
        }

        private static void CreateReplay(MainMenu self)
        {
            ResetBrowser(self);

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

            var canSwitchFolder = months.Any(month => month != replayService.CurrentMonth);

            _monthMsg = new Text($"< {MonthLabel(replayService.CurrentMonth)} >")
            {
                Position = new Vector2(-70f, -36f)
            };
            self.Add(_monthMsg);

            var gauge = _loadingMsg;

            _loadTask = Task.Run<Action>(async () =>
            {
                try
                {
                    var replays = (await replayService.LoadAndGetReplays(gauge.Report)).ToArray();
                    var totalSize = replayService.GetTotalSize();

                    return () => BuildBrowser(self, replays, totalSize, canSwitchFolder, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError("Error when loading replays: {error}", ex);

                    return StopLoading;
                }
            });
        }

        private static void BuildBrowser(MainMenu self, Domain.Models.Replay[] replays, long totalSize, bool canSwitchFolder, ILogger logger)
        {
            _totalSize = totalSize;

            PopulateBrowser(self, replays, canSwitchFolder, _lastLaunched, logger);
            StopLoading();
            SetGuides(self, canSwitchFolder);

            if (_replays.Count == 0)
            {
                return;
            }

            DynamicData.For(self).Invoke("TweenBGCameraToY", 1);
            Sounds.ui_startGame.Play();
        }

        private static void PopulateBrowser(MainMenu self, Domain.Models.Replay[] replays, bool canSwitchFolder, string selectFileName, ILogger logger)
        {
            var service = ServiceCollections.ResolveReplayService();
            var dynMainMenu = DynamicData.For(self);
            var dynCamera = DynamicData.For(self.UILayer);

            _replays.Clear();

            _hud = new ReplayBrowserHud
            {
                FolderSize = replays.Sum(replay => replay?.FileSize ?? 0),
                TotalSize = _totalSize,
                HasReplays = replays.Length > 0,
                IsInFavorites = service.IsInFavorites(),
            };
            self.Add(_hud);

            if (replays.Length == 0)
            {
                _noMsg = new Text(service.IsInFavorites()
                    ? "NO FAVORITES YET"
                    : canSwitchFolder
                        ? "NOTHING THIS MONTH"
                        : "PLAY SOME GAMES FIRST!")
                {
                    Position = new Vector2(100, 100)
                };
                self.Add(_noMsg);

                return;
            }

            var maxY = 50.0f;

            _replaysPanel = new ReplaysPanel(225, 88 + self.UILayer.Camera.Y);

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

                dynCamera.Invoke("Add", but, false);

                maxY += 35.0f;
            }

            self.MaxUICameraY = maxY;

            var selected = _replays.FirstOrDefault(replay => replay.FileName == selectFileName) ?? _replays.FirstOrDefault();

            if (selected != null)
            {
                dynMainMenu.Set("ToStartSelected", selected);
                selected.Selected = true;
            }

            dynCamera.Invoke("Add", _replaysPanel, false);
        }

        private static void StopLoading()
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

            _lastLaunched = toLaunch.FileName;

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
                    failure = ServiceCollections.LaunchReplay(toLaunch.FileName, currentSong);
                }
                catch (Exception e)
                {
                    ServiceCollections.ResolveLogger().LogError("Could not start {name}: {error}", toLaunch.FileName, e);

                    failure = "CHECK LOG";
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
