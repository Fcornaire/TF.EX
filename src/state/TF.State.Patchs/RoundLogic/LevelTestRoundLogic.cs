using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Extensions;
using TowerFall;

namespace TF.State.Patchs.RoundLogic
{
    [HarmonyPatch(typeof(LevelTestRoundLogic))]
    public class LevelTestRoundLogicPatch
    {
        /// <summary>
        /// Same but without shuffle and swapping depend of player port
        /// </summary>
        /// 
        [HarmonyPrefix]
        [HarmonyPatch("OnLevelLoadFinish")]
        public static bool RoundLogic_OnLevelLoadFinish(TowerFall.RoundLogic __instance)
        {
            if (!(__instance is LevelTestRoundLogic))
            {
                return true;
            }

            if (!TF.State.Domain.Context.StateFlags.IsTestMode)
            {
                return true;
            }


            var session = __instance.Session;

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
                        xMLPositions[i] + Vector2.UnitY * 2f,
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

            return false;
        }
    }
}
