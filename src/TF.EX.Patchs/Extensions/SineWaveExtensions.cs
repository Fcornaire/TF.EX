using Monocle;
using MonoMod.Utils;

namespace TF.EX.Patchs.Extensions
{
    public static class SineWaveExtensions
    {
        public static void UpdateAttributes(this SineWave self, float counter)
        {
            var dynSine = DynamicData.For(self);

            dynSine.Set("Counter", counter);
            dynSine.Set("Value", (float)Math.Sin(counter));
            dynSine.Set("ValueOverTwo", (float)Math.Sin(counter / 2f));
            dynSine.Set("TwoValue", (float)Math.Sin(counter * 2f));
        }
    }
}
