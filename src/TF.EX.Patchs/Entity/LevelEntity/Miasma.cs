using HarmonyLib;
using Monocle; // for Calc.Choose (extension method) used in the Amaranth direction roll
using TF.EX.Domain;
using TF.EX.TowerFallExtensions.Entity.LevelEntity;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.Miasma))]
    internal class MiasmaPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void Miasma_Update(TowerFall.Miasma __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (netplayManager.IsUpdating())
            {
                return;
            }

            var state = ServiceCollections.ResolveSessionService().GetSession().Miasma;

            if (state.IsDissipating)
            {
                state.DissipateTicks += TowerFall.TFGame.TimeMult;
                MiasmaSequenceController.EvaluateDissipate(
                    state.DissipateTicks, state.DissipateStartPercent, state.DissipateStartSideWeight,
                    out float dp, out float dsw, out bool shouldRemove);
                __instance.Percent = dp;
                __instance.SideWeight = dsw;
                __instance.Collidable = false;
                if (shouldRemove)
                {
                    __instance.RemoveSelf();
                }
                return;
            }

            state.Ticks += TowerFall.TFGame.TimeMult;

            if (MiasmaSequenceController.ShouldRollAmaranthDir(state.Mode, state.Ticks, state.Dir))
            {
                state.Dir = Monocle.Calc.Random.Choose(1, -1);
            }

            var result = MiasmaSequenceController.Evaluate(state.Mode, state.Ticks, state.Dir);
            __instance.Percent = result.Percent;
            __instance.SideWeight = result.SideWeight;
            __instance.Collidable = result.Collidable;
            __instance.NervesOfSteelCheck = result.NervesOfSteelCheck;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Dissipate")]
        public static void Miasma_Dissipate(TowerFall.Miasma __instance)
        {
            var state = ServiceCollections.ResolveSessionService().GetSession().Miasma;

            if (state.IsDissipating)
            {
                return;
            }

            state.IsDissipating = true;
            state.DissipateTicks = 0f;
            state.DissipateStartPercent = __instance.Percent;
            state.DissipateStartSideWeight = __instance.SideWeight;
        }
    }
}
