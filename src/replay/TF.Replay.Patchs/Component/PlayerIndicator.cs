using HarmonyLib;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Component
{
    [HarmonyPatch(typeof(PlayerIndicator))]
    internal static class PlayerIndicatorPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch([typeof(Vector2), typeof(int), typeof(bool)])]
        public static void PlayerIndicator_ctor(PlayerIndicator __instance)
        {
            if (!StandalonePlayback.IsActive)
            {
                return;
            }

            var dynIndicator = DynamicData.For(__instance);
            var name = ReplayArchers.NameForSeat(dynIndicator.Get<int>("playerIndex"));

            if (name != null)
            {
                dynIndicator.Set("text", name);
            }
        }
    }
}
