using Microsoft.Extensions.Logging;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;
using static TowerFall.PauseMenu;

namespace TF.EX.Patchs.Entity
{
    public class PauseMenuPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly IMatchmakingService _matchmakingService;
        private readonly IInputService _inputService;
        private readonly ILogger _logger;

        public PauseMenuPatch(INetplayManager netplayManager, IMatchmakingService matchmakingService, IInputService inputService, ILogger logger)
        {
            _netplayManager = netplayManager;
            _matchmakingService = matchmakingService;
            _inputService = inputService;
            _logger = logger;
        }

        public void Load()
        {
            On.TowerFall.PauseMenu.VersusMatchSettingsAndSave += PauseMenu_VersusMatchSettingsAndSave;
            On.TowerFall.PauseMenu.AddItem += PauseMenu_AddItem;
            On.TowerFall.PauseMenu.VersusRematch += PauseMenu_VersusRematch;
            On.TowerFall.PauseMenu.VersusArcherSelect += PauseMenu_VersusArcherSelect;
            On.TowerFall.PauseMenu.Quit += PauseMenu_Quit;
            On.TowerFall.PauseMenu.Update += PauseMenu_Update;
        }

        public void Unload()
        {
            On.TowerFall.PauseMenu.VersusMatchSettingsAndSave -= PauseMenu_VersusMatchSettingsAndSave;
            On.TowerFall.PauseMenu.AddItem -= PauseMenu_AddItem;
            On.TowerFall.PauseMenu.VersusRematch -= PauseMenu_VersusRematch;
            On.TowerFall.PauseMenu.VersusArcherSelect -= PauseMenu_VersusArcherSelect;
            On.TowerFall.PauseMenu.Quit -= PauseMenu_Quit;
            On.TowerFall.PauseMenu.Update -= PauseMenu_Update;
        }

        private void PauseMenu_Update(On.TowerFall.PauseMenu.orig_Update orig, PauseMenu self)
        {
            orig(self);

            if (IsNetplayEndgame(self))
            {
                var lobby = _matchmakingService.GetOwnLobby();
                if (lobby.IsEmpty)
                {
                    var dynPauseMenu = DynamicData.For(self);
                    List<string> optionNames = dynPauseMenu.Get<List<string>>("optionNames");
                    List<string> selectedOptionNames = dynPauseMenu.Get<List<string>>("selectedOptionNames");
                    List<Action> optionActions = dynPauseMenu.Get<List<Action>>("optionActions");

                    if (optionNames.Count > 1)
                    {
                        optionNames.Remove("REMATCH!");
                        optionNames.Remove("ARCHER SELECT");
                        dynPauseMenu.Set("optionIndex", 0);
                    }

                    if (selectedOptionNames.Count > 1)
                    {
                        selectedOptionNames.Remove("> REMATCH!");
                        selectedOptionNames.Remove("> ARCHER SELECT");
                    }

                    if (optionActions.Count > 1)
                    {
                        //A bit hacky but the last action is the Quit action
                        optionActions.RemoveRange(0, optionActions.Count - 1);
                    }
                }
            }
        }

        private void PauseMenu_Quit(On.TowerFall.PauseMenu.orig_Quit orig, PauseMenu self)
        {
            if (IsNetplayEndgame(self))
            {
                var lobby = _matchmakingService.GetOwnLobby();

                if (!lobby.IsEmpty)
                {
                    Task.Run(async () =>
                    {
                        await _matchmakingService.LeaveLobby(() => { }, () => { });
                    });
                }

                Sounds.ui_clickBack.Play();

                TFGame.Instance.Scene = new MainMenu(MainMenu.MenuState.VersusOptions);
                var dynPauseMenu = DynamicData.For(self);
                Level level = dynPauseMenu.Get<Level>("level");
                level.Session.MatchSettings.LevelSystem.Dispose();

                return;
            }

            orig(self);
        }

        private void ArcherSelectOrRematch(On.TowerFall.PauseMenu.orig_VersusArcherSelect origArcherSelect, On.TowerFall.PauseMenu.orig_VersusRematch origRematch, PauseMenu self, Func<Task> action)
        {
            if (!IsNetplayEndgame(self))
            {
                if (origArcherSelect != null)
                {
                    origArcherSelect(self);
                }

                if (origRematch != null)
                {
                    origRematch(self);
                }

                return;
            }

            var ownLobby = _matchmakingService.GetOwnLobby();
            if (ownLobby.IsEmpty)
            {
                Sounds.ui_invalid.Play();
                Notification.Create(TFGame.Instance.Scene, "All players left...");
                return;
            }

            _inputService.DisableAllControllers();

            Task.Run(async () =>
            {
                Sounds.ui_click.Play();
                await action();
                Notification.Create(TFGame.Instance.Scene, "Waiting for other players...", 10, 10, true);
            });
        }

        private void PauseMenu_VersusArcherSelect(On.TowerFall.PauseMenu.orig_VersusArcherSelect orig, PauseMenu self)
        {
            ArcherSelectOrRematch(orig, null, self, _matchmakingService.ArcherSelectChoice);
        }

        private void PauseMenu_VersusRematch(On.TowerFall.PauseMenu.orig_VersusRematch orig, PauseMenu self)
        {
            ArcherSelectOrRematch(null, orig, self, _matchmakingService.RematchChoice);
        }

        private void PauseMenu_AddItem(On.TowerFall.PauseMenu.orig_AddItem orig, PauseMenu self, string name, Action action)
        {
            if (name == "MATCH SETTINGS" && IsNetplayEndgame(self))
            {
                _logger.LogDebug<PauseMenuPatch>("Ignore Adding MATCH SETTINGS button to VersusMatchEnd menu on netplay");
                return;
            }

            var lobby = _matchmakingService.GetOwnLobby();
            if (lobby.IsEmpty && name == "REMATCH!")
            {
                _logger.LogDebug<PauseMenuPatch>("Ignore Adding REMATCH button to VersusMatchEnd menu on netplay because lobby is empty");
                return;
            }

            if (lobby.IsEmpty && name == "ARCHER SELECT")
            {
                _logger.LogDebug<PauseMenuPatch>("Ignore Adding ARCHER SELECT button to VersusMatchEnd menu on netplay because lobby is empty");
                return;
            }


            orig(self, name, action);
        }


        private void PauseMenu_VersusMatchSettingsAndSave(On.TowerFall.PauseMenu.orig_VersusMatchSettingsAndSave orig, TowerFall.PauseMenu self)
        {
            if (_netplayManager.IsReplayMode())
            {
                Sounds.ui_clickBack.Play();
                MainMenu mainMenu = new MainMenu(MainMenu.MenuState.Main);
                TFGame.Instance.Scene = mainMenu;

                var dynPauseMenu = DynamicData.For(self);
                var level = dynPauseMenu.Get<Level>("level");

                level.Session.MatchSettings.LevelSystem.Dispose();

                return;
            }

            orig(self);

        }

        private bool IsNetplayEndgame(PauseMenu self)
        {
            var dynPauseMenu = DynamicData.For(self);
            var menuType = dynPauseMenu.Get<MenuType>("menuType");

            return _matchmakingService.IsConnectedToServer() && menuType == MenuType.VersusMatchEnd;
        }

    }
}
