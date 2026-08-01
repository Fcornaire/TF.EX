using HarmonyLib;
using TF.EX.Domain.Interop;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(Session))]
    public class SessionPatch
    {
        /// <summary>
        /// Some hack since MatchResult is not tracked in the gamestate
        /// </summary>

        [HarmonyPrefix]
        [HarmonyPatch("GotoNextRound")]
        public static bool Session_GotoNextRound(Session __instance)
        {
            var logger = ServiceCollections.ResolveLogger();
            var mode = TowerFall.MainMenu.VersusMatchSettings.Mode;

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

        /// <summary>
        /// Some hack since MatchResult is not tracked in the gamestate
        /// </summary>

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

        [HarmonyPostfix]
        [HarmonyPatch("StartRound")]
        public static void Session_StartRound()
        {
            StateApi.Current.SetSessionRoundStarted(true);
        }
    }
}
