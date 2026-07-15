using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Patchs.Calc;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Spikeball))]
    internal class SpikeballPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Vector2), typeof(bool)])]
        public static void Spikeball_ctor_Vector2_Vector2_bool(Spikeball __instance)
        {
            var dynSpikeball = DynamicData.For(__instance);

            CalcPatch.RegisterRng();
            dynSpikeball.Set("spinDir", Monocle.Calc.Random.Choose(1, -1));
            CalcPatch.UnregisterRng();
        }
    }
}
