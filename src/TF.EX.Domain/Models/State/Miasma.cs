namespace TF.EX.Domain.Models.State
{
    public class Miasma
    {
        public float Counter { get; set; }
        public MiasmaState State { get; set; }
        public float Percent { get; set; }
        public int CoroutineTimer { get; set; }
        public bool IsCollidable { get; set; }
        public float SineTentaclesWaveCounter { get; set; }
        public float SineCounter { get; set; }

        public static Miasma Default()
        {
            return new Miasma
            {
                Counter = Constants.DEFAULT_MIASMA_COUNTER,
                State = MiasmaState.Uninitialized,
                Percent = Constants.DEFAULT_MIASMA_PERCENT,
                CoroutineTimer = Constants.DEFAULT_COUROUTINE_TIMER,
                IsCollidable = false,
                SineTentaclesWaveCounter = 0.0f,
                SineCounter = 0.0f
            };
        }
    }
}
