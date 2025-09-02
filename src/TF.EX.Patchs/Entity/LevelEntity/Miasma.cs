using HarmonyLib;
using TF.EX.Domain;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.Miasma))]
    internal class MiasmaPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Dissipate")]
        public static void Miasma_Dissipate(TowerFall.Miasma __instance)
        {
            var sessionService = ServiceCollections.ResolveSessionService();
            var miasmaState = sessionService.GetSession().Miasma;

            miasmaState.IsDissipating = true;
            miasmaState.Percent = __instance.Percent;
            miasmaState.SideWeight = __instance.SideWeight;
        }
    }
}
