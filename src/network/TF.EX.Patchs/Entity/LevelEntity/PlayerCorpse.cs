using HarmonyLib;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(PlayerCorpse))]
    public class PlayerCorpsePatch
    {
        // Vanilla only adds the TEAM REVIVE reviver when Mode == TeamDeathmatch, but a netplay match runs under the Netplay mode
        [HarmonyPostfix]
        [HarmonyPatch("Added")]
        public static void PlayerCorpse_Added_Postfix(PlayerCorpse __instance)
        {
            var matchSettings = __instance.Level.Session.MatchSettings;

            if (__instance.PlayerIndex == -1
                || !matchSettings.Mode.IsNetplay()
                || !matchSettings.TeamMode
                || !matchSettings.Variants.TeamRevive)
            {
                return;
            }

            __instance.Level.Add(new TeamReviver(__instance, TeamReviver.Modes.TeamDeathmatch));
        }
    }
}
