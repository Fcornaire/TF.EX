using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class NetplayButton : MainModeButton
    {
        private readonly Image icon;
        private readonly Action onConfirm;

        public override float BaseScale => 1.5f;

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

        public NetplayButton(Vector2 position, Vector2 tweenFrom, Action onConfirm) : base(position, tweenFrom, "NETPLAY", "ONLINE MATCHS")
        {
            this.onConfirm = onConfirm;
            icon = new Image(MenuIcons.Online());
            icon.CenterOrigin();
            Add(icon);
        }

        protected override void MenuAction()
        {
            MainMenu.CurrentMatchSettings = MainMenu.VersusMatchSettings;
            MainMenu.RollcallMode = MainMenu.RollcallModes.Versus;
            onConfirm?.Invoke();
        }

        public override void Render()
        {
            icon.DrawOutline();
            base.Render();
        }
    }
}
