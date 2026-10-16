using HarmonyLib;
using System.Reflection;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs
{
    [HarmonyPatch(typeof(SaveData))]
    internal static class SaveDataPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SaveData.Save))]
        public static bool SaveData_Save(ref string __result)
        {
            if (!SaveProgressGuard.IsSuppressing)
            {
                return true;
            }

            __result = null;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class SteamStatsPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.TimesLaunched));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.TotalVersusKills));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.MatchesPlayed));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.RoundsPlayed));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.ArrowsShot));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.ArrowsCollected));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.ArrowsCaught));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.TreasuresTaken));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.Dodges));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.Jumps));
            yield return AccessTools.PropertySetter(typeof(GameStats), nameof(GameStats.VersusRandomPlays));
            yield return AccessTools.Method(typeof(GameStats), nameof(GameStats.RegisterVersusTowerSelection));
            yield return AccessTools.Method(typeof(GameStats), nameof(GameStats.RegisterVersusKill));
            yield return AccessTools.Method(typeof(UnlockData), nameof(UnlockData.UnlockAchievement));
        }

        public static bool Prefix() => !SaveProgressGuard.IsSuppressing;
    }
}
