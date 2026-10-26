using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class CornerButton : TowerFall.MenuItem
    {
        private const float IconGap = 4f;

        private static readonly Color SelectedColor = Calc.HexToColor("FFDC6B");

        private readonly Vector2 tweenFrom;
        private readonly Vector2 tweenTo;
        private readonly string label;
        private readonly float labelX;
        private readonly float iconScale;
        private readonly Action onConfirm;
        private readonly Image icon;
        private readonly Wiggler wiggler;
        private readonly SineWave sine;
        private TowerFall.MenuItem[] below = [];

        public CornerButton(Vector2 position, Vector2 tweenFrom, string label, Subtexture iconTexture, float iconScale, float justifyX, Action onConfirm) : base(position)
        {
            this.tweenFrom = tweenFrom;
            tweenTo = position;
            this.label = label;
            this.iconScale = iconScale;
            this.onConfirm = onConfirm;

            icon = new Image(iconTexture);
            icon.CenterOrigin();

            var iconWidth = icon.Width * iconScale;
            var left = -justifyX * (iconWidth + IconGap + TFGame.Font.MeasureString(label).X);
            icon.X = left + iconWidth / 2f;
            labelX = left + iconWidth + IconGap;
            Add(icon);

            wiggler = Wiggler.Create(20, 4f);
            Add(wiggler);

            sine = new SineWave(120);
            Add(sine);
        }

        public void LinkItems(params TowerFall.MenuItem[] items)
        {
            below = items;

            foreach (var item in items)
            {
                item.UpItem = this;
            }

            DownItem = items.LastOrDefault();
        }

        public override void Update()
        {
            if (!Selected)
            {
                DownItem = below.FirstOrDefault(item => item.Selected) ?? DownItem;
            }

            base.Update();

            icon.Color = Selected ? SelectedColor : Color.White;
            icon.Scale = Vector2.One * iconScale * (1f + wiggler.Value * 0.2f);
            icon.Y = Selected ? (float)Math.Round(sine.Value * 1.5f) : 0f;
        }

        public override void Render()
        {
            icon.DrawOutline();
            base.Render();

            Draw.OutlineTextJustify(TFGame.Font, label, Position + new Vector2(labelX, 0f), Selected ? SelectedColor : Color.White, Color.Black, new Vector2(0f, 0.5f));
        }

        public override void TweenIn()
        {
            Position = tweenFrom;

            var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 20, start: true);
            tween.OnUpdate = t => Position = Vector2.Lerp(tweenFrom, tweenTo, t.Eased);
            Add(tween);
        }

        public override void TweenOut()
        {
            var start = Position;

            var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 12, start: true);
            tween.OnUpdate = t => Position = Vector2.Lerp(start, tweenFrom, t.Eased);
            Add(tween);
        }

        protected override void OnSelect()
        {
            wiggler.Start();
        }

        protected override void OnDeselect()
        {
        }

        protected override void OnConfirm()
        {
            Sounds.ui_click.Play();
            onConfirm?.Invoke();
        }
    }
}
