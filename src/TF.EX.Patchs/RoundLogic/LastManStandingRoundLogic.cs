using HarmonyLib;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs.RoundLogic
{
    [HarmonyPatch(typeof(LastManStandingRoundLogic))]
    internal class LastManStandingRoundLogicPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnLevelLoadFinish")]
        public static bool LastManStandingRoundLogic_OnLevelLoadFinish()
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (!netplayManager.IsRollbackFrame()) //Prevent adding a VersusStart on a rollback frame
            {
                return true;
            }

            return false;
        }
    }
}
