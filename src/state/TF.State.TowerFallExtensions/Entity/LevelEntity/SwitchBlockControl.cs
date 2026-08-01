using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class SwitchBlockControlExtensions
    {
        public static SwitchBlockControl GetState(this TowerFall.SwitchBlockControl entity)
        {
            var dyn = DynamicData.For(entity);
            var timer = dyn.Get<Counter>("timer");

            return new SwitchBlockControl
            {
                Timer = timer.Value,
            };
        }

        public static void LoadState(this TowerFall.SwitchBlockControl entity, SwitchBlockControl toLoad)
        {
            var dyn = DynamicData.For(entity);
            var timer = dyn.Get<Counter>("timer");

            DynamicData.For(timer).Set("counter", toLoad.Timer);
        }
    }
}
