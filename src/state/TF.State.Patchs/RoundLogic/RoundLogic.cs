using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;

using TF.State.Domain.Extensions;
using TF.State.Domain.Models;
using TF.State.TowerFallExtensions;

using TF.State.Domain.Context;
namespace TF.State.Patchs.RoundLogic
{
    [HarmonyPatch(typeof(TowerFall.RoundLogic))]
    public class RoundLogicPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("SpawnPlayersFFA")]
        public static bool RoundLogic_SpawnPlayersFFA(TowerFall.RoundLogic __instance)
        {
            if (!StateFlags.IsCaptureActive && !StateFlags.IsReplayMode)
            {
                return true;
            }

            Vector2[] array = new Vector2[4];
            List<Vector2> xMLPositions = __instance.Session.CurrentLevel.GetXMLPositions("PlayerSpawn");

            if (xMLPositions.Count < TowerFall.TFGame.PlayerAmount) //Team-only levels
            {
                var teamSpawns = __instance.Session.CurrentLevel.GetXMLPositions("TeamSpawnA");
                teamSpawns.AddRange(__instance.Session.CurrentLevel.GetXMLPositions("TeamSpawnB"));

                foreach (var spawn in teamSpawns)
                {
                    if (xMLPositions.Count >= TowerFall.TFGame.PlayerAmount)
                    {
                        break;
                    }

                    if (!xMLPositions.Contains(spawn))
                    {
                        xMLPositions.Add(spawn);
                    }
                }
            }

            TF.State.Patchs.Calc.CalcPatch.RegisterRng();
            xMLPositions = CalcExtensions.OwnVectorShuffle(xMLPositions).ToList();
            TF.State.Patchs.Calc.CalcPatch.UnregisterRng();
            int num;
            if (!__instance.Session.IsInOvertime)
            {
                num = TowerFall.TFGame.PlayerAmount;
            }
            else
            {
                int highestScore = __instance.Session.GetHighestScore();
                num = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (TowerFall.TFGame.Players[i] && __instance.Session.Scores[i] == highestScore)
                    {
                        num++;
                    }
                }
            }

            int num2 = 0;
            var players = new List<TowerFall.Player>();
            for (int j = 0; j < 4; j++)
            {
                if (!__instance.Session.ShouldSpawn(j))
                {
                    continue;
                }

                if (num2 == 0 && num == 2 && xMLPositions[0].X != 160f)
                {
                    Vector2 vector = TowerFall.WrapMath.Opposite(xMLPositions[0]);
                    if (xMLPositions.Contains(vector))
                    {
                        xMLPositions[1] = vector;
                    }
                }

                int i = j;
                array[j] = xMLPositions[num2] + Vector2.UnitY * 2f;

                TowerFall.Player entity = new TowerFall.Player(i, array[j], TowerFall.Allegiance.Neutral, TowerFall.Allegiance.Neutral, __instance.Session.GetPlayerInventory(i), __instance.Session.GetSpawnHatState(i), frozen: true, flash: true, indicator: true);

                players.Add(entity);

                num2++;
            }

            foreach (TowerFall.Player entity in players.ToArray())
            {
                __instance.Session.CurrentLevel.Add(entity);
            }

            Traverse.Create(__instance).Property("Players").SetValue(num); //Updating directly to skip the original method logic
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("SpawnPlayersTeams")]
        public static bool RoundLogic_SpawnPlayersTeams(TowerFall.RoundLogic __instance)
        {
            if (!StateFlags.IsCaptureActive && !StateFlags.IsReplayMode)
            {
                return true;
            }

            var level = __instance.Session.CurrentLevel;
            var teamSpawns = level.GetXMLPositions("TeamSpawn");
            var teamSpawnsA = level.GetXMLPositions("TeamSpawnA");
            var teamSpawnsB = level.GetXMLPositions("TeamSpawnB");

            foreach (var spawn in teamSpawns)
            {
                if (spawn.X <= 160f)
                {
                    teamSpawnsA.Add(spawn);
                }
                else
                {
                    teamSpawnsB.Add(spawn);
                }
            }

            var neededA = CountTeam(__instance, TowerFall.Allegiance.Blue);
            var neededB = CountTeam(__instance, TowerFall.Allegiance.Red);

            if (teamSpawnsA.Count < neededA || teamSpawnsB.Count < neededB)
            {
                var playerSpawns = level.GetXMLPositions("PlayerSpawn");
                var used = new List<Vector2>(teamSpawnsA);
                used.AddRange(teamSpawnsB);

                TopUpSpawns(teamSpawnsA, neededA, playerSpawns.Where(spawn => spawn.X <= 160f), playerSpawns, used);
                TopUpSpawns(teamSpawnsB, neededB, playerSpawns.Where(spawn => spawn.X > 160f), playerSpawns, used);
            }

            teamSpawnsA.Sort(SortTeamSpawnsLeft);
            teamSpawnsB.Sort(SortTeamSpawnsRight);

            TF.State.Patchs.Calc.CalcPatch.RegisterRng();
            var order = CalcExtensions.OwnShuffledIndexes(Math.Max(teamSpawnsA.Count, teamSpawnsB.Count));
            TF.State.Patchs.Calc.CalcPatch.UnregisterRng();

            teamSpawnsA = ApplyOrder(teamSpawnsA, order);
            teamSpawnsB = ApplyOrder(teamSpawnsB, order);

            SpawnTeam(__instance, TowerFall.Allegiance.Blue, teamSpawnsA);
            SpawnTeam(__instance, TowerFall.Allegiance.Red, teamSpawnsB);

            Traverse.Create(__instance).Property("Players").SetValue(TowerFall.TFGame.PlayerAmount);
            return false;
        }

        private static int CountTeam(TowerFall.RoundLogic self, TowerFall.Allegiance allegiance)
        {
            var count = 0;
            for (int i = 0; i < 4; i++)
            {
                if (self.Session.ShouldSpawn(i) && self.Session.MatchSettings.Teams[i] == allegiance)
                {
                    count++;
                }
            }

            return count;
        }

        private static void TopUpSpawns(List<Vector2> spawns, int needed, IEnumerable<Vector2> preferred, IEnumerable<Vector2> fallback, List<Vector2> used)
        {
            foreach (var candidate in preferred.Concat(fallback))
            {
                if (spawns.Count >= needed)
                {
                    return;
                }

                if (!used.Contains(candidate))
                {
                    spawns.Add(candidate);
                    used.Add(candidate);
                }
            }
        }

        private static List<Vector2> ApplyOrder(List<Vector2> spawns, IEnumerable<int> order)
        {
            return order.Where(index => index < spawns.Count).Select(index => spawns[index]).ToList();
        }

        private static int SortTeamSpawnsLeft(Vector2 a, Vector2 b)
        {
            if (a.Y != b.Y)
            {
                return (int)((a.Y - b.Y) * 10f);
            }

            return (int)((b.X - a.X) * 10f);
        }

        private static int SortTeamSpawnsRight(Vector2 a, Vector2 b)
        {
            if (a.Y != b.Y)
            {
                return (int)((a.Y - b.Y) * 10f);
            }

            return (int)((a.X - b.X) * 10f);
        }

        private static void SpawnTeam(TowerFall.RoundLogic self, TowerFall.Allegiance allegiance, List<Vector2> spawns)
        {
            var index = 0;
            for (int i = 0; i < 4; i++)
            {
                if (!self.Session.ShouldSpawn(i) || self.Session.MatchSettings.Teams[i] != allegiance)
                {
                    continue;
                }

                if (index >= spawns.Count)
                {
                    break;
                }

                var entity = new TowerFall.Player(i, spawns[index] + Vector2.UnitY * 2f,
                    self.Session.MatchSettings.GetPlayerAllegiance(i),
                    self.Session.MatchSettings.GetPlayerAllegiance(i),
                    self.Session.GetPlayerInventory(i),
                    self.Session.GetSpawnHatState(i),
                    frozen: true, flash: true, indicator: true);

                self.Session.CurrentLevel.Add(entity);
                index++;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnUpdate")]
        public static void RoundLogic_OnUpdate_Prefix(TowerFall.RoundLogic __instance)
        {
            var sessionService = ServiceCollections.ResolveSessionService();

            if (StateFlags.IsCaptureActive)
            {
                var session = sessionService.GetSession();
                LoadState(__instance, session);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnUpdate")]
        public static void RoundLogic_OnUpdate(TowerFall.RoundLogic __instance)
        {

            var miasma = Traverse.Create(__instance).Field("miasma").GetValue<TowerFall.Miasma>();

            if (StateFlags.HasFramesToReSimulate && __instance.Session.CurrentLevel.Get<TowerFall.Miasma>() == null && miasma != null)
            {
                if (StateFlags.IsRollbackFrame) //We might be in the first RBF
                {
                    var dynMiasma = DynamicData.For(miasma);
                    dynMiasma.Set("Scene", __instance.Session.CurrentLevel);

                    __instance.Session.CurrentLevel.GetGameplayLayer().Entities.Add(miasma); //We manually add/tag the miasma
                    miasma.Added();
                    dynMiasma.Set("actualDepth", Constants.MIASMA_CUSTOM_DEPTH); //Setting the custom depth for sorting layer later
                }
            }
        }


        //TODO: Move to level LoadState method
        private static void LoadState(TowerFall.RoundLogic self, TF.State.Domain.Models.Session toLoad)
        {
            var dynamicRounlogic = DynamicData.For(self);
            var miasmaCounter = dynamicRounlogic.Get<Counter>("miasmaCounter");

            if (miasmaCounter.Value > 0)
            {
                dynamicRounlogic.Set("miasma", null);
            }
            else
            {
                dynamicRounlogic.Set("miasma", self.Session.CurrentLevel.Get<TowerFall.Miasma>());
            }
        }
    }
}
