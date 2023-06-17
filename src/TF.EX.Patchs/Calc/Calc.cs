using TF.EX.Domain;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.Calc
{
    public class CalcPatch : IHookable
    {
        public static bool ShouldRegisterRng = false;

        private readonly IRngService _rngService;

        public CalcPatch(IRngService rngService)
        {
            _rngService = rngService;
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

            rngService.Get().ResetRandom();
            int length = toShuffle.Count();
            while (--length > 0)
            {
                rngService.AddGen(Domain.Models.State.RngGenType.Integer);
            }
        }

        private float NextFloat_Patch(On.Monocle.Calc.orig_NextFloat_Random orig, Random random)
        {
            if (ShouldRegisterRng)
            {
                _rngService.Get().ResetRandom();
                random = Monocle.Calc.Random;
                _rngService.AddGen(Domain.Models.State.RngGenType.Double);
            }

            return orig(random);
        }

        private int RangleIntInt_Patch(On.Monocle.Calc.orig_Range_Random_int_int orig, Random random, int min, int add)
        {
            if (ShouldRegisterRng)
            {
                _rngService.Get().ResetRandom();
                random = Monocle.Calc.Random;
                _rngService.AddGen(Domain.Models.State.RngGenType.Integer);
            }

            return orig(random, min, add);
        }
    }
}
