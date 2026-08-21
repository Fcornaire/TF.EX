using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.Patchs.Calc;
using TF.State.TowerFallExtensions.Entity;
using TF.State.TowerFallExtensions.Entity.LevelEntity;
using TowerFall;

using TF.State.Domain.Context;
namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Spikeball))]
    internal class SpikeballPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Vector2), typeof(bool)])]
        public static void Spikeball_ctor_Vector2_Vector2_bool(Spikeball __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dynSpikeball = DynamicData.For(__instance);

            CalcPatch.RegisterRng();
            dynSpikeball.Set("spinDir", Monocle.Calc.Random.Choose(1, -1));
            CalcPatch.UnregisterRng();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool Spikeball_Update(Spikeball __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            if (StateFlags.IsRestoring)
            {
                return false;
            }

            __instance.DeleteAllComponents<Coroutine>();
            __instance.DeleteAllComponents<Tween>();

            var dyn = DynamicData.For(__instance);
            if (dyn.Get<bool>("exploding"))
            {
                var timer = SpikeballOwnedState.GetSpinTimer(__instance);
                if (timer < 0f)
                {
                    if (__instance.Level.Session.RoundLogic.RoundStarted)
                    {
                        timer = 0f;
                        SpikeballOwnedState.SetSpinTimer(__instance, timer);
                        SpikeballOwnedState.ApplySpinRate(__instance, timer);
                    }
                }
                else
                {
                    timer += TFGame.TimeMult;
                    SpikeballOwnedState.SetSpinTimer(__instance, timer);
                    SpikeballOwnedState.ApplySpinRate(__instance, timer);
                }
            }

            return true;
        }
    }
}
