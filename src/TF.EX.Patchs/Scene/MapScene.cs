using FortRise.Adventure;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    internal class MapScenePatch : IHookable
    {
        private readonly IInputService _inputService;
        private readonly IMatchmakingService matchmakingService;
        private readonly IRngService _rngService;
        private readonly INetplayManager netplayManager;

        public MapScenePatch(IInputService inputService, IMatchmakingService matchmakingService, IRngService rngService, INetplayManager netplayManager)
        {
            _inputService = inputService;
            this.matchmakingService = matchmakingService;
            _rngService = rngService;
            this.netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.MapScene.StartSession += MapScene_StartSession;
            On.TowerFall.MapScene.GetRandomVersusTower += MapScene_GetRandomVersusTower;
            On.TowerFall.MapScene.Begin += MapScene_Begin;
        }

        public void Unload()
        {
            On.TowerFall.MapScene.StartSession -= MapScene_StartSession;
            On.TowerFall.MapScene.GetRandomVersusTower -= MapScene_GetRandomVersusTower;
            On.TowerFall.MapScene.Begin -= MapScene_Begin;
        }

        private void MapScene_StartSession(On.TowerFall.MapScene.orig_StartSession orig, TowerFall.MapScene self)
        {
            var lobby = matchmakingService.GetOwnLobby();
            netplayManager.UpdatePlayers(lobby.Players, lobby.Spectators);
            matchmakingService.DisconnectFromLobby();

            orig(self);

            _rngService.Reset();
            _inputService.EnsureRemoteController();
            ServiceCollections.PurgeCache();
        }

        private void MapScene_Begin(On.TowerFall.MapScene.orig_Begin orig, MapScene self)
        {
            orig(self);

            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();
            if (currentMode.IsNetplay())
            {
                self.Selection.OnDeselect();

                var mapId = matchmakingService.GetOwnLobby().GameData.MapId;

                self.Selection = self.Buttons.First(button => mapId == -1 ? (button is VersusRandomSelect && button is not AdventureChaoticRandomSelect) : mapId == button.Data?.ID.X);
                self.Selection.OnSelect();
                self.ScrollToButton(self.Selection);
            }
        }

        /// <summary>
        /// Almost the same as original, but with a custom shuffle method and netplay safe (aka can work online).
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <returns></returns>
        private TowerFall.MapButton MapScene_GetRandomVersusTower(On.TowerFall.MapScene.orig_GetRandomVersusTower orig, TowerFall.MapScene self)
        {
            List<MapButton> list = new List<MapButton>(self.Buttons);
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

            _rngService.Reset();
            var shuffled = CalcExtensions.OwnMapButtonShuffle(list).ToArray();
            return shuffled[0];
            //return shuffled.SingleOrDefault(b => b.Data.ID.X == 1); //Usefull for debug
        }

        private bool IsNetplaySafe(string title)
        {
            return Constants.NETPLAY_SAFE_MAP.Contains(title);
        }
    }
}
