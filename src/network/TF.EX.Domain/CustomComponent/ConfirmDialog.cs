using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
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
        private const int LevelPauseLayer = 4;

        private static readonly Color SelectionA = Calc.HexToColor("F87858");
        private static readonly Color SelectionB = Calc.HexToColor("F7BB59");
        private static readonly Color NoSelection = Calc.HexToColor("F8F8F8");
        private static readonly Color DetailColor = Calc.HexToColor("FFDA9B");

        private readonly string _title;
        private readonly string _detail;
        private readonly string[] _options;
        private readonly float _centerX;
        private readonly PlayerInput[] _originalInputs;
        private readonly PlayerInput[] _blockedInputs;
        private readonly Wiggler _selectionWiggler;

        private int _optionIndex;
        private bool _isFlashOn;

        public ConfirmDialog(string title, string detail, string confirmLabel) : this(title, detail, confirmLabel, 0, ScreenCenterX)
        {
            _originalInputs = MenuInput.MenuInputs;
            _blockedInputs = new PlayerInput[_originalInputs.Length];
            MenuInput.MenuInputs = _blockedInputs;
        }

        private ConfirmDialog(string title, string detail, string confirmLabel, int layer, float centerX) : base(layer)
        {
            _title = title;
            _detail = detail;
            _options = [CancelLabel, confirmLabel];
            _centerX = centerX;
            Depth = -10000;

            Add(_selectionWiggler = Wiggler.Create(20, 4f));
        }

        public static ConfirmDialog OpenOverLevelLayer(string title, string detail, string confirmLabel)
        {
            var centerX = ScreenCenterX + (ServiceCollections.ResolveWiderSetModApi()?.UIXOffset ?? 0f);

            return new ConfirmDialog(title, detail, confirmLabel, LevelPauseLayer, centerX);
        }

        private bool IsConfirmSelected => _optionIndex == 1;

        public bool IsConfirmed => IsConfirmSelected && IsPressed(RawMenuButton.Confirm);

        public bool IsCancelled => IsPressed(RawMenuButton.Back) || (!IsConfirmSelected && IsPressed(RawMenuButton.Confirm));

        public void Close()
        {
            RestoreInputs();

            if (Scene != null)
            {
                RemoveSelf();
            }
        }

        public override void SceneEnd()
        {
            base.SceneEnd();
            RestoreInputs();
        }

        private void RestoreInputs()
        {
            if (MenuInput.MenuInputs == _blockedInputs)
            {
                MenuInput.MenuInputs = _originalInputs;
            }
        }

        public override void Update()
        {
            base.Update();

            if (Scene.OnInterval(4))
            {
                _isFlashOn = !_isFlashOn;
            }

            var step = IsPressed(RawMenuButton.Down) ? 1 : IsPressed(RawMenuButton.Up) ? -1 : 0;
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
            Draw.Rect(0f, 0f, _centerX * 2f, 240f, Color.Black * 0.5f);
            MenuPanel.DrawPanel(_centerX - PanelWidth / 2f, ScreenCenterY - PanelHeight / 2f, PanelWidth, PanelHeight);

            base.Render();

            var titleAt = new Vector2(_centerX, ScreenCenterY - PanelHeight / 2f - TitleOffsetY);

            Draw.TextureCentered(TFGame.MenuAtlas["questResults/arrow"], titleAt + new Vector2(0f, -2f), Color.White);
            Draw.OutlineTextCentered(TFGame.Font, _title, titleAt, Color.White, 2f);

            Draw.TextCentered(TFGame.Font, _detail, new Vector2(_centerX, ScreenCenterY + DetailOffsetY), DetailColor);

            for (int i = 0; i < _options.Length; i++)
            {
                var isSelected = i == _optionIndex;
                var at = new Vector2(_centerX + (isSelected ? _selectionWiggler.Value * 3f : 0f), ScreenCenterY + FirstOptionOffsetY + i * OptionSpacing);
                var color = !isSelected ? NoSelection : _isFlashOn ? SelectionB : SelectionA;

                Draw.TextCentered(TFGame.Font, isSelected ? "> " + _options[i] : _options[i], at, color);
            }
        }

        private bool IsPressed(RawMenuButton button)
        {
            return _originalInputs == null
                ? RawMenuInput.Pressed(button)
                : _originalInputs.Any(input => input != null && IsPressed(input, button));
        }

        private static bool IsPressed(PlayerInput input, RawMenuButton button)
        {
            return button switch
            {
                RawMenuButton.Confirm => input.MenuConfirm,
                RawMenuButton.Back => input.MenuBack,
                RawMenuButton.Up => input.MenuUp,
                RawMenuButton.Down => input.MenuDown,
                _ => false,
            };
        }
    }
}
