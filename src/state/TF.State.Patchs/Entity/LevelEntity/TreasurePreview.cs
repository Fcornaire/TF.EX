using HarmonyLib;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TreasurePreview))]
    public class TreasurePreviewPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, typeof(Vector2), typeof(bool))]
        public static void TreasurePreview_ctor(TreasurePreview __instance)
        {
            DynamicData.For(__instance).Add("previewChestDepth", -1.0);
        }
    }
}
