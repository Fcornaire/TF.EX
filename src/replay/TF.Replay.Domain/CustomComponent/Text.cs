using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain.CustomComponent
{
    public class Text : Entity
    {
        private OutlineText _outlineText;

        public Text(string text)
        {
            _outlineText = new OutlineText(TFGame.Font, text?.ToUpperInvariant() ?? "");
            _outlineText.Scale = new Vector2(1.0f);
            _outlineText.Position = TFGame.Instance.Screen.Center / 2;

            Add(_outlineText);
        }
    }
}
