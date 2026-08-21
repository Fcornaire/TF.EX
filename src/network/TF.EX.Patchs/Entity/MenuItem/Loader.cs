using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(TowerFall.Loader))]
    internal class LoaderPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("TweenIn")]
        public static bool Loader_TweenIn(TowerFall.Loader __instance)
        {
            var mode = TowerFall.MainMenu.VersusMatchSettings?.Mode;
            if (mode?.IsNetplay() != true && ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty)
            {
                return true;
            }

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
