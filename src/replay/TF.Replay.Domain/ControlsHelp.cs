using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class ControlsHelp
    {
        private const float GuideY = 8f;

        private static float GuideX => 310f + Overlay.RightOffset;

        private static readonly (string Key, string Text, string Action)[] Rows =
        [
            ("Space", null, "PAUSE"),
            ("Left", null, "STEP BACK"),
            ("Right", null, "STEP FORWARD"),
            (null, "L-CLICK", "SEEK"),
            (null, "R-CLICK X2", "GIF IN / OUT"),
            ("G", null, "EXPORT GIF"),
            ("R", null, "RESTART REPLAY"),
            (null, "F1", "HURTBOXES"),
            (null, "ESC", "QUIT TO MENU"),
            ("H", null, "CLOSE"),
        ];

        private static readonly Dictionary<string, Subtexture> _icons = new Dictionary<string, Subtexture>();

        private static Subtexture Icon(string key)
        {
            if (key == null)
            {
                return null;
            }

            if (_icons.TryGetValue(key, out var cached))
            {
                return cached;
            }

            Subtexture icon = null;

            try
            {
                icon = TFGame.MenuAtlas[$"controls/keyboard/{key}"];
            }
            catch
            {
               
            }

            _icons[key] = icon;

            return icon;
        }

        public static void Reset() => _icons.Clear();

        public static void Render(bool expanded)
        {
            RenderGuide(expanded ? "CLOSE" : "CONTROLS");

            if (!expanded)
            {
                return;
            }

            const float rowHeight = 16f;
            const float top = 44f;
            const float width = 228f;

            var left = 46f + Overlay.CenterOffset;

            var height = Rows.Length * rowHeight + 12f;

            Draw.Rect(left - 1f, top - 8f, width + 2f, height + 2f, Color.White * 0.5f);
            Draw.Rect(left, top - 7f, width, height, Color.Black * 0.9f);

            for (int i = 0; i < Rows.Length; i++)
            {
                var y = top + i * rowHeight;
                var icon = Icon(Rows[i].Key);

                if (icon != null)
                {
                    Draw.TextureCentered(icon, new Vector2(left + 38f, y), Color.White);
                }
                else
                {
                    Draw.OutlineTextCentered(TFGame.Font, (Rows[i].Text ?? Rows[i].Key ?? "").ToUpperInvariant(),
                        new Vector2(left + 34f, y), Color.Yellow, Color.Black);
                }

                Draw.OutlineTextCentered(TFGame.Font, Rows[i].Action,
                    new Vector2(left + 145f, y), Color.White, Color.Black);
            }
        }

        private static void RenderGuide(string title)
        {
            var icon = Icon("H");
            var titleWidth = TFGame.Font.MeasureString(title).X;

            if (icon == null)
            {
                Draw.OutlineTextCentered(TFGame.Font, $"H : {title}",
                    new Vector2(GuideX - titleWidth / 2f, GuideY), Color.White, Color.Black);
                return;
            }

            var total = icon.Width + 4f + titleWidth;

            Draw.TextureCentered(icon, new Vector2(GuideX - icon.Width / 2f, GuideY), Color.White);
            Draw.OutlineTextCentered(TFGame.Font, title,new Vector2(GuideX - total + titleWidth / 2f, GuideY), Color.White, Color.Black);
        }
    }
}
