using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class JoinCodeEntry : TowerFall.MenuItem
    {
        private const int CODE_LEN = 5;
        private static readonly Color TitleColor = Calc.HexToColor("FFDC6B");

        private string code = "";
        private readonly Action<string> onSubmit;
        private readonly Func<string> pasteProvider;
        private KeyboardState previousKeyboard;

        public JoinCodeEntry(Vector2 position, Action<string> onSubmit, Func<string> pasteProvider) : base(position)
        {
            this.onSubmit = onSubmit;
            this.pasteProvider = pasteProvider;
        }

        public override void Update()
        {
            base.Update();

            var keyboard = Keyboard.GetState();

            foreach (var key in keyboard.GetPressedKeys())
            {
                if (!previousKeyboard.IsKeyDown(key))
                {
                    HandleKey(key, keyboard);
                }
            }

            previousKeyboard = keyboard;

            if (MenuInput.Alt)
            {
                Paste();
            }
        }

        private void HandleKey(Keys key, KeyboardState keyboard)
        {
            if (key == Keys.Back)
            {
                if (code.Length > 0)
                {
                    code = code.Substring(0, code.Length - 1);
                    Sounds.ui_move2.Play();
                }

                return;
            }

            if (key == Keys.V && (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl)))
            {
                Paste();
                return;
            }

            var character = ToCodeChar(key);

            if (character.HasValue)
            {
                Append(character.Value);
            }
        }

        private static char? ToCodeChar(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                return (char)('A' + (key - Keys.A));
            }

            if (key >= Keys.D2 && key <= Keys.D9)
            {
                return (char)('2' + (key - Keys.D2));
            }

            if (key >= Keys.NumPad2 && key <= Keys.NumPad9)
            {
                return (char)('2' + (key - Keys.NumPad2));
            }

            return null;
        }

        private void Append(char character)
        {
            if (!IsCodeChar(character) || code.Length >= CODE_LEN)
            {
                Sounds.ui_invalid.Play();
                return;
            }

            code += character;
            Sounds.ui_move2.Play();
        }

        private void Paste()
        {
            var pasted = pasteProvider?.Invoke() ?? string.Empty;
            var sanitized = new string(pasted.Trim().ToUpperInvariant().Where(IsCodeChar).ToArray());

            if (sanitized.Length == 0)
            {
                Sounds.ui_invalid.Play();
                return;
            }

            code = sanitized.Length > CODE_LEN ? sanitized.Substring(0, CODE_LEN) : sanitized;
            Sounds.ui_click.Play();
        }

        private static bool IsCodeChar(char character)
        {
            return (character >= 'A' && character <= 'Z' && character != 'I' && character != 'O') || (character >= '2' && character <= '9');
        }

        public void Submit()
        {
            if (code.Length != CODE_LEN)
            {
                Sounds.ui_invalid.Play();
                return;
            }

            onSubmit?.Invoke(code);
        }

        protected override void OnConfirm() => Submit();

        public override void Render()
        {
            base.Render();

            Draw.OutlineTextCentered(TFGame.Font, "ENTER CODE", new Vector2(Position.X, Position.Y - 30f), TitleColor, 1f);

            const float spacing = 16f;
            var startX = Position.X - spacing * (CODE_LEN - 1) / 2f;

            for (int i = 0; i < CODE_LEN; i++)
            {
                var label = i < code.Length ? "*" : "_";
                var color = i < code.Length ? Color.White : Color.Gray;

                Draw.OutlineTextCentered(TFGame.Font, label, new Vector2(startX + spacing * i, Position.Y), color, 2f);
            }

            Draw.OutlineTextCentered(TFGame.Font, "TYPE OR PASTE THE CODE", new Vector2(Position.X, Position.Y + 30f), Color.Gray, 1f);
        }

        public override void TweenIn()
        {
        }

        public override void TweenOut()
        {
        }

        protected override void OnSelect()
        {
        }

        protected override void OnDeselect()
        {
        }
    }
}
