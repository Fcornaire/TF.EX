using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class VersusSelectBanner : TowerFall.MenuItem
    {
        private static readonly Color TitleColor = Calc.HexToColor("FFDC6B");

        private readonly List<Entry> entries = new();

        private bool tweeningOut;

        public VersusSelectBanner(Vector2 position) : base(position)
        {
            LayerIndex = -1;
        }

        public void Track(Func<bool> isVisible, string title, params string[] lines)
        {
            entries.Add(new Entry(isVisible, title, lines));
        }

        public override void Update()
        {
            base.Update();

            foreach (var entry in entries)
            {
                var target = !tweeningOut && entry.IsVisible() ? 1f : 0f;
                entry.Alpha = Calc.Approach(entry.Alpha, target, 0.08f * Engine.TimeMult);
            }
        }

        public override void Render()
        {
            foreach (var entry in entries)
            {
                if (entry.Alpha <= 0f)
                {
                    continue;
                }

                Draw.TextureBannerV(
                    TFGame.MenuAtlas["questResults/tipBanner"],
                    Position,
                    new Vector2(100f, 37f),
                    Vector2.One,
                    0f,
                    Color.White * entry.Alpha,
                    SpriteEffects.None,
                    Scene.FrameCounter * 0.03f,
                    4f,
                    5,
                    MathHelper.Pi / 8f);

                var y = Position.Y - 25f;
                Draw.OutlineTextCentered(TFGame.Font, entry.Title, new Vector2(Position.X, y), TitleColor * entry.Alpha, Color.Black * entry.Alpha);

                foreach (var line in entry.Lines)
                {
                    y += 8f;
                    Draw.OutlineTextCentered(TFGame.Font, line, new Vector2(Position.X, y), Color.White * entry.Alpha, Color.Black * entry.Alpha);
                }
            }
        }

        public override void TweenIn()
        {
            tweeningOut = false;
        }

        public override void TweenOut()
        {
            tweeningOut = true;
        }

        protected override void OnSelect()
        {
        }

        protected override void OnDeselect()
        {
        }

        protected override void OnConfirm()
        {
        }

        private sealed class Entry
        {
            public Entry(Func<bool> isVisible, string title, string[] lines)
            {
                IsVisible = isVisible;
                Title = title;
                Lines = lines;
            }

            public Func<bool> IsVisible { get; }
            public string Title { get; }
            public string[] Lines { get; }
            public float Alpha { get; set; }
        }
    }
}
