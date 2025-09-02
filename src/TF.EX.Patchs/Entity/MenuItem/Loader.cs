using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(TowerFall.Loader))]
    internal class LoaderPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("TweenIn")]
        public static bool Loader_TweenIn(TowerFall.Loader __instance)
        {
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 12, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                __instance.Position = Vector2.Lerp(new Vector2(160f, 280f), new Vector2(160f, 120f), t.Eased);
            };
            __instance.Add(tween);

            return false;
        }
    }
}
