using Microsoft.Xna.Framework;
using Monocle;

namespace TF.EX.Patchs.Entity.MenuItem
{
    internal class LoaderPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.Loader.TweenIn += Loader_TweenIn;
        }

        public void Unload()
        {
            On.TowerFall.Loader.TweenIn -= Loader_TweenIn;
        }

        private void Loader_TweenIn(On.TowerFall.Loader.orig_TweenIn orig, TowerFall.Loader self)
        {
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 12, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                self.Position = Vector2.Lerp(new Vector2(160f, 280f), new Vector2(160f, 120f), t.Eased);
            };
            self.Add(tween);
        }
    }
}
