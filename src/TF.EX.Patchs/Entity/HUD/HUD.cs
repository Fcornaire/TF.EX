using HarmonyLib;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    [HarmonyPatch(typeof(TowerFall.HUD))]
    internal class HUDPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void HUD_Update(TowerFall.HUD __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();

            if (__instance is VersusMatchResults && !netplayManager.IsReplayMode())
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
        }
    }
}
