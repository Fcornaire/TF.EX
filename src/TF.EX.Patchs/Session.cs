using HarmonyLib;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Patchs.Calc;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(Session))]
    public class SessionPatch
    {
        /// <summary>
        /// Some hack since MatchResult is not tracked in the gamestate
        /// </summary>
        /// <param name="__instance"></param>

        [HarmonyPrefix]
        [HarmonyPatch("GotoNextRound")]
        public static bool Session_GotoNextRound(Session __instance)
        {
            var logger = ServiceCollections.ResolveLogger();
            var mode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            if (mode.IsNetplay() && __instance.GetWinner() != -1)
            {
                logger.LogDebug<Session>("Skipping GotoNextRound since game ended");

                var vsRoundResult = __instance.CurrentLevel.Get<VersusRoundResults>();
                var vsMatchResult = __instance.CurrentLevel.Get<VersusMatchResults>();

                if (vsRoundResult != null && vsMatchResult != null)
                {
                    logger.LogDebug<Session>("Hack! Setting roundResults to matchResults");

                    vsRoundResult.MatchResults = vsMatchResult;
                    var dynMatchResult = DynamicData.For(vsMatchResult);
                    dynMatchResult.Set("roundResults", vsRoundResult);
                    __instance.CurrentLevel.Frozen = true;
                    ArcherData.Get(TFGame.Characters[__instance.GetWinner()], TFGame.AltSelect[__instance.GetWinner()]).PlayVictoryMusic();
                }
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("StartGame")]
        public static void Session_StartGame()
        {
            var rngService = ServiceCollections.ResolveRngService();

            CalcPatch.Reset();
            rngService.Reset();
        }

        /// <summary>
        /// Some hack since MatchResult is not tracked in the gamestate
        /// </summary>
        /// <param name="__instance"></param>

        [HarmonyPrefix]
        [HarmonyPatch("CreateResults")]
        public static bool Session_CreateResults(Session __instance)
        {
            var versusMatchResults = __instance.CurrentLevel.Get<VersusMatchResults>();

            if (versusMatchResults != null)
            {
                var logger = ServiceCollections.ResolveLogger();
                logger.LogDebug<Session>("VersusMatchResults found, skipping CreateResults");
                versusMatchResults.TweenIn();

                ArcherData.Get(TFGame.Characters[__instance.GetWinner()], TFGame.AltSelect[__instance.GetWinner()]).PlayVictoryMusic();
                __instance.CurrentLevel.Frozen = true;
                return false;
            }

            return true;
        }

        //[HarmonyPostfix]
        //[HarmonyPatch("GetWinner")]
        //public static void Session_GetWinner(Session __instance, ref int __result)
        //{
        //    var netplayManager = ServiceCollections.ResolveNetplayManager();
        //    var inputService = ServiceCollections.ResolveInputService();

        //    if (netplayManager.IsInit() && netplayManager.ShouldSwapPlayer())
        //    {
        //        if (__result == 0)
        //        {
        //            __result = inputService.GetLocalPlayerInputIndex();
        //        }

        //        if (__result == 1)
        //        {
        //            __result = inputService.GetRemotePlayerInputIndex();
        //        }
        //    }
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch("GetOldScore")]
        //public static void Session_GetOldScore(Session __instance, ref int scoreIndex)
        //{
        //    var netplayManager = ServiceCollections.ResolveNetplayManager();
        //    var inputService = ServiceCollections.ResolveInputService();

        //    if ((netplayManager.IsInit() || netplayManager.IsTestMode()) && netplayManager.ShouldSwapPlayer())
        //    {
        //        if (scoreIndex == 0)
        //        {
        //            scoreIndex = inputService.GetLocalPlayerInputIndex();
        //        }

        //        if (scoreIndex == 1)
        //        {
        //            scoreIndex = inputService.GetRemotePlayerInputIndex();
        //        }
        //    }
        //}


        [HarmonyPostfix]
        [HarmonyPatch("StartRound")]
        public static void Session_StartRound()
        {
            var sessionService = ServiceCollections.ResolveSessionService();

            var session = sessionService.GetSession();
            session.RoundStarted = true;
        }
    }
}
