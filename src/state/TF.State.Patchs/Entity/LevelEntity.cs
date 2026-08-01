using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.State.Domain.Context;

namespace TF.State.Patchs.Entity
{
    [HarmonyPatch(typeof(TowerFall.LevelEntity))]
    internal class LevelEntityPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Render")]
        public static void LevelEntity_Render_Prefix(TowerFall.LevelEntity __instance, out Vector2 __state)
        {
            __state = __instance.Position;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Render")]
        public static void LevelEntity_Render_Postfix(TowerFall.LevelEntity __instance, Vector2 __state)
        {
            if (StateFlags.IsCaptureActive)
            {
                __instance.Position = __state;
            }
        }
    }
}
