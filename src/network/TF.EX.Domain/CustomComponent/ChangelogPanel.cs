using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Utils;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class ChangelogPanel : TowerFall.MenuItem
    {
        private const float PanelX = 20f;
        private const float PanelY = 32f;
        private const float PanelWidth = 280f;
        private const float PanelHeight = 180f;
        private const float HorizontalPadding = 16f;
        private const float VerticalPadding = 15f;

        private const float ContentLeft = PanelX + HorizontalPadding;
        private const float ContentRight = PanelX + PanelWidth - HorizontalPadding;
        private const float ContentTop = PanelY + VerticalPadding;
        private const float ContentHeight = PanelHeight - VerticalPadding * 2f;

        private const float LineHeight = 10f;
        private const float SectionGap = 5f;
        private const float ReleaseGap = 12f;
        private const float BulletX = ContentLeft + 4f;
        private const float EntryX = ContentLeft + 12f;
        private const float ArrowGap = 7f;

        private static readonly Color TitleColor = Calc.HexToColor("FFDC6B");
        private static readonly Color SectionColor = Calc.HexToColor("8FD3FF");
        private static readonly Color MutedColor = Calc.HexToColor("9A9AB0");

        private readonly record struct Segment(string Text, Color Color, float X, float JustifyX = 0f);

        private sealed record Line(float Gap, Segment[] Segments, bool IsUnderlined = false);

        private readonly List<Line> lines = [];
        private readonly List<int> releaseStarts = [];
        private readonly string title;
        private readonly Action onClose;
        private readonly int maxScroll;
        private readonly SineWave arrowWave;
        private int scroll;

        public ChangelogPanel(IReadOnlyList<ChangelogRelease> releases, string title, Action onClose) : base(new Vector2(160f, PanelY))
        {
            this.title = title;
            this.onClose = onClose;

            foreach (var release in releases)
            {
                releaseStarts.Add(lines.Count);

                lines.Add(new Line(
                    lines.Count > 0 ? ReleaseGap : 0f,
                    [
                        new Segment($"V{release.Version}", TitleColor, ContentLeft),
                        new Segment(release.Date, MutedColor, ContentRight, 1f),
                    ],
                    IsUnderlined: true));

                foreach (var section in release.Sections)
                {
                    lines.Add(new Line(SectionGap, [new Segment(section.Title, SectionColor, ContentLeft)]));

                    foreach (var entry in section.Entries)
                    {
                        AddEntry(entry);
                    }
                }
            }

            maxScroll = ComputeMaxScroll();

            arrowWave = new SineWave(80);
            Add(arrowWave);
        }

        private void AddEntry(ChangelogEntry entry)
        {
            var tag = entry.Tag.Length > 0 ? $"{entry.Tag}: " : "";
            var tagWidth = TFGame.Font.MeasureString(tag).X;
            var width = ContentRight - EntryX;
            var wrapped = Wrap(entry.Text, width - tagWidth, width);

            for (int i = 0; i < wrapped.Count; i++)
            {
                if (i > 0)
                {
                    lines.Add(new Line(0f, [new Segment(wrapped[i], Color.White, EntryX)]));
                    continue;
                }

                lines.Add(new Line(0f,
                [
                    new Segment("-", MutedColor, BulletX),
                    new Segment(tag, MutedColor, EntryX),
                    new Segment(wrapped[i], Color.White, EntryX + tagWidth),
                ]));
            }
        }

        private static List<string> Wrap(string text, float firstWidth, float width)
        {
            var wrapped = new List<string>();
            var current = "";

            foreach (var word in text.Split(' '))
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";
                var limit = wrapped.Count == 0 ? firstWidth : width;

                if (current.Length > 0 && TFGame.Font.MeasureString(candidate).X > limit)
                {
                    wrapped.Add(current);
                    current = word;
                    continue;
                }

                current = candidate;
            }

            wrapped.Add(current);

            return wrapped;
        }

        private float HeightFrom(int start, int index) => (index == start ? 0f : lines[index].Gap) + LineHeight;

        private int ComputeMaxScroll()
        {
            for (int start = 0; start < lines.Count; start++)
            {
                var height = 0f;

                for (int i = start; i < lines.Count; i++)
                {
                    height += HeightFrom(start, i);
                }

                if (height <= ContentHeight)
                {
                    return start;
                }
            }

            return 0;
        }

        public override void Update()
        {
            base.Update();

            if (!Selected)
            {
                return;
            }

            if (MenuInput.Up && scroll > 0)
            {
                ScrollTo(scroll - 1);
            }
            else if (MenuInput.Down && scroll < maxScroll)
            {
                ScrollTo(scroll + 1);
            }
            else if (MenuInput.Left && scroll > 0)
            {
                ScrollTo(releaseStarts.LastOrDefault(start => start < scroll));
            }
            else if (MenuInput.Right && scroll < maxScroll)
            {
                ScrollTo(Math.Min(maxScroll, releaseStarts.FirstOrDefault(start => start > scroll, maxScroll)));
            }
        }

        private void ScrollTo(int line)
        {
            scroll = line;
            Sounds.ui_move1.Play();
        }

        protected override void OnConfirm()
        {
            Sounds.ui_click.Play();
            onClose?.Invoke();
        }

        public override void Render()
        {
            base.Render();

            Draw.OutlineTextCentered(TFGame.Font, title, new Vector2(160f, 12f), TitleColor, Color.Black);

            MenuPanel.DrawPanel(PanelX, PanelY, PanelWidth, PanelHeight);

            var y = 0f;

            for (int i = scroll; i < lines.Count; i++)
            {
                var height = HeightFrom(scroll, i);

                if (y + height > ContentHeight)
                {
                    break;
                }

                y += height - LineHeight;
                RenderLine(lines[i], ContentTop + y);
                y += LineHeight;
            }

            var canJump = releaseStarts.Count > 1;

            if (scroll > 0)
            {
                DrawArrow(new Vector2(160f, PanelY - ArrowGap), -MathHelper.PiOver2);

                if (canJump)
                {
                    DrawArrow(new Vector2(PanelX - ArrowGap, PanelY + PanelHeight / 2f), MathHelper.Pi);
                }
            }

            if (scroll < maxScroll)
            {
                DrawArrow(new Vector2(160f, PanelY + PanelHeight + ArrowGap), MathHelper.PiOver2);

                if (canJump)
                {
                    DrawArrow(new Vector2(PanelX + PanelWidth + ArrowGap, PanelY + PanelHeight / 2f), 0f);
                }
            }
        }

        private static void RenderLine(Line line, float y)
        {
            foreach (var segment in line.Segments)
            {
                if (segment.Text.Length > 0)
                {
                    Draw.OutlineTextJustify(TFGame.Font, segment.Text, new Vector2(segment.X, y), segment.Color, Color.Black, new Vector2(segment.JustifyX, 0f));
                }
            }

            if (line.IsUnderlined)
            {
                Draw.Rect(ContentLeft, y + 9f, ContentRight - ContentLeft, 1f, TitleColor * 0.35f);
            }
        }

        private void DrawArrow(Vector2 center, float angle)
        {
            var bob = Calc.AngleToVector(angle, arrowWave.Value * 2f);

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
