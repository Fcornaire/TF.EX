using MonoMod.Utils;
using TF.State.Domain.Models.Component;

namespace TF.State.TowerFallExtensions.ComponentExtensions
{
    public static class FireControlExtensions
    {
        public static FireControl GetState(this TowerFall.FireControl fireControl)
        {
            var dynFireControl = DynamicData.For(fireControl);
            var counter = dynFireControl.Get<Monocle.Counter>("counter");

            return new FireControl
            {
                Counter = counter.GetState()
            };
        }

        public static void LoadState(this TowerFall.FireControl fireControl, FireControl toLoad)
        {
            var dynFireControl = DynamicData.For(fireControl);
            var counter = dynFireControl.Get<Monocle.Counter>("counter");

            counter.LoadState(toLoad.Counter);
        }
    }
}
