using HarmonyLib;
using TF.InputDisplayer.Domain;
using TowerFall;

namespace TF.InputDisplayer.Patchs
{
    [HarmonyPatch(typeof(Session))]
    internal static class SessionPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("StartRound")]
        public static void Session_StartRound() => Rounds.Mark();
    }
}
