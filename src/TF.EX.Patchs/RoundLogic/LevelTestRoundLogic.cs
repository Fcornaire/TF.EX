using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs.RoundLogic
{
    public class LevelTestRoundLogicPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;

        public LevelTestRoundLogicPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.RoundLogic.OnLevelLoadFinish += RoundLogic_OnLevelLoadFinish;
        }

        public void Unload()
        {
            On.TowerFall.RoundLogic.OnLevelLoadFinish -= RoundLogic_OnLevelLoadFinish;
        }

        /// <summary>
        /// Same but without shuffle and swapping depend of player port
        /// </summary>
        private void RoundLogic_OnLevelLoadFinish(On.TowerFall.RoundLogic.orig_OnLevelLoadFinish orig, TowerFall.RoundLogic self)
        {
            if (!(self is LevelTestRoundLogic))
            {
                orig(self);
                return;
            }

            var session = self.Session;

            List<Vector2> xMLPositions = session.CurrentLevel.GetXMLPositions("PlayerSpawn");
            if (xMLPositions.Count == 0)
            {
                var spawnA = session.CurrentLevel.GetXMLPositions("TeamSpawnA");
                xMLPositions.AddRange(spawnA);

                var spawnB = session.CurrentLevel.GetXMLPositions("TeamSpawnB");
                xMLPositions.AddRange(spawnB);
            }

            for (int i = 0; i < 4; i++)
            {
                if (TFGame.PlayerInputs[i] != null && xMLPositions.Count > i)
                {
                    Player player = new Player(
                        i,
                        xMLPositions.GetPositionByPlayerDraw(_netplayManager.ShouldSwapPlayer(), i) + Vector2.UnitY * 2f,
                        session.TestTeam,
                        session.TestTeam,
                        PlayerInventory.Default,
                        session.TestHatState,
                        frozen: false,
                        flash: false,
                        indicator: false
                   );

                    var dynLayer = DynamicData.For(session.CurrentLevel.Layers[player.LayerIndex]);
                    dynLayer.Invoke("Add", player, false);

                    Alarm.Set(player, 30, player.RemoveIndicator);
                }
            }

            session.StartRound();
        }
    }
}
