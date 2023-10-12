using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVarianText : MenuItem
    {
        private Vector2 tweenTo;

        private Vector2 tweenFrom;


        public LobbyVarianText(Vector2 position, Vector2 tweenFrom) : base(position)
        {
            tweenTo = position;
            this.tweenFrom = tweenFrom;
        }

        public override void TweenIn()
        {
            Position = tweenFrom;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 20, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                Position = Vector2.Lerp(tweenFrom, tweenTo, t.Eased);
            };
            Add(tween);
        }

        public override void TweenOut()
        {
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 12, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                Position = Vector2.Lerp(tweenTo, tweenFrom, t.Eased);
            };
            Add(tween);
        }

        public override void Render()
        {
            base.Render();

            Draw.OutlineTextCentered(TFGame.Font, "VARIANTS", Position, Calc.HexToColor("3CBCFC"), 2f);
        }

        protected override void OnConfirm()
        {
        }

        protected override void OnDeselect()
        {
        }

        protected override void OnSelect()
        {
        }
    }
}
