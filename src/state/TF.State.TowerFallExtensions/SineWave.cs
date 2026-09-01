using MonoMod.Utils;

namespace TF.State.TowerFallExtensions
{
    public static class SineWaveExtensions
    {
        public static void UpdateAttributes(this Monocle.SineWave self, float counter)
        {
            var dynSine = DynamicData.For(self);

            dynSine.Set("Counter", counter);
            dynSine.Set("Value", (float)Domain.DeterministicMath.Sin(counter));
            dynSine.Set("ValueOverTwo", (float)Domain.DeterministicMath.Sin(counter / 2f));
            dynSine.Set("TwoValue", (float)Domain.DeterministicMath.Sin(counter * 2f));
        }
    }
}
