using HarmonyLib;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.TowerFallExtensions;
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
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            var lobby = matchmakingService.GetOwnLobby();
            netplayManager.UpdatePlayers(lobby.Players, lobby.Spectators);
            matchmakingService.DisconnectFromLobby();
        }

        [HarmonyPostfix]
        [HarmonyPatch("StartSession")]
        public static void StartSession_Postfix()
        {
            var rgnService = ServiceCollections.ResolveRngService();
            var inputService = ServiceCollections.ResolveInputService();

            rgnService.Reset();
            inputService.EnsureRemoteController();
            ServiceCollections.PurgeCache();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Begin")]
        public static void Begin_Postfix(MapScene __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();
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
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <returns></returns>
        /// 
        [HarmonyPostfix]
        [HarmonyPatch("GetRandomVersusTower")]
        public static void MapScene_GetRandomVersusTower(TowerFall.MapScene __instance, ref TowerFall.MapButton __result)
        {
            var rngService = ServiceCollections.ResolveRngService();

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

            rngService.Reset();
            var shuffled = CalcExtensions.OwnMapButtonShuffle(list).ToArray();
            __result = shuffled[0];
            //return shuffled.SingleOrDefault(b => b.Data.ID.X == 1); //Usefull for debug
        }

        private static bool IsNetplaySafe(string title)
        {
            return Constants.NETPLAY_SAFE_MAP.Contains(title);
        }
    }
}
