using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LocalVersusButton : MainModeButton
    {
        private readonly Image leftArrow;
        private readonly Image rightArrow;

        public override float BaseScale => 1f;

        public override float ImageScale
        {
            get { return leftArrow.Scale.X; }
            set { leftArrow.Scale = rightArrow.Scale = Vector2.One * value; }
        }

        public override float ImageRotation
        {
            get { return leftArrow.Rotation; }
            set { leftArrow.Rotation = rightArrow.Rotation = value; }
        }

        public override float ImageY
        {
            get { return leftArrow.Y; }
            set { leftArrow.Y = rightArrow.Y = value; }
        }

        public LocalVersusButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, "VERSUS", "2-4 ARCHERS")
        {
            leftArrow = new Image(TFGame.Atlas["versus/introArrow"]);
            leftArrow.CenterOrigin();
            Add(leftArrow);

            rightArrow = new Image(TFGame.Atlas["versus/introArrow"]);
            rightArrow.FlipX = true;
            rightArrow.CenterOrigin();
            Add(rightArrow);
        }

        protected override void MenuAction()
        {
            MainMenu.VersusMatchSettings.ClearNetplayMode();

            MainMenu.CurrentMatchSettings = MainMenu.VersusMatchSettings;
            MainMenu.RollcallMode = MainMenu.RollcallModes.Versus;
            base.MainMenu.State = MainMenu.MenuState.Rollcall;
        }

        public override void Render()
        {
            leftArrow.DrawOutline();
            rightArrow.DrawOutline();
            base.Render();
        }
    }
}
