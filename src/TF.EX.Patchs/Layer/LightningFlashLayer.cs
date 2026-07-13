using HarmonyLib;
using Monocle;

namespace TF.EX.Patchs.Layer
{
    [HarmonyPatch(typeof(TowerFall.Background.LightningFlashLayer))]
    internal class LightningFlashLayerPatch
    {
        private static Random _random = new();

        //Use custom random range to avoid using calc random which is used deterministically
        [HarmonyPostfix]
        [HarmonyPatch("SequenceD")]
        public static void LightningFlashLayer_SequenceD(TowerFall.Background.LightningFlashLayer __instance)
        {
            var alarm = Traverse.Create(__instance).Field("alarm").GetValue<Alarm>();

            alarm.Start(_random.Range(500, 800));
        }

        [HarmonyPostfix]
        [HarmonyPatch("SequenceC")]
        public static void LightningFlashLayer_SequenceC(TowerFall.Background.LightningFlashLayer __instance)
        {
            var alarm = Traverse.Create(__instance).Field("alarm").GetValue<Alarm>();

            alarm.Start(_random.Range(4, 10));
        }

        [HarmonyPostfix]
        [HarmonyPatch("SequenceB")]
        public static void LightningFlashLayer_SequenceB(TowerFall.Background.LightningFlashLayer __instance)
        {
            var alarm = Traverse.Create(__instance).Field("alarm").GetValue<Alarm>();

            alarm.Start(_random.Range(6, 10));
        }
    }
}
