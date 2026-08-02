using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class OnlineVersusButton : MainModeButton
    {
        private readonly Image icon;

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

        public OnlineVersusButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, "ONLINE", "2 ARCHERS")
        {
            icon = new Image(MenuIcons.Online());
            icon.CenterOrigin();
            Add(icon);
        }

        protected override void MenuAction()
        {
            MainMenu.CurrentMatchSettings = MainMenu.VersusMatchSettings;
            MainMenu.RollcallMode = MainMenu.RollcallModes.Versus;
            base.MainMenu.State = Models.MenuState.LobbyBrowser.ToTFModel();
        }

        public override void Render()
        {
            icon.DrawOutline();
            base.Render();
        }
    }
}
