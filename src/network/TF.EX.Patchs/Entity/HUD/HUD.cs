using HarmonyLib;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    [HarmonyPatch(typeof(TowerFall.HUD))]
    internal class HUDPatch
    {
        private static TowerFall.HUD _reassignTarget;

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void HUD_Update(TowerFall.HUD __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();

            if (__instance is VersusMatchResults
                && !netplayManager.IsReplayMode()
                && (netplayManager.IsInit() || netplayManager.IsTestMode()))
            {
                var dynVersusMatchResults = Traverse.Create(__instance);
                var finished = dynVersusMatchResults.Field("finished").GetValue<bool>();
                var hasReset = dynVersusMatchResults.Field("HasReset").GetValue<bool>(); //TODO: huh ?

                if (finished && !hasReset)
                {
                    if (netplayManager.IsTestMode())
                    {
                        replayService.Reset();
                    }
                    else
                    {
                        replayService.Export();
                    }

                    ServiceCollections.ResolveNetplayManager().Reset();
                    dynVersusMatchResults.Field("HasReset").SetValue(true);
                }
            }

            if (__instance is VersusMatchResults
                && TowerFall.MainMenu.VersusMatchSettings?.Mode.IsNetplay() == true
                && !netplayManager.IsReplayMode()
                && !netplayManager.IsTestMode()
                && !netplayManager.IsInit()
                && _reassignTarget != __instance
                && Traverse.Create(__instance).Field("finished").GetValue<bool>())
            {
                _reassignTarget = __instance;

                TowerFall.PlayerInput.AssignInputs();
                MenuInput.UpdateInputs();
                ServiceCollections.ResolveInputService().RebindLocalInput();

                var matchmakingService = ServiceCollections.ResolveMatchmakingService();
                var level = __instance.Scene as TowerFall.Level;

                matchmakingService.NotifyMatchEnded(WinnerSeat(level?.Session), SeatScores(level?.Session, matchmakingService.GetOwnLobby().Players.Count));

                if (level != null && !matchmakingService.GetOwnLobby().IsEmpty)
                {
                    Domain.CustomComponent.MatchEndChoices.Create(level);
                }
            }
        }

        private static int WinnerSeat(Session session)
        {
            var winner = session?.GetWinner() ?? -1;

            if (winner < 0 || session.MatchSettings?.TeamMode != true)
            {
                return winner;
            }

            return Enumerable.Range(0, TFGame.PlayerAmount).FirstOrDefault(seat => TFGame.Players[seat] && session.GetScoreIndex(seat) == winner, -1);
        }

        private static List<int> SeatScores(Session session, int seats)
        {
            if (session?.Scores == null)
            {
                return [];
            }

            return [.. Enumerable.Range(0, Math.Min(seats, TFGame.PlayerAmount)).Select(seat => session.Scores[session.GetScoreIndex(seat)])];
        }
    }
}
