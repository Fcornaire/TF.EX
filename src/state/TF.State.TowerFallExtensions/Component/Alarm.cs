using MonoMod.Utils;
using TF.State.Domain.Models.Component;

namespace TF.State.TowerFallExtensions.ComponentExtensions
{
    public static class AlarmExtensions
    {
        public static Alarm GetState(this Monocle.Alarm alarm)
        {
            return new Alarm
            {
                Duration = alarm.Duration,
                FramesLeft = alarm.FramesLeft,
                Active = alarm.Active
            };
        }

        public static void LoadState(this Monocle.Alarm alarm, Alarm toLoad)
        {
            var dynAlarm = DynamicData.For(alarm);
            dynAlarm.Set("Duration", toLoad.Duration);
            dynAlarm.Set("FramesLeft", toLoad.FramesLeft);
            alarm.Active = toLoad.Active;
        }
    }
}
