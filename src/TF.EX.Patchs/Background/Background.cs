using HarmonyLib;
using TF.EX.Domain;

namespace TF.EX.Patchs.Background
{
    [HarmonyPatch(typeof(TowerFall.Background))]
    internal class BackgroundPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool Background_Update(TowerFall.Background __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (netplayManager.IsRollbackFrame())
            {
                return false;
            }
            return true;
        }
    }
}
