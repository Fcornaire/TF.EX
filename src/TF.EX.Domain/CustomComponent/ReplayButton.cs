using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{

    public class ReplayButton : MainModeButton
    {
        private readonly Image image;

        public override float BaseScale => 2f;
        public override float BaseTextScale => 1f;

        public override float AddYMult => 0.4f;

        public override float ImageScale
        {
            get
            {
                return image.Scale.X;
            }
            set
            {
                image.Scale = Vector2.One * value;
            }
        }
        public override float ImageRotation
        {
            get
            {
                return image.Rotation;
            }
            set
            {
                image.Rotation = value;
            }
        }
        public override float ImageY
        {
            get
            {
                return image.Y;
            }
            set
            {
                image.Y = value;
            }
        }

        public ReplayButton(Vector2 position, Vector2 tweenFrom, string text, string playersText) : base(position, tweenFrom, text, playersText)
        {
            image = new Image(TFGame.Atlas["replays/play"]);
            image.CenterOrigin();
            Add(image);
        }


        protected override void MenuAction()
        {
            base.MainMenu.State = (MainMenu.MenuState)18;
        }

        public override void Render()
        {
            image.DrawOutline();
            base.Render();
        }
    }
}
