using HarmonyLib;
using MonoMod.Utils;
using TF.State.Domain.Logging;
using TF.State.Domain;
using TowerFall;

namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(VersusLevelSystem))]
    public class VersusLevelSystemPatch
    {
        //>Same as original but using custom one since the original is not using random from calc
        [HarmonyPrefix]
        [HarmonyPatch("GenLevels")]
        public static bool VersusLevelSystem_GenLevels(VersusLevelSystem __instance, MatchSettings matchSettings)
        {
            if (!TF.State.Domain.Context.StateFlags.IsCaptureActive)
            {
                return true;
            }

            if (TF.State.Domain.Context.ScenarioLevels.IsActive)
            {
                DynamicData.For(__instance).Set("levels", TF.State.Domain.Context.ScenarioLevels.Levels.ToList());

                return false;
            }

            var logger = ServiceCollections.ResolveLogger();
            var rngService = ServiceCollections.ResolveRngService();

            var dynVersusLevelSystem = DynamicData.For(__instance);
            var lastLevel = dynVersusLevelSystem.Get<string>("lastLevel");
            var levels = __instance.OwnGenLevel(matchSettings, __instance.VersusTowerData, lastLevel, rngService);

            logger.LogDebug<VersusLevelSystemPatch>($"Generated {levels.Count()} levels for {__instance.VersusTowerData.Theme.Name}");

            dynVersusLevelSystem.Set("levels", levels);

            return false;
        }
    }
}
