using HarmonyLib;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.TowerFallExtensions.Layer;
using TowerFall;

namespace TF.EX.Patchs.Layer
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
                hudService.Update(new Domain.Models.State.Entity.HUD.HUD
                {
                    VersusStart = new Domain.Models.State.Entity.HUD.VersusStart(),
                    VersusRoundResults = new Domain.Models.State.Entity.HUD.VersusRoundResults
                    {
                        CoroutineState = hud.VersusRoundResults.CoroutineState
                    }
                });
            }

            if (entity is VersusRoundResults)
            {
                var hud = hudService.Get();
                hudService.Update(new Domain.Models.State.Entity.HUD.HUD
                {
                    VersusStart = new Domain.Models.State.Entity.HUD.VersusStart
                    {
                        CoroutineState = hud.VersusStart.CoroutineState,
                        TweenState = hud.VersusStart.TweenState
                    },
                    VersusRoundResults = new Domain.Models.State.Entity.HUD.VersusRoundResults()
                });
            }


            if (entity is Miasma)
            {
                var session = sessionService.GetSession();
                session.Miasma = TF.EX.Domain.Models.State.Miasma.Default(); //FIX
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateEntityList")]
        public static void Layer_UpdateEntityList(Monocle.Layer __instance)
        {
            if (__instance.IsGameplayLayer())
            {
                var netplayManager = ServiceCollections.ResolveNetplayManager();
                if (netplayManager.IsRollbackFrame() && !(TFGame.Instance.Scene is LevelLoaderXML)) //Remove entities from the precedent frame (but on a level only)
                {
                    var dynLayer = DynamicData.For(__instance);
                    var toAdd = dynLayer.Get<List<Monocle.Entity>>("toAdd");
                    var toRemove = dynLayer.Get<HashSet<Monocle.Entity>>("toRemove");
                    var toRemoveCache = dynLayer.Get<HashSet<Monocle.Entity>>("toRemoveCache");

                    toAdd.Clear();
                    toRemove.Clear();
                    toRemoveCache.Clear();
                }
            }
        }
    }
}
