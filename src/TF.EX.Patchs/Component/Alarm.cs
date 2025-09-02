using HarmonyLib;
using Monocle;
using TF.EX.Domain;

namespace TF.EX.Patchs.Component
{
    [HarmonyPatch(typeof(Alarm))]
    public class AlarmPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Alarm.Removed))]
        public static void Alarm_Removed(Alarm __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (netplayManager.IsInit())
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
