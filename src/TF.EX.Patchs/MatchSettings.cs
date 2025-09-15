using HarmonyLib;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(MatchSettings))]
    internal class MatchSettingsPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("PlayerGoals")]
        public static void MatchSettings_PlayerGoals(ref int __result)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (netplayManager.IsTestMode())
            {
                __result = 2;
            }

            if (netplayManager.GetNetplayMode() == Domain.Models.NetplayMode.Local)
            {
                __result = 10;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("CleanSettingsVersus")]
        public static void MatchSettings_CleanSettingsVersus(MatchSettings __instance)
        {
            var inputService = ServiceCollections.ResolveInputService();

            if (TFGame.PlayerAmount == 0)
            {
                __instance.Mode = Modes.LastManStanding;
            }
        }
    }
}
