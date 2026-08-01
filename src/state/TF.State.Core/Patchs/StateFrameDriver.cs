using HarmonyLib;
using TF.State.Domain.Context;

namespace TF.State.Core.Patchs
{
    [HarmonyPatch(typeof(TowerFall.Level))]
    internal static class StateFrameDriverPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(TowerFall.Level.Update))]
        public static void Level_Update_Postfix()
        {
            if (StateFlags.FrameDriverOwner != null)
            {
                return;
            }

            StateFlags.CurrentFrame++;
        }
    }
}
