using MonoMod.Utils;
using TF.State.Domain.Models.Component;

namespace TF.State.TowerFallExtensions.ComponentExtensions
{
    public static class SineWaveExtensions
    {
        public static SineWave GetState(this Monocle.SineWave sineWave)
        {
            return new SineWave
            {
                Counter = sineWave.Counter
            };
        }

        public static void LoadState(this Monocle.SineWave sineWave, SineWave toLoad)
        {
            var dynSineWave = DynamicData.For(sineWave);
            dynSineWave.Set("Counter", toLoad.Counter);
            dynSineWave.Set("Value", (float)Domain.DeterministicMath.Sin(toLoad.Counter));
            dynSineWave.Set("ValueOverTwo", (float)Domain.DeterministicMath.Sin(toLoad.Counter / 2f));
            dynSineWave.Set("TwoValue", (float)Domain.DeterministicMath.Sin(toLoad.Counter * 2f));
        }
    }
}
