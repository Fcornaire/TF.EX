using HarmonyLib;
using Monocle;
using MonoMod.Utils;
using System.Collections;
using TF.EX.Domain;

namespace TF.EX.Patchs.Component
{
    [HarmonyPatch(typeof(Coroutine))]
    public class CoroutinePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(IEnumerator)])]
        public static void Coroutine_ctor_IEnumerator(Coroutine __instance, IEnumerator functionCall)
        {
            var dynCoroutine = DynamicData.For(__instance);
            dynCoroutine.Set("NAME", functionCall.GetType().Name);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool Coroutine_Update(Coroutine __instance)
        {
            if (__instance.Entity is TowerFall.Miasma)
            {
                // The miasma sequence is now driven by MiasmaPatch/MiasmaSequenceController
                return false;
            }

            if (__instance.Entity is TowerFall.MoonGlassBlock)
            {
                // The shatter is driven by MoonGlassBlockExplodeController, offline stays vanilla.
                return !ServiceCollections.ResolveNetplayManager().IsInit();
            }

            var hudService = ServiceCollections.ResolveHUDService();

            if (__instance.Entity is TowerFall.VersusStart)
            {
                var dynCoroutine = DynamicData.For(__instance);
                if (dynCoroutine.TryGet("NAME", out string name))
                {
                    if (name.Contains("SetupSequence"))
                    {
                        var hud = hudService.Get();
                        hud.VersusStart.CoroutineState += 1;
                        hudService.Update(hud);
                    }
                }
            }

            if (__instance.Entity is TowerFall.VersusRoundResults)
            {
                var hud = hudService.Get();
                hud.VersusRoundResults.CoroutineState += 1;

                hudService.Update(hud);
            }

            return true;
        }
    }
}
