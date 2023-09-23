using TF.EX.Domain;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.Calc
{
    public class CalcPatch : IHookable
    {
        private static bool _shouldRegisterRng = false;
        private static bool _shouldIgnoreToRegisterRng = false;
        private static int _rngRegisteringCount = 0;

        private readonly IRngService _rngService;

        public CalcPatch(IRngService rngService)
        {
            _rngService = rngService;
        }

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

        public void Load()
        {
            On.Monocle.Calc.NextFloat_Random += NextFloat_Patch;
            On.Monocle.Calc.Range_Random_int_int += RangleIntInt_Patch;
        }

        public void Unload()
        {
            On.Monocle.Calc.NextFloat_Random -= NextFloat_Patch;
            On.Monocle.Calc.Range_Random_int_int -= RangleIntInt_Patch;
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

        private float NextFloat_Patch(On.Monocle.Calc.orig_NextFloat_Random orig, Random random)
        {
            if (_shouldRegisterRng && !_shouldIgnoreToRegisterRng)
            {
                _rngService.Get().ResetRandom(ref Monocle.Calc.Random);
                random = Monocle.Calc.Random;
                _rngService.AddGen(RngGenType.Double);
            }

            return orig(random);
        }

        private int RangleIntInt_Patch(On.Monocle.Calc.orig_Range_Random_int_int orig, Random random, int min, int add)
        {
            if (_shouldRegisterRng && !_shouldIgnoreToRegisterRng)
            {
                _rngService.Get().ResetRandom(ref Monocle.Calc.Random);
                random = Monocle.Calc.Random;
                _rngService.AddGen(RngGenType.Integer);
            }

            return orig(random, min, add);
        }
    }
}
