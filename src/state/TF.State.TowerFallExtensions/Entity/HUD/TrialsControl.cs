using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.State.Domain.Models.Entity.HUD;

namespace TF.State.TowerFallExtensions.Entity.HUD
{
    public static class TrialsControlExtensions
    {
        private const int IconWidth = 28;

        public static TrialsControl GetState(this TowerFall.TrialsControl entity)
        {
            var dyn = DynamicData.For(entity);

            return new TrialsControl
            {
                Time = dyn.Get<TimeSpan>("time").Ticks,
                Started = entity.Started,
                CanEnd = dyn.Get<bool>("canEnd"),
                Drawing = dyn.Get<int>("drawing"),
            };
        }

        public static void LoadState(this TowerFall.TrialsControl entity, TrialsControl toLoad, int liveTargets)
        {
            var dyn = DynamicData.For(entity);

            dyn.Set("time", TimeSpan.FromTicks(toLoad.Time));
            dyn.Set("canEnd", toLoad.CanEnd);

            dyn.Set("Started", toLoad.Started);

            dyn.Set("drawing", liveTargets);

            RestoreCounterIcons(dyn, liveTargets);
        }

        private static void RestoreCounterIcons(DynamicData dyn, int drawing)
        {
            var images = dyn.Get<Monocle.Image[]>("images");

            if (images == null)
            {
                return;
            }

            var uncrossed = TowerFall.TFGame.Atlas["dummy/hitCounter"]
                .GetAbsoluteClipRect(new Rectangle(0, 0, IconWidth, IconWidth));

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                {
                    continue;
                }

                var rect = uncrossed;

                if (i >= drawing)
                {
                    rect.X += IconWidth;
                }

                images[i].ClipRect = rect;
            }
        }
    }
}
