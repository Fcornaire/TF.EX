using HarmonyLib;
using TF.EX.Domain;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.RoundLogic
{
    [HarmonyPatch(typeof(LastManStandingRoundLogic))]
    internal class LastManStandingRoundLogicPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnLevelLoadFinish")]
        public static bool LastManStandingRoundLogic_OnLevelLoadFinish()
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (!netplayManager.IsRollbackFrame()) //Prevent adding a VersusStart on a rollback frame
            {
                return true;
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(LastManStandingRoundLogic.OnRoundStart))]
        public static void LastManStandingRoundLogic_OnRoundStart(LastManStandingRoundLogic __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (netplayManager.IsTestMode())
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
