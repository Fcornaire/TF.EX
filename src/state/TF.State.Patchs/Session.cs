using HarmonyLib;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.Domain.Extensions;
using TF.State.Patchs.Calc;
using TowerFall;

namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(Session))]
    public class SessionStatePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("StartGame")]
        public static void Session_StartGame(Session __instance)
        {
            var rngService = ServiceCollections.ResolveRngService();

            CalcPatch.Reset();
            rngService.Reset();

            if (TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay() || StateFlags.IsTestMode)
            {
                __instance.MatchSettings.RandomLevelSeed = rngService.GetSeed();
            }
        }
    }
}
