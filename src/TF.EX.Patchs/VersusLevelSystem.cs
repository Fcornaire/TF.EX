using HarmonyLib;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(VersusLevelSystem))]
    public class VersusLevelSystemPatch
    {
        /// <summary>Same as original but using custom one since the original is not using random from calc</summary>
        [HarmonyPrefix]
        [HarmonyPatch("GenLevels")]
        public static bool VersusLevelSystem_GenLevels(VersusLevelSystem __instance, MatchSettings matchSettings)
        {
            var logger = ServiceCollections.ResolveLogger();
            var rngService = ServiceCollections.ResolveRngService();

            var dynVersusLevelSystem = DynamicData.For(__instance);
            var lastLevel = dynVersusLevelSystem.Get<string>("lastLevel");
            var levels = __instance.OwnGenLevel(matchSettings, __instance.VersusTowerData, lastLevel, rngService);

            logger.LogDebug<VersusLevelSystemPatch>($"Generated levels: {string.Join("\n", levels)}");

            dynVersusLevelSystem.Set("levels", levels);

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(VersusTowerData)])]
        public static void VersusLevelSystem_ctor(VersusLevelSystem __instance)
        {
            var dynVersusLevelSystem = Traverse.Create(__instance);
            dynVersusLevelSystem.Property("ShowControls").SetValue(false);
            dynVersusLevelSystem.Property("ShowTriggerControls").SetValue(false);
        }
    }
}
