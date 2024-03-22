using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Ports;

namespace TF.EX.Patchs.Component
{
    public class AlarmPatch : IHookable
    {
        private readonly INetplayManager netplayManager;

        public AlarmPatch(INetplayManager netplayManager)
        {
            this.netplayManager = netplayManager;
        }

        public void Load()
        {
            On.Monocle.Alarm.Removed += Alarm_Removed;
        }

        public void Unload()
        {
            On.Monocle.Alarm.Removed -= Alarm_Removed;
        }

        private void Alarm_Removed(On.Monocle.Alarm.orig_Removed orig, Monocle.Alarm self)
        {
            orig(self);

            if (this.netplayManager.IsInit())
            {
                var dynAlarm = DynamicData.For(self);
                Stack<Alarm> cached = dynAlarm.Get<Stack<Alarm>>("cached");

                if (cached != null && cached.Any())
                {
                    cached.Clear();
                }
            }
        }
    }
}
