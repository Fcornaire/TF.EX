using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain.CustomComponent
{
    public class ConfirmDialog : Entity
    {
        private const float ScreenCenterX = 160f;
        private const float ScreenCenterY = 120f;
        private const float PanelWidth = 140f;
        private const float PanelHeight = 70f;
        private const float TitleOffsetY = 8f;
        private const float DetailOffsetY = -17f;
        private const float FirstOptionOffsetY = 3f;
        private const float OptionSpacing = 14f;
        private const string CancelLabel = "CANCEL";

        private static readonly Color SelectionA = Calc.HexToColor("F87858");
        private static readonly Color SelectionB = Calc.HexToColor("F7BB59");
        private static readonly Color NoSelection = Calc.HexToColor("F8F8F8");

        private readonly string _title;
        private readonly string _detail;
        private readonly string[] _options;
        private readonly PlayerInput[] _originalInputs;
        private readonly PlayerInput[] _blockedInputs;
        private readonly Wiggler _selectionWiggler;

        private int _optionIndex;
        private bool _isFlashOn;

        public ConfirmDialog(string title, string detail, string confirmLabel)
        {
            _title = title;
            _detail = detail;
            _options = [CancelLabel, confirmLabel];
            Depth = -10000;

            Add(_selectionWiggler = Wiggler.Create(20, 4f));

            _originalInputs = MenuInput.MenuInputs;
            _blockedInputs = new PlayerInput[_originalInputs.Length];
            MenuInput.MenuInputs = _blockedInputs;
        }

        private bool IsConfirmSelected => _optionIndex == 1;

        public bool IsConfirmed => IsConfirmSelected && IsPressed(input => input.MenuConfirm);

        public bool IsCancelled => IsPressed(input => input.MenuBack) || (!IsConfirmSelected && IsPressed(input => input.MenuConfirm));

        public void Close()
        {
            if (MenuInput.MenuInputs == _blockedInputs)
            {
                MenuInput.MenuInputs = _originalInputs;
            }

            if (Scene != null)
            {
                RemoveSelf();
            }
        }

        public override void Update()
        {
            base.Update();

            if (Scene.OnInterval(4))
            {
                _isFlashOn = !_isFlashOn;
            }

            var step = IsPressed(input => input.MenuDown) ? 1 : IsPressed(input => input.MenuUp) ? -1 : 0;
            var next = Math.Clamp(_optionIndex + step, 0, _options.Length - 1);

            if (next == _optionIndex)
            {
                return;
            }

            Sounds.ui_move1.Play();
            _optionIndex = next;
            _selectionWiggler.Start();
        }

        public override void Render()
        {
            Draw.Rect(0f, 0f, 320f, 240f, Color.Black * 0.5f);
            MenuPanel.DrawPanel(ScreenCenterX - PanelWidth / 2f, ScreenCenterY - PanelHeight / 2f, PanelWidth, PanelHeight);

            base.Render();

            var titleAt = new Vector2(ScreenCenterX, ScreenCenterY - PanelHeight / 2f - TitleOffsetY);

            Draw.TextureCentered(TFGame.MenuAtlas["questResults/arrow"], titleAt + new Vector2(0f, -2f), Color.White);
            Draw.OutlineTextCentered(TFGame.Font, _title, titleAt, Color.White, 2f);

            Draw.TextCentered(TFGame.Font, _detail, new Vector2(ScreenCenterX, ScreenCenterY + DetailOffsetY), ReplayInfos.SelectedColor);

            for (int i = 0; i < _options.Length; i++)
            {
                var isSelected = i == _optionIndex;
                var at = new Vector2(ScreenCenterX + (isSelected ? _selectionWiggler.Value * 3f : 0f), ScreenCenterY + FirstOptionOffsetY + i * OptionSpacing);
                var color = !isSelected ? NoSelection : _isFlashOn ? SelectionB : SelectionA;

                Draw.TextCentered(TFGame.Font, isSelected ? "> " + _options[i] : _options[i], at, color);
            }
        }

        private bool IsPressed(Func<PlayerInput, bool> check) => _originalInputs.Any(input => input != null && check(input));
    }
}
