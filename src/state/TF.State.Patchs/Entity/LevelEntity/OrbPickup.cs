using HarmonyLib;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(OrbPickup))]
    internal class OrbPickupPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(OrbPickup.OnPlayerCollide))]
        public static void OrbPickup_OnPlayerCollide_Prefix(out LocalSettingsGuard.Snapshot __state)
        {
            __state = LocalSettingsGuard.Neutralize();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(OrbPickup.OnPlayerCollide))]
        public static void OrbPickup_OnPlayerCollide_Postfix(LocalSettingsGuard.Snapshot __state)
        {
            LocalSettingsGuard.Restore(__state);
        }
    }
}
