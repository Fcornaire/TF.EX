using HarmonyLib;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TowerFall;
using static TowerFall.PauseMenu;

namespace TF.EX.Patchs.Entity
{
    [HarmonyPatch(typeof(PauseMenu))]
    public class PauseMenuPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PauseMenu.Update))]
        public static void PauseMenu_Update(PauseMenu __instance)
        {
            if (IsNetplayEndgame(__instance))
            {
                var matchmakingService = ServiceCollections.ResolveMatchmakingService();
                var lobby = matchmakingService.GetOwnLobby();
                if (lobby.IsEmpty)
                {
                    var dynPauseMenu = DynamicData.For(__instance);
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

        [HarmonyPrefix]
        [HarmonyPatch("Quit")]
        public static bool PauseMenu_Quit(PauseMenu __instance)
        {
            if (IsNetplayEndgame(__instance))
            {
                var matchmakingService = ServiceCollections.ResolveMatchmakingService();

                var lobby = matchmakingService.GetOwnLobby();

                if (!lobby.IsEmpty)
                {
                    Task.Run(async () =>
                    {
                        await matchmakingService.LeaveLobby(() => { }, () => { });
                    });
                }

                Sounds.ui_clickBack.Play();

                TFGame.Instance.Scene = new MainMenu(MainMenu.MenuState.VersusOptions);
                var dynPauseMenu = DynamicData.For(__instance);
                Level level = dynPauseMenu.Get<Level>("level");
                level.Session.MatchSettings.LevelSystem.Dispose();

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("VersusArcherSelect")]
        public static bool PauseMenu_VersusArcherSelect(PauseMenu __instance)
        {
            if (IsNetplayEndgame(__instance))
            {
                var matchmakingService = ServiceCollections.ResolveMatchmakingService();
                var inputService = ServiceCollections.ResolveInputService();

                var ownLobby = matchmakingService.GetOwnLobby();
                if (ownLobby.IsEmpty)
                {
                    Sounds.ui_invalid.Play();
                    Notification.Create(TFGame.Instance.Scene, "All players left...");
                    return false;
                }

                inputService.DisableAllControllers();

                Task.Run(async () =>
                {
                    Sounds.ui_click.Play();
                    await matchmakingService.ArcherSelectChoice();
                    Notification.Create(TFGame.Instance.Scene, "Waiting for other players...", 10, 10, true);
                });

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("VersusRematch")]
        public static bool PauseMenu_VersusRematch(PauseMenu __instance)
        {
            if (IsNetplayEndgame(__instance))
            {
                var matchmakingService = ServiceCollections.ResolveMatchmakingService();
                var inputService = ServiceCollections.ResolveInputService();

                var ownLobby = matchmakingService.GetOwnLobby();
                if (ownLobby.IsEmpty)
                {
                    Sounds.ui_invalid.Play();
                    Notification.Create(TFGame.Instance.Scene, "All players left...");
                    return false;
                }

                inputService.DisableAllControllers();

                Task.Run(async () =>
                {
                    Sounds.ui_click.Play();
                    await matchmakingService.RematchChoice();
                    Notification.Create(TFGame.Instance.Scene, "Waiting for other players...", 10, 10, true);
                });

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("AddItem")]
        public static bool PauseMenu_AddItem(PauseMenu __instance, string name)
        {
            var logger = ServiceCollections.ResolveLogger();
            if (name == "MATCH SETTINGS" && IsNetplayEndgame(__instance))
            {
                logger.LogDebug<PauseMenuPatch>("Ignore Adding MATCH SETTINGS button to VersusMatchEnd menu on netplay");
                return false;
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var lobby = matchmakingService.GetOwnLobby();
            if (lobby.IsEmpty && name == "REMATCH!")
            {
                logger.LogDebug<PauseMenuPatch>("Ignore Adding REMATCH button to VersusMatchEnd menu on netplay because lobby is empty");
                return false;
            }

            if (lobby.IsEmpty && name == "ARCHER SELECT")
            {
                logger.LogDebug<PauseMenuPatch>("Ignore Adding ARCHER SELECT button to VersusMatchEnd menu on netplay because lobby is empty");
                return false;
            }

            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch("VersusMatchSettingsAndSave")]
        public static bool PauseMenu_VersusMatchSettingsAndSave(PauseMenu __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (netplayManager.IsReplayMode())
            {
                Sounds.ui_clickBack.Play();
                MainMenu mainMenu = new MainMenu(MainMenu.MenuState.Main);
                TFGame.Instance.Scene = mainMenu;

                var dynPauseMenu = DynamicData.For(__instance);
                var level = dynPauseMenu.Get<Level>("level");

                level.Session.MatchSettings.LevelSystem.Dispose();

                return false;
            }

            return true;
        }

        private static bool IsNetplayEndgame(PauseMenu self)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var archerService = ServiceCollections.ResolveArcherService();

            var menuType = Traverse.Create(self).Field("menuType").GetValue<MenuType>();

            return (matchmakingService.IsConnectedToServer() || archerService.GetArchers().Count() > 0) && menuType == MenuType.VersusMatchEnd;
        }

    }
}
