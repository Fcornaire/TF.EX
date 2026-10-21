using Microsoft.Xna.Framework;
using Monocle;
using TF.Replay.Domain.Utils;
using TowerFall;

namespace TF.Replay.Domain.CustomComponent
{
    public class ReplayBrowserHud : Entity
    {
        private const float GuideRight = 310f;
        private const float InnerGuideRight = 232f;
        private const float FavoriteY = 154f;
        private const float DeleteY = 208f;
        private const float FavoritesFolderY = 226f;
        private const float SizesLeft = 10f;
        private const float SizesY = 35f;
        private const float IconGap = 2f;
        private const float TitleGap = 4f;
        private const float KeyCapPadding = 2f;
        private const float KeyCapHeight = 10f;

        public long FolderSize { get; set; }
        public long TotalSize { get; set; }
        public bool HasReplays { get; set; }
        public bool IsInFavorites { get; set; }

        public override void Render()
        {
            base.Render();

            var sizes = $"FOLDER {SizeFormat.Of(FolderSize)}  TOTAL {SizeFormat.Of(TotalSize)}";
            var sizesWidth = TFGame.Font.MeasureString(sizes).X;

            Draw.OutlineTextCentered(TFGame.Font, sizes, new Vector2(SizesLeft + sizesWidth / 2f, SizesY), Color.LightGray, Color.Black);

            var input = FirstInput();

            if (input == null)
            {
                return;
            }

            if (HasReplays)
            {
                DrawGuide(GuideRight, FavoriteY, IsInFavorites ? "UNFAVORITE" : "FAVORITE", input.ArrowsIcon);
                DrawKeyCapGuide(InnerGuideRight, DeleteY, ReplayBrowserKeys.DeleteKeyLabel(input), "DELETE");
            }

            if (!IsInFavorites)
            {
                DrawGuide(InnerGuideRight, FavoritesFolderY, "GO TO FAVORITES", input.Alt2Icon, input.AltIcon);
            }
        }

        private static void DrawGuide(float right, float y, string title, params Subtexture[] icons)
        {
            var x = right;

            for (int i = icons.Length - 1; i >= 0; i--)
            {
                if (icons[i] == null)
                {
                    continue;
                }

                Draw.TextureCentered(icons[i], new Vector2(x - icons[i].Width / 2f, y), Color.White);
                x -= icons[i].Width + IconGap;
            }

            DrawTitle(x + IconGap - TitleGap, y, title);
        }

        private static void DrawKeyCapGuide(float right, float y, string key, string title)
        {
            var capWidth = TFGame.Font.MeasureString(key).X + KeyCapPadding * 2f;
            var capLeft = right - capWidth;
            var capTop = y - KeyCapHeight / 2f;

            Draw.Rect(capLeft, capTop, capWidth, KeyCapHeight, Color.Black * 0.6f);
            Draw.HollowRect(capLeft, capTop, capWidth, KeyCapHeight, Color.White * 0.6f);
            Draw.TextCentered(TFGame.Font, key, new Vector2(capLeft + capWidth / 2f, y), Color.White);

            DrawTitle(capLeft - TitleGap, y, title);
        }

        private static void DrawTitle(float right, float y, string title)
        {
            var width = TFGame.Font.MeasureString(title).X;

            Draw.TextCentered(TFGame.Font, title, new Vector2(right - width / 2f, y), Color.White);
        }

        private static PlayerInput FirstInput()
        {
            for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
            {
                if (TFGame.PlayerInputs[i] != null)
                {
                    return TFGame.PlayerInputs[i];
                }
            }

            return null;
        }
    }
}
