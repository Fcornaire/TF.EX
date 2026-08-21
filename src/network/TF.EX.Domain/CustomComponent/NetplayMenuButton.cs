using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class NetplayMenuButton : MainModeButton
    {
        private readonly Image icon;
        private readonly Action onConfirm;

        public override float BaseScale => 1f;

        public override float ImageScale
        {
            get { return icon.Scale.X; }
            set { icon.Scale = Vector2.One * value; }
        }

        public override float ImageRotation
        {
            get { return icon.Rotation; }
            set { icon.Rotation = value; }
        }

        public override float ImageY
        {
            get { return icon.Y; }
            set { icon.Y = value; }
        }

        public NetplayMenuButton(Vector2 position, Vector2 tweenFrom, string title, string subtitle, Subtexture iconTexture, Action onConfirm)
            : base(position, tweenFrom, title, subtitle)
        {
            this.onConfirm = onConfirm;
            icon = new Image(iconTexture);
            icon.CenterOrigin();
            Add(icon);
        }

        protected override void MenuAction()
        {
            onConfirm?.Invoke();
        }

        public override void Render()
        {
            icon.DrawOutline();
            base.Render();
        }
    }
}
