using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class Fader : Entity
    {
        private float alpha;

        public Fader(int layerIndex = 0, int depth = 10000) : base(layerIndex)
        {
            base.Depth = depth;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 50, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                alpha = MathHelper.Lerp(0f, 0.4f, t.Eased);
            };
            Add(tween);
        }

        public override void Render()
        {
            Draw.Rect(0f, 0f, TFGame.Instance.Screen.Width, TFGame.Instance.Screen.Height, Color.Black * alpha);
        }
    }
}
