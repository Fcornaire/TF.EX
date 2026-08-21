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
                    replayService.Export();
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

                matchmakingService.NotifyMatchEnded();

                if (__instance.Scene is TowerFall.Level level && !matchmakingService.GetOwnLobby().IsEmpty)
                {
                    Domain.CustomComponent.MatchEndChoices.Create(level);
                }
            }
        }
    }
}
