using MonoMod.Utils;
using TF.State.Domain.Models.Component;

namespace TF.State.TowerFallExtensions.ComponentExtensions
{
    public static class WigglerExtensions
    {
        public static Wiggler GetState(this Monocle.Wiggler wiggler)
        {
            return new Wiggler
            {
                Counter = wiggler.Counter,
                SineCounter = DynamicData.For(wiggler).Get<float>("sineCounter"),
                Value = wiggler.Value,
                Active = wiggler.Active
            };
        }

        public static void LoadState(this Monocle.Wiggler wiggler, Wiggler toLoad)
        {
            var dynWiggler = DynamicData.For(wiggler);
            dynWiggler.Set("Counter", toLoad.Counter);
            dynWiggler.Set("sineCounter", toLoad.SineCounter);
            dynWiggler.Set("Value", toLoad.Value);
            wiggler.Active = toLoad.Active;
        }
    }
}
