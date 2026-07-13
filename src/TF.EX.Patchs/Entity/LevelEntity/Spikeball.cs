using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Spikeball))]
    internal class SpikeballPatch
    {
        /// <summary>
        /// Patch fot spawning spikeball to use our RNG
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Vector2), typeof(bool)])]
        public static void Spikeball_ctor_Vector2_Vector2_bool(Spikeball __instance)
        {
            var rngService = ServiceCollections.ResolveRngService();

            rngService.Get().ResetRandom(ref Monocle.Calc.Random);
            var dynSpikeball = DynamicData.For(__instance);
            dynSpikeball.Set("spinDir", Monocle.Calc.Random.Choose(1, -1));
            rngService.AddGen(Domain.Models.State.RngGenType.Integer);
        }
    }
}
