using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.EX.Domain;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Chain))]
    internal class ChainPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(int)])]
        public static void Chain_ctor_Vector2_int_Prefix()
        {
            var rngService = ServiceCollections.ResolveRngService();
            rngService.Get().ResetRandom(ref Monocle.Calc.Random);
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(int)])]
        public static void Chain_ctor_Vector2_int_Postfix()
        {
            var rngService = ServiceCollections.ResolveRngService();
            rngService.AddGen(RngGenType.Integer);
        }
    }
}
