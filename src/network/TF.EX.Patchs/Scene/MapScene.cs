using HarmonyLib;
using TF.EX.Domain.Models;
using TF.EX.Domain.Interop;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    [HarmonyPatch(typeof(MapScene))]
    internal class MapScenePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("StartSession")]
        public static void StartSession_Prefix()
        {
            var mode = MainMenu.VersusMatchSettings?.Mode;
            if (mode == null || !mode.Value.IsNetplay())
            {
                return;
            }

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var archerService = ServiceCollections.ResolveArcherService();

            var lobby = matchmakingService.GetOwnLobby();
            netplayManager.UpdatePlayers(lobby.Players, lobby.Spectators);

            if (lobby.IsEmpty)
            {
                return;
            }

            archerService.CompactSeatsToHandles();
            archerService.ApplyToGame();
            matchmakingService.ApplyTeamsToMatchSettings();
        }

        [HarmonyPostfix]
        [HarmonyPatch("StartSession")]
        public static void StartSession_Postfix()
        {
            var mode = MainMenu.VersusMatchSettings?.Mode;
            if (mode == null || !mode.Value.IsNetplay())
            {
                return;
            }

            var inputService = ServiceCollections.ResolveInputService();

            StateApi.Current.ResetRng();
            inputService.EnsureRemoteController(Math.Max(2, ServiceCollections.ResolveMatchmakingService().GetOwnLobby().Players.Count));
            inputService.EnsureEveryControllerSlot();
            StateApi.Current.PurgeCache();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Begin")]
        public static void Begin_Postfix(MapScene __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            var currentMode = MainMenu.VersusMatchSettings?.Mode ?? Modes.LastManStanding;
            if (currentMode.IsNetplay())
            {
                __instance.Selection.OnDeselect();

                var mapId = matchmakingService.GetOwnLobby().GameData.MapId;

                //TODO: && button is not AdventureChaoticRandomSelect

                __instance.Selection = __instance.Buttons.First(button => mapId == -1 ? (button is VersusRandomSelect) : mapId == button.Data?.ID.X);
                __instance.Selection.OnSelect();
                __instance.ScrollToButton(__instance.Selection);
            }
        }

        /// <summary>
        /// Almost the same as original, but with a custom shuffle method and netplay safe (aka can work online).
        /// </summary>
        /// 
        [HarmonyPostfix]
        [HarmonyPatch("GetRandomVersusTower")]
        public static void MapScene_GetRandomVersusTower(TowerFall.MapScene __instance, ref TowerFall.MapButton __result)
        {
            var mode = MainMenu.VersusMatchSettings?.Mode;
            if (mode == null || !mode.Value.IsNetplay())
            {
                return;
            }

            List<MapButton> list = new List<MapButton>(__instance.Buttons);
            list.RemoveAll((MapButton b) => b is not VersusMapButton);
            list.RemoveAll((MapButton b) => !IsNetplaySafe(b.Title));
            if (!GameData.DarkWorldDLC)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Locked)
                    {
                        list.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (list.Count((MapButton b) => b is VersusMapButton && !(b as VersusMapButton).NoRandom) > 0)
            {
                list.RemoveAll((MapButton b) => (b as VersusMapButton).NoRandom);
            }
            else
            {
                foreach (MapButton item in list)
                {
                    if (item.HasAltAction)
                    {
                        item.AltAction();
                    }
                }
            }

            StateApi.Current.ResetRng();

            Monocle.Calc.Shuffle(list, new Random(StateApi.Current.GetSeed()));

            __result = list[0];
        }

        private static bool IsNetplaySafe(string title)
        {
            return Constants.NETPLAY_SAFE_MAP.Contains(title);
        }
    }
}
