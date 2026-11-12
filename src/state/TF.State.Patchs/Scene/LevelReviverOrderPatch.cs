using HarmonyLib;
using TF.State.Patchs.Entity.LevelEntity;

namespace TF.State.Patchs.Scene
{
    /// <summary>
    /// Ticks the level TeamRevivers regardless of the game version
    /// Dont ask me why steam and non steam version will update TeamReviver before/after Update ¯\_(ツ)_/¯
    /// </summary>
    [HarmonyPatch(typeof(Monocle.Scene))]
    internal class LevelReviverOrderPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void Scene_Update(Monocle.Scene __instance)
        {
            if (__instance is TowerFall.Level level && !level.Frozen && !level.Paused)
            {
                TeamReviverPatch.TickRevivers(level);
            }
        }
    }
}
