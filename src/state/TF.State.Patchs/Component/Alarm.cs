using HarmonyLib;
using Monocle;
using TF.State.Domain;
using TF.State.Domain.Context;

namespace TF.State.Patchs.Component
{
    [HarmonyPatch(typeof(Alarm))]
    public class AlarmPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Alarm.Removed))]
        public static void Alarm_Removed(Alarm __instance)
        {

            if (StateFlags.IsCaptureActive)
            {
                var cached = Traverse.Create(__instance).Field("cached").GetValue<Stack<Alarm>>();

                if (cached != null && cached.Any())
                {
                    cached.Clear();
                }
            }
        }
    }
}
