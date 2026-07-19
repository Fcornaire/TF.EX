using MonoMod.Utils;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.TowerFallExtensions.ComponentExtensions
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
            dynSineWave.Set("Value", (float)Math.Sin(toLoad.Counter));
            dynSineWave.Set("ValueOverTwo", (float)Math.Sin(toLoad.Counter / 2f));
            dynSineWave.Set("TwoValue", (float)Math.Sin(toLoad.Counter * 2f));
        }
    }
}
