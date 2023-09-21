using MonoMod.Utils;
using TF.EX.Domain.Models.State.Monocle;

namespace TF.EX.TowerFallExtensions.MonocleExtensions
{
    public static class CounterExtensions
    {
        public static Counter GetState(this Monocle.Counter counter)
        {
            var dynCounter = DynamicData.For(counter);

            return new Counter
            {
                CounterValue = dynCounter.Get<float>("counter")
            };
        }

        public static void LoadState(this Monocle.Counter counter, Counter toLoad)
        {
            var dynCounter = DynamicData.For(counter);
            dynCounter.Set("counter", toLoad.CounterValue);
        }
    }
}
