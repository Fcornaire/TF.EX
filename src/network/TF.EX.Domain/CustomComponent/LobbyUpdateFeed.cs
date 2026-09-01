using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyUpdateFeed : Entity
    {
        private class Entry
        {
            public string Text;
            public Subtexture Icon;
            public Color Tint;
            public DateTime? ShownAt;
        }

        private const double LIFETIME_SECONDS = 6.0;
        private const float SLIDE_SECONDS = 0.25f;
        private const float STAGGER_SECONDS = 0.06f;
        private const int MAX_ENTRIES = 10;
        private const float MARGIN = 2f;
        private const float TOP = 44f;
        private const float ROW_HEIGHT = 18f;
        private const float ICON_SIZE = 12f;
        private const float ICON_TEXT_GAP = 4f;

        private static readonly List<Entry> entries = [];
        private static LobbyUpdateFeed instance;

        private LobbyUpdateFeed() : base(-1)
        {
            Depth = -1000;
        }

        public static void Push(string text, Subtexture icon, Color tint)
        {
            if (TFGame.Instance.Scene is not MainMenu menu)
            {
                return;
            }

            entries.Add(new Entry
            {
                Text = text?.ToUpperInvariant(),
                Icon = icon,
                Tint = tint,
            });

            if (instance == null || instance.Scene != menu)
            {
                instance = new LobbyUpdateFeed();
                menu.Add(instance);
            }
        }

        public override void Removed()
        {
            base.Removed();

            if (instance == this)
            {
                instance = null;
            }
        }

        public override void SceneEnd()
        {
            base.SceneEnd();

            entries.Clear();

            if (instance == this)
            {
                instance = null;
            }
        }

        public override void Render()
        {
            base.Render();

            entries.RemoveAll(entry => entry.ShownAt != null && (DateTime.UtcNow - entry.ShownAt.Value).TotalSeconds > LIFETIME_SECONDS);

            if (Scene is not MainMenu menu || menu.State != MainMenu.MenuState.Rollcall)
            {
                return;
            }

            var y = TOP + menu.UILayer.Camera.Y;

            var index = 0;

            foreach (var entry in entries.Take(MAX_ENTRIES).ToArray())
            {
                entry.ShownAt ??= DateTime.UtcNow;

                var age = (float)(DateTime.UtcNow - entry.ShownAt.Value).TotalSeconds;
                var slideIn = Math.Clamp((age - index * STAGGER_SECONDS) / SLIDE_SECONDS, 0f, 1f);
                var slideOut = Math.Clamp((float)(LIFETIME_SECONDS - age) / SLIDE_SECONDS, 0f, 1f);

                DrawEntry(entry, y, Math.Min(slideIn, slideOut));

                y += ROW_HEIGHT;
                index++;
            }
        }

        private static void DrawEntry(Entry entry, float y, float slideProgress)
        {
            var edge = TFGame.MenuAtlas["variants/bubbleEdge"];
            var middle = TFGame.MenuAtlas["variants/bubbleMiddle"];

            var text = entry.Text ?? "";
            var textWidth = string.IsNullOrEmpty(text) ? 0f : TFGame.Font.MeasureString(text).X;
            var iconWidth = entry.Icon != null ? ICON_SIZE + (textWidth > 0 ? ICON_TEXT_GAP : 0f) : 0f;
            var contentWidth = iconWidth + textWidth;

            var middleTiles = Math.Max(1, (int)Math.Ceiling((contentWidth - 12f) / 10.0));
            var width = 20 + middleTiles * 10;

            var eased = 1f - (float)Math.Pow(1f - slideProgress, 3);
            var x = MARGIN + (eased - 1f) * (width + MARGIN);

            Draw.Texture(edge, new Vector2(x, y), entry.Tint, Vector2.Zero, Vector2.One);

            for (int index = 0; index < middleTiles; index++)
            {
                Draw.Texture(middle, new Vector2(x + 10 + index * 10, y), entry.Tint, Vector2.Zero, Vector2.One);
            }

            Draw.Texture(edge, new Vector2(x + 10 + middleTiles * 10, y), entry.Tint, Vector2.Zero, 1f, 0.0f, SpriteEffects.FlipHorizontally);

            var contentX = x + width / 2f - contentWidth / 2f;

            if (entry.Icon != null)
            {
                var scale = ICON_SIZE / Math.Max(entry.Icon.Width, entry.Icon.Height);

                Draw.Texture(
                    entry.Icon,
                    new Vector2(contentX + ICON_SIZE / 2f - entry.Icon.Width * scale / 2f, y + 7f - entry.Icon.Height * scale / 2f),
                    Color.White,
                    Vector2.Zero,
                    new Vector2(scale, scale));

                contentX += iconWidth;
            }

            if (textWidth > 0)
            {
                Draw.TextJustify(TFGame.Font, text, new Vector2(contentX, y + 7f), Color.Black, new Vector2(0f, 0.5f));
            }
        }
    }
}
