using HarmonyLib;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Context;
using TowerFall;

namespace TF.EX.Patchs.Component
{
    [HarmonyPatch(typeof(PlayerIndicator))]
    public class PlayerIndicatorPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch([typeof(Vector2), typeof(int), typeof(bool)])]
        public static void PlayerIndicator_ctor(PlayerIndicator __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (!ExFlags.IsCaptureActive)
            {
                return;
            }

            var dynPlayerIndcator = Traverse.Create(__instance);
            var text = dynPlayerIndcator.Field("text").GetValue<string>();
            var playerIndex = dynPlayerIndcator.Field("playerIndex").GetValue<int>();

            text = netplayManager.GetNameForSeat(playerIndex);

            dynPlayerIndcator.Field("text").SetValue(text);
        }
    }
}
