using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class OnlinePlayToggle : BorderButton
    {
        private static readonly Color OnColor = Calc.HexToColor("FFDC6B");

        public static bool IsOn { get; set; }

        public OnlinePlayToggle(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 168, 26)
        {
            UpdateSides();
        }

        public override void Update()
        {
            base.Update();

            if (!Selected)
            {
                return;
            }

            if ((MenuInput.Right && !IsOn) || (MenuInput.Left && IsOn))
            {
                Sounds.ui_move2.Play();
                IsOn = !IsOn;
                base.OnConfirm();
            }

            UpdateSides();
        }

        private void UpdateSides()
        {
            DrawLeft = IsOn;
            DrawRight = !IsOn;
        }

        public override void Render()
        {
            var label = IsOn ? "ONLINE PLAY: ON" : "ONLINE PLAY: OFF";
            Draw.OutlineTextCentered(TFGame.Font, label, Position, IsOn && Selected ? OnColor : base.DrawColor, 1f);

            base.Render();
        }
    }
}
