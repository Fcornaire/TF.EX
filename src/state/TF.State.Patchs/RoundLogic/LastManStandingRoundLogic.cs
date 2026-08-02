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

                var folder = Path.Combine(Directory.GetCurrentDirectory(), "EntitiesDump");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var map = Path.GetFileNameWithoutExtension((__instance.Session?.MatchSettings?.LevelSystem as VersusLevelSystem).LastLevel.Replace('\\', '/'));
                var path = Path.Combine(folder, $"{__instance.Session?.MatchSettings?.LevelSystem?.Theme?.Name}-{map}");
                File.WriteAllText(path, dump);
            }
        }

    }
}
