
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class Text : Entity
    {
        private OutlineText _outlineText;

        public Text(string text)
        {
            _outlineText = new OutlineText(TFGame.Font, text);
            _outlineText.Scale = new Vector2(1.0f);
            _outlineText.Position = TFGame.Instance.Screen.Center / 2;

            Add(_outlineText);
        }
        public override void Render()
        {
            base.Render();
        }
    }
}
