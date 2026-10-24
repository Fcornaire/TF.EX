using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Utils;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class TutorialPanel : TowerFall.MenuItem
    {
        private const string Title = "WELCOME TO EX";
        private const float PanelCenterY = 120f;
        private const float TitleY = 30f;
        private const float PanelWidth = 200f;
        private const float PanelHeight = 150f;
        private const float ArrowGap = 7f;

        private static readonly Color TitleColor = Calc.HexToColor("FFDC6B");

        private readonly IReadOnlyList<TutorialSlide> _slides;
        private readonly Action _onClose;
        private readonly SineWave _arrowWave;
        private readonly Wiggler _wiggler;
        private Slideshow _slideshow;
        private int _index;
        private bool _canAct;

        private bool IsLastSlide => _index == _slides.Count - 1;

        public TutorialPanel(IReadOnlyList<TutorialSlide> slides, Action onClose) : base(new Vector2(160f, PanelCenterY))
        {
            _slides = slides;
            _onClose = onClose;

            _arrowWave = new SineWave(80);
            Add(_arrowWave);

            _wiggler = Wiggler.Create(30, 4f);
            Add(_wiggler);

            ShowSlide(0);
        }

        private void ShowSlide(int slideIndex)
        {
            if (_slideshow != null)
            {
                Remove(_slideshow);
            }

            _index = slideIndex;

            var slide = _slides[_index];
            var data = new SlideData(slide.TextA, slide.TextB, slide.Dir)
            {
                Screens = [slide.Image()]
            };

            _slideshow = new Slideshow(data)
            {
                Active = false,
                Position = new Vector2(0f, -18f)
            };
            Add(_slideshow);

            MainMenu?.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, IsLastSlide ? "DONE" : "NEXT");

            _canAct = true;
        }

        public override void Update()
        {
            base.Update();

            _slideshow.Update();

            if (!Selected || !_canAct)
            {
                return;
            }

            if (MenuInput.Left && _index > 0)
            {
                Sounds.ui_move1.Play();
                SwitchTo(_index - 1);
            }
            else if (MenuInput.Right && !IsLastSlide)
            {
                Sounds.ui_move1.Play();
                SwitchTo(_index + 1);
            }
        }

        protected override void OnConfirm()
        {
            if (!_canAct)
            {
                return;
            }

            Sounds.ui_click.Play();

            if (IsLastSlide)
            {
                _canAct = false;
                _onClose?.Invoke();
                return;
            }

            SwitchTo(_index + 1);
        }

        private void SwitchTo(int slideIndex)
        {
            _canAct = false;
            _wiggler.Start();
            _slideshow.FadeOut(() => ShowSlide(slideIndex));
        }

        public override void Render()
        {
            Draw.OutlineTextCentered(TFGame.Font, Title, new Vector2(160f, TitleY), TitleColor, Color.Black);

            MenuPanel.DrawPanel(X - PanelWidth / 2f, Y - PanelHeight / 2f, PanelWidth, PanelHeight);

            base.Render();

            if (MenuButtons.Confirm != null)
            {
                Draw.TextureCentered(MenuButtons.Confirm, Position + new Vector2(0f, 57f), Color.White, 1f - _wiggler.Value * 0.25f, 0f);
            }

            Draw.TextJustify(TFGame.Font, $"{_index + 1} / {_slides.Count}", Position + new Vector2(88f, 63f), Color.Gray, new Vector2(1f, 1f));

            if (_index > 0)
            {
                DrawArrow(new Vector2(X - PanelWidth / 2f - ArrowGap, Y), MathHelper.Pi);
            }

            if (!IsLastSlide)
            {
                DrawArrow(new Vector2(X + PanelWidth / 2f + ArrowGap, Y), 0f);
            }
        }

        private void DrawArrow(Vector2 center, float angle)
        {
            var bob = Calc.AngleToVector(angle, _arrowWave.Value * 2f);

            Draw.Texture(TFGame.MenuAtlas["portraits/arrow"], center + bob, TitleColor, new Vector2(5f, 5f), 1f, angle);
        }

        public override void TweenIn()
        {
        }

        public override void TweenOut()
        {
            Visible = false;
        }

        protected override void OnSelect()
        {
        }

        protected override void OnDeselect()
        {
        }
    }
}
