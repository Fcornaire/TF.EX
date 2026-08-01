using HarmonyLib;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.TowerFallExtensions;
using TowerFall;

using TF.State.Domain.Context;
namespace TF.State.Patchs.RoundLogic
{
    [HarmonyPatch(typeof(LastManStandingRoundLogic))]
    internal class LastManStandingRoundLogicPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnLevelLoadFinish")]
        public static bool LastManStandingRoundLogic_OnLevelLoadFinish()
        {

            if (!StateFlags.IsRollbackFrame) //Prevent adding a VersusStart on a rollback frame
            {
                return true;
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(LastManStandingRoundLogic.OnRoundStart))]
        public static void LastManStandingRoundLogic_OnRoundStart(LastManStandingRoundLogic __instance)
        {

            if (StateFlags.IsTestMode)
            {
                var dump = EntityDumper.Dump(__instance.Session.CurrentLevel);

                if (!Directory.Exists($"{Directory.GetCurrentDirectory()}\\EntitiesDump"))
                {
                    Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\EntitiesDump");
                }

                var map = (__instance.Session?.MatchSettings?.LevelSystem as VersusLevelSystem).LastLevel.Split("\\").Last().Split(".").FirstOrDefault();
                var path = Path.Combine($"{Directory.GetCurrentDirectory()}\\EntitiesDump", $"{__instance.Session?.MatchSettings?.LevelSystem?.Theme?.Name}-{map}");
                File.WriteAllText(path, dump);
            }
        }

    }
}
