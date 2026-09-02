using HarmonyLib;
using TF.State.Domain.Context;

namespace TF.State.Patchs.Scene
{
    [HarmonyPatch(typeof(TowerFall.Level))]
    internal class LevelScreenShakePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ScreenShake")]
        public static bool Level_ScreenShake()
        {
            return !StateFlags.IsRestoring;
        }
    }
}
