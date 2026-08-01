using HarmonyLib;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.TowerFallExtensions.Layer;
using TowerFall;

using TF.State.Domain.Context;
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

        [HarmonyPrefix]
        [HarmonyPatch("UpdateEntityList")]
        public static void Layer_UpdateEntityList(Monocle.Layer __instance)
        {
            if (__instance.IsGameplayLayer())
            {
                if (StateFlags.IsRollbackFrame && !(TFGame.Instance.Scene is LevelLoaderXML)) //Remove entities from the precedent frame (but on a level only)
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
