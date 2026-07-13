using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.EX.Domain;
using TF.EX.Domain.Models.State;

namespace TF.EX.Patchs.Calc
{
    [HarmonyPatch(typeof(Monocle.Calc))]
    public class CalcPatch
    {
        private static bool _shouldRegisterRng = false;
        private static bool _shouldIgnoreToRegisterRng = false;
        private static int _rngRegisteringCount = 0;

        public static void RegisterRng()
        {
            _shouldRegisterRng = true;
            _rngRegisteringCount++;
        }

        public static void UnregisterRng()
        {
            _rngRegisteringCount--;

            if (_rngRegisteringCount < 0)
            {
                _rngRegisteringCount = 0;
            }

            if (_rngRegisteringCount == 0)
            {
                _shouldRegisterRng = false;
            }
        }

        public static void Reset()
        {
            _shouldIgnoreToRegisterRng = false;
            _shouldRegisterRng = false;
            _rngRegisteringCount = 0;
        }

        public static void IgnoreToRegisterRng()
        {
            _shouldIgnoreToRegisterRng = true;
        }

        public static void UnignoreToRegisterRng()
        {
            _shouldIgnoreToRegisterRng = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Range", [typeof(Random), typeof(Vector2), typeof(Vector2)])]
        public static void Range_Random_Vector2_Vector2(ref Random random)
        {
            var rngService = ServiceCollections.ResolveRngService();

            if (_shouldRegisterRng && !_shouldIgnoreToRegisterRng)
            {
                rngService.Get().ResetRandom(ref Monocle.Calc.Random);
                random = Monocle.Calc.Random;
                rngService.AddGen(RngGenType.Double);
            }
        }

        /// <summary>
        /// Register the shuffle of the given list.
        /// <para>Needed because Calc.Shuffle is not hookable (generics)</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toShuffle"></param>
        public static void RegisterShuffle<T>(IEnumerable<T> toShuffle)
        {
            var rngService = ServiceCollections.ResolveRngService();

            rngService.Get().ResetRandom(ref Monocle.Calc.Random);
            int length = toShuffle.Count();
            while (--length > 0)
            {
                rngService.AddGen(RngGenType.Integer);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("NextFloat", [typeof(Random)])]
        public static void NextFloat_Patch(ref Random random)
        {
            var rngService = ServiceCollections.ResolveRngService();

            if (_shouldRegisterRng && !_shouldIgnoreToRegisterRng)
            {
                rngService.Get().ResetRandom(ref Monocle.Calc.Random);
                random = Monocle.Calc.Random;
                rngService.AddGen(RngGenType.Double);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Range", [typeof(Random), typeof(int), typeof(int)])]
        public static void RangleIntInt_Patch(ref Random random)
        {
            var rngService = ServiceCollections.ResolveRngService();

            if (_shouldRegisterRng && !_shouldIgnoreToRegisterRng)
            {
                rngService.Get().ResetRandom(ref Monocle.Calc.Random);
                random = Monocle.Calc.Random;
                rngService.AddGen(RngGenType.Integer);
            }
        }
    }
}
