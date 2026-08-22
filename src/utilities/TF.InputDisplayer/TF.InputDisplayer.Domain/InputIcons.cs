using Monocle;
using TF.InputDisplayer.Domain.Models;
using TowerFall;

namespace TF.InputDisplayer.Domain
{
    public static class InputIcons
    {
        public const int Width = 12;
        public const int Height = 13;

        public const string UpLeft = "upLeft";
        public const string UpRight = "upRight";
        public const string DownLeft = "downLeft";
        public const string DownRight = "downRight";
        public const string Neutral = "neutral";

        public static readonly string[] Shipped = { UpLeft, UpRight, DownLeft, DownRight, Neutral };

        private static readonly Dictionary<string, Func<Subtexture>> _shipped = new Dictionary<string, Func<Subtexture>>();

        private static readonly Subtexture[] _directions = new Subtexture[9];
        private static readonly Subtexture[] _buttons = new Subtexture[InputPacker.ButtonCount];

        private static bool _resolved;

        public static void Register(string name, Func<Subtexture> resolve)
        {
            _shipped[name] = resolve;
            _resolved = false;
        }

        public static bool Ready()
        {
            if (_resolved)
            {
                return true;
            }

            if (TFGame.MenuAtlas == null)
            {
                return false;
            }

            _directions[0] = Shipped_(UpLeft);
            _directions[1] = TFGame.MenuAtlas["controls/keyboard/Up"];
            _directions[2] = Shipped_(UpRight);
            _directions[3] = TFGame.MenuAtlas["controls/keyboard/Left"];
            _directions[4] = Shipped_(Neutral);
            _directions[5] = TFGame.MenuAtlas["controls/keyboard/Right"];
            _directions[6] = Shipped_(DownLeft);
            _directions[7] = TFGame.MenuAtlas["controls/keyboard/Down"];
            _directions[8] = Shipped_(DownRight);

            _buttons[0] = TFGame.MenuAtlas["controls/xb360/a"];
            _buttons[1] = TFGame.MenuAtlas["controls/xb360/x"];
            _buttons[2] = TFGame.MenuAtlas["controls/xb360/b"];
            _buttons[3] = TFGame.MenuAtlas["controls/xb360/rt"];

            foreach (var direction in _directions)
            {
                if (direction == null)
                {
                    return false;
                }
            }

            foreach (var button in _buttons)
            {
                if (button == null)
                {
                    return false;
                }
            }

            _resolved = true;

            return true;
        }

        public static Subtexture Direction(int packed)
        {
            var index = (InputPacker.MoveY(packed) + 1) * 3 + InputPacker.MoveX(packed) + 1;

            return _directions[index];
        }

        public static Subtexture Button(int index) => _buttons[index];

        public static int ButtonWidth
        {
            get
            {
                var width = 0;

                foreach (var button in _buttons)
                {
                    if (button != null && button.Width > width)
                    {
                        width = button.Width;
                    }
                }

                return width == 0 ? 14 : width;
            }
        }

        private static Subtexture Shipped_(string name) => _shipped.TryGetValue(name, out var resolve) ? resolve() : null;
    }
}
