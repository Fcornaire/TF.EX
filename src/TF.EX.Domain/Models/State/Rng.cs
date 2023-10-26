
using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class Rng
    {
        [Key(0)]
        public int Seed { get; set; } = -1;
        [Key(1)]
        public ICollection<RngGenType> Gen_type { get; set; } = new List<RngGenType>();

        public void ResetGenType()
        {
            Gen_type = new List<RngGenType>();
        }

        public void ResetRandom(ref Random random)
        {
            random = new Random(Seed);
            Reset(random);
        }

        private void Reset(Random r)
        {
            foreach (var gen in Gen_type)
            {
                switch (gen)
                {
                    case RngGenType.Integer:
                        r.Next();
                        break;
                    case RngGenType.Double:
                        r.NextDouble();
                        break;
                }
            }
        }

        public string Debug()
        {
            var counterInt = 0;
            var counterDouble = 0;
            foreach (var gen in Gen_type)
            {
                switch (gen)
                {
                    case RngGenType.Integer:
                        counterInt++;
                        break;
                    case RngGenType.Double:
                        counterDouble++;
                        break;
                }
            }

            return $"SEED: {Seed}, GEN TYPE: INT {counterInt} - DOUBLE {counterDouble}";
        }
    }

    public enum RngGenType
    {
        Integer,
        Double,
    }
}
