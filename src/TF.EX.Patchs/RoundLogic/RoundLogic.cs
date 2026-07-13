using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.TowerFallExtensions;

namespace TF.EX.Patchs.RoundLogic
{
    [HarmonyPatch(typeof(TowerFall.RoundLogic))]
    public class RoundLogicPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("SpawnPlayersFFA")]
        public static bool RoundLogic_SpawnPlayersFFA(TowerFall.RoundLogic __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (!netplayManager.IsInit() && !netplayManager.IsReplayMode())
            {
                return true;
            }

            var rngService = ServiceCollections.ResolveRngService();

            Vector2[] array = new Vector2[4];
            List<Vector2> xMLPositions = __instance.Session.CurrentLevel.GetXMLPositions("PlayerSpawn");

            rngService.Get().ResetRandom(ref Monocle.Calc.Random);

            xMLPositions = CalcExtensions.OwnVectorShuffle(xMLPositions).ToList();
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

                if (netplayManager.ShouldSwapPlayer())
                {
                    i = j == 0 ? 1 : 0;
                    array[j] = xMLPositions.GetPositionByPlayerDraw(netplayManager.ShouldSwapPlayer(), num2) + Vector2.UnitY * 2f;
                }

                TowerFall.Player entity = new TowerFall.Player(i, array[j], TowerFall.Allegiance.Neutral, TowerFall.Allegiance.Neutral, __instance.Session.GetPlayerInventory(i), __instance.Session.GetSpawnHatState(i), frozen: true, flash: true, indicator: true);

                players.Add(entity);

                num2++;
            }

            if (netplayManager.ShouldSwapPlayer())
            {
                players.Reverse(); //TODO: Not true with more than 2 players
            }

            foreach (TowerFall.Player entity in players.ToArray())
            {
                __instance.Session.CurrentLevel.Add(entity);
            }

            Traverse.Create(__instance).Property("Players").SetValue(num); //Updating directly to skip the original method logic
            return false;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch("InsertCrownEvent")]
        //public static bool RoundLogic_InsertCrownEvent(TowerFall.RoundLogic __instance)
        //{
        //    var netplayManager = ServiceCollections.ResolveNetplayManager();
        //    var inputInputService = ServiceCollections.ResolveInputService();

        //    bool[] array = new bool[__instance.Session.Scores.Length];

        //    if ((netplayManager.IsInit() || netplayManager.IsTestMode()) && netplayManager.ShouldSwapPlayer())
        //    {
        //        for (int i = 0; i < array.Length; i++)
        //        {
        //            if (i == 0)
        //            {
        //                array[inputInputService.GetLocalPlayerInputIndex()] = __instance.Session.TeamHasCrown(i);
        //            }

        //            if (i == 1)
        //            {
        //                array[inputInputService.GetRemotePlayerInputIndex()] = __instance.Session.TeamHasCrown(i);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        for (int i = 0; i < array.Length; i++)
        //        {
        //            array[i] = __instance.Session.TeamHasCrown(i);
        //        }
        //    }

        //    __instance.Events.Add(new TowerFall.CrownChangeEvent(array));

        //    return false;
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch("AddScore")]
        //public static void RoundLogic_AddScore(TowerFall.RoundLogic __instance, int scoreIndex)
        //{
        //    var netplayManager = ServiceCollections.ResolveNetplayManager();
        //    var inputInputService = ServiceCollections.ResolveInputService();
        //    var logger = ServiceCollections.ResolveLogger();

        //    if ((netplayManager.IsInit() || netplayManager.IsTestMode()) && netplayManager.ShouldSwapPlayer())
        //    {
        //        var evt = __instance.Events.Find(e => e is TowerFall.GainPointEvent && (e as TowerFall.GainPointEvent).ScoreIndex == scoreIndex);
        //        __instance.Events.Remove(evt);

        //        if (scoreIndex == 0)
        //        {
        //            __instance.Events.Add(new TowerFall.GainPointEvent(inputInputService.GetLocalPlayerInputIndex()));
        //        }

        //        if (scoreIndex == 1)
        //        {
        //            __instance.Events.Add(new TowerFall.GainPointEvent(inputInputService.GetRemotePlayerInputIndex()));
        //        }
        //    }
        //}

        [HarmonyPrefix]
        [HarmonyPatch("OnUpdate")]
        public static void RoundLogic_OnUpdate_Prefix(TowerFall.RoundLogic __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var sessionService = ServiceCollections.ResolveSessionService();

            if (netplayManager.IsInit())
            {
                var session = sessionService.GetSession();
                LoadState(__instance, session);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnUpdate")]
        public static void RoundLogic_OnUpdate(TowerFall.RoundLogic __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            var miasma = Traverse.Create(__instance).Field("miasma").GetValue<TowerFall.Miasma>();

            if (netplayManager.HaveFramesToReSimulate() && __instance.Session.CurrentLevel.Get<TowerFall.Miasma>() == null && miasma != null)
            {
                if (netplayManager.IsRollbackFrame()) //We might be in the first RBF
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
        private static void LoadState(TowerFall.RoundLogic self, TF.EX.Domain.Models.State.Session toLoad)
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
