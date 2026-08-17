using HarmonyLib;
using TF.State.Domain;
using TowerFall;

namespace TF.State.Patchs.Layer
{
    [HarmonyPatch(typeof(Monocle.Layer))]
    internal class LayerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Monocle.Layer.Remove), typeof(Monocle.Entity))]
        public static void Layer_Remove_Entity(Monocle.Layer __instance, Monocle.Entity entity)
        {
            var hudService = ServiceCollections.ResolveHUDService();
            var sessionService = ServiceCollections.ResolveSessionService();

            if (entity is VersusStart)
            {
                var hud = hudService.Get();
                hudService.Update(new TF.State.Domain.Models.Entity.HUD.HUD
                {
                    VersusStart = new TF.State.Domain.Models.Entity.HUD.VersusStart(),
                    VersusRoundResults = new TF.State.Domain.Models.Entity.HUD.VersusRoundResults
                    {
                        CoroutineState = hud.VersusRoundResults.CoroutineState
                    }
                });
            }

            if (entity is VersusRoundResults)
            {
                var hud = hudService.Get();
                hudService.Update(new TF.State.Domain.Models.Entity.HUD.HUD
                {
                    VersusStart = new TF.State.Domain.Models.Entity.HUD.VersusStart
                    {
                        CoroutineState = hud.VersusStart.CoroutineState,
                        TweenState = hud.VersusStart.TweenState
                    },
                    VersusRoundResults = new TF.State.Domain.Models.Entity.HUD.VersusRoundResults()
                });
            }


            if (entity is Miasma)
            {
                var session = sessionService.GetSession();
                session.Miasma = TF.State.Domain.Models.Miasma.Default(); //FIX
            }
        }
    }
}
