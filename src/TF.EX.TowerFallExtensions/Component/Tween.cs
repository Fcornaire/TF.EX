using MonoMod.Utils;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.TowerFallExtensions.ComponentExtensions
{
    public static class TweenExtensions
    {
        public static Tween GetState(this Monocle.Tween tween)
        {
            return new Tween
            {
                Duration = tween.Duration,
                FramesLeft = tween.FramesLeft,
                Active = tween.Active
            };
        }

        public static void LoadState(this Monocle.Tween tween, Tween toLoad)
        {
            var dynTween = DynamicData.For(tween);
            dynTween.Set("Duration", toLoad.Duration);
            dynTween.Set("FramesLeft", toLoad.FramesLeft);
            tween.Active = toLoad.Active;
        }
    }
}
