using HarmonyLib;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    internal static class ScenarioPlatformHolder
    {
        public static bool HoldsStill(TowerFall.LevelEntity platform)
        {
            if (!ScenarioLevels.IsActive)
            {
                return false;
            }

            return platform.Level?.Session?.RoundLogic?.RoundStarted != true;
        }
    }

    [HarmonyPatch(typeof(LoopPlatform))]
    internal class LoopPlatformPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LoopPlatform.Update))]
        public static bool LoopPlatform_Update(LoopPlatform __instance)
        {
            return !ScenarioPlatformHolder.HoldsStill(__instance);
        }
    }

    [HarmonyPatch(typeof(MovingPlatform))]
    internal class MovingPlatformPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MovingPlatform.Update))]
        public static bool MovingPlatform_Update(MovingPlatform __instance)
        {
            return !ScenarioPlatformHolder.HoldsStill(__instance);
        }
    }

    [HarmonyPatch(typeof(ShiftBlock))]
    internal class ShiftBlockHoldPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ShiftBlock.Update))]
        public static bool ShiftBlock_Update(ShiftBlock __instance)
        {
            return !ScenarioPlatformHolder.HoldsStill(__instance);
        }
    }

    [HarmonyPatch(typeof(RotatePlatform))]
    internal class RotatePlatformPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(RotatePlatform.Update))]
        public static bool RotatePlatform_Update(RotatePlatform __instance)
        {
            return !ScenarioPlatformHolder.HoldsStill(__instance);
        }
    }
}
