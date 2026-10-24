using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Models.WebSocket.Server;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LeaderboardPanel : TowerFall.MenuItem
    {
        private const float PanelX = 20f;
        private const float PanelY = 26f;
        private const float PanelWidth = 280f;
        private const float PanelHeight = 164f;
        private const float HorizontalPadding = 16f;
        private const float VerticalPadding = 15f;

        private const float ContentLeft = PanelX + HorizontalPadding;
        private const float ContentRight = PanelX + PanelWidth - HorizontalPadding;
        private const float ContentTop = PanelY + VerticalPadding;

        private const float LineHeight = 10f;
        private const float RowsTop = ContentTop + 14f;
        private const int RowsPerPage = 10;
        private const float PagerY = PanelY + PanelHeight - VerticalPadding - 10f;
        private const float FooterY = PanelY + PanelHeight + 10f;
        private const float ArrowGap = 7f;

        private const float RankX = ContentLeft + 20f;
        private const float NameX = ContentLeft + 30f;
        private const float RatingX = ContentRight - 70f;
        private const float MatchesX = ContentRight;

        private const double RequestTimeoutSeconds = 6;

        private static readonly Color TitleColor = Calc.HexToColor("FFDC6B");
        private static readonly Color MutedColor = Calc.HexToColor("9A9AB0");
        private static readonly Color SilverColor = Calc.HexToColor("C8C8DC");
        private static readonly Color BronzeColor = Calc.HexToColor("D49A6A");

        private enum LoadState
        {
            Loading,
            Loaded,
            Failed,
        }

        private readonly SineWave arrowWave;
        private readonly DateTime requestDeadline;
        private readonly Action onFailed;
        private readonly Action<int?> onOpenProfile;
        private readonly int? rankToSelect;
        private LoadState state = LoadState.Loading;
        private List<LeaderboardEntry> entries = [];
        private List<LeaderboardEntry> around = [];
        private LeaderboardEntry own;
        private int page;
        private int cursor;
        private bool areGuidesShown;

        public LeaderboardPanel(Action onFailed, Action<int?> onOpenProfile, int? rankToSelect) : base(new Vector2(160f, PanelY))
        {
            this.onFailed = onFailed;
            this.onOpenProfile = onOpenProfile;
            this.rankToSelect = rankToSelect;
            requestDeadline = DateTime.UtcNow.AddSeconds(RequestTimeoutSeconds);

            arrowWave = new SineWave(80);
            Add(arrowWave);
        }

        private int TopPageCount => (entries.Count + RowsPerPage - 1) / RowsPerPage;

        private int PageCount => Math.Max(1, TopPageCount + (around.Count > 0 ? 1 : 0));

        private bool IsAroundPage => around.Count > 0 && page == TopPageCount;

        public void OnLoaded(LeaderboardData data)
        {
            entries = data?.Entries ?? [];
            around = data?.Around ?? [];
            own = data?.Own;
            state = LoadState.Loaded;
            SetCursor(rankToSelect ?? own?.Rank);
        }

        public void OnFailed()
        {
            if (state != LoadState.Loading)
            {
                return;
            }

            state = LoadState.Failed;
            onFailed?.Invoke();
        }

        private void SetCursor(int? rank)
        {
            var aroundIndex = around.FindIndex(entry => entry.Rank == rank);

            if (rank >= 1 && rank <= entries.Count)
            {
                page = (rank.Value - 1) / RowsPerPage;
                cursor = (rank.Value - 1) % RowsPerPage;
            }
            else if (aroundIndex >= 0)
            {
                page = TopPageCount;
                cursor = aroundIndex;
            }
        }

        private List<LeaderboardEntry> GetPageRows()
        {
            return IsAroundPage ? around : [.. entries.Skip(page * RowsPerPage).Take(RowsPerPage)];
        }

        public override void Update()
        {
            base.Update();

            if (DateTime.UtcNow >= requestDeadline)
            {
                OnFailed();
            }

            ShowGuides();

            if (!Selected || state != LoadState.Loaded)
            {
                return;
            }

            if (MenuInput.Alt)
            {
                OpenProfile(null);
            }
            else if (MenuInput.Up)
            {
                MoveCursor(-1);
            }
            else if (MenuInput.Down)
            {
                MoveCursor(1);
            }
            else if (MenuInput.Left && page > 0)
            {
                ShowPage(page - 1, cursor);
            }
            else if (MenuInput.Right && page < PageCount - 1)
            {
                ShowPage(page + 1, cursor);
            }
        }

        private void ShowGuides()
        {
            if (areGuidesShown || state != LoadState.Loaded || MainMenu == null || MainMenu.State != CreatedState)
            {
                return;
            }

            if (entries.Count > 0)
            {
                MainMenu.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "VIEW PROFILE");
            }

            MainMenu.ButtonGuideC.SetDetails(MenuButtonGuide.ButtonModes.Alt, "MY PROFILE");
            areGuidesShown = true;
        }

        private void MoveCursor(int step)
        {
            var next = cursor + step;
            var rowCount = GetPageRows().Count;

            if (next >= 0 && next < rowCount)
            {
                cursor = next;
                Sounds.ui_move1.Play();
            }
            else if (next < 0 && page > 0)
            {
                ShowPage(page - 1, RowsPerPage - 1);
            }
            else if (next >= rowCount && page < PageCount - 1)
            {
                ShowPage(page + 1, 0);
            }
        }

        private void ShowPage(int next, int row)
        {
            page = next;
            cursor = Math.Max(0, Math.Min(row, GetPageRows().Count - 1));
            Sounds.ui_move1.Play();
        }

        private void OpenProfile(int? rank)
        {
            Sounds.ui_click.Play();
            onOpenProfile?.Invoke(rank);
        }

        public override void Render()
        {
            base.Render();

            Draw.OutlineTextCentered(TFGame.Font, "LEADERBOARD", new Vector2(160f, 12f), TitleColor, Color.Black);

            MenuPanel.DrawPanel(PanelX, PanelY, PanelWidth, PanelHeight);

            if (state == LoadState.Loading)
            {
                DrawMessage("LOADING...");
                return;
            }

            if (state == LoadState.Failed)
            {
                DrawMessage("COULD NOT REACH THE SERVER");
                return;
            }

            if (entries.Count == 0)
            {
                DrawMessage("NO RATED PLAYERS YET");
                return;
            }

            DrawHeader();
            DrawRows();
            DrawPager();
            DrawFooter();
        }

        private static void DrawMessage(string text)
        {
            Draw.OutlineTextCentered(TFGame.Font, text, new Vector2(160f, PanelY + PanelHeight / 2f), MutedColor, Color.Black);
        }

        private static void DrawHeader()
        {
            DrawText("RANK", RankX, ContentTop, MutedColor, 1f);
            DrawText("NAME", NameX, ContentTop, MutedColor, 0f);
            DrawText("RATING", RatingX, ContentTop, MutedColor, 1f);
            DrawText("MATCHES", MatchesX, ContentTop, MutedColor, 1f);

            Draw.Rect(ContentLeft, ContentTop + 9f, ContentRight - ContentLeft, 1f, TitleColor * 0.35f);
        }

        private void DrawRows()
        {
            var y = RowsTop;
            var rows = GetPageRows();

            for (var row = 0; row < rows.Count; row++)
            {
                var entry = rows[row];
                var isOwn = own != null && entry.Rank == own.Rank;
                var color = isOwn ? TitleColor : Color.White;

                if (isOwn)
                {
                    Draw.Rect(ContentLeft - 4f, y - 1f, ContentRight - ContentLeft + 8f, LineHeight, TitleColor * 0.15f);
                }

                if (row == cursor)
                {
                    Draw.HollowRect(ContentLeft - 4f, y - 1f, ContentRight - ContentLeft + 8f, LineHeight, color * 0.8f);
                }

                DrawText(entry.Rank.ToString(), RankX, y, isOwn ? TitleColor : RankColor(entry.Rank), 1f);
                DrawText(DisplayName(entry.Name), NameX, y, color, 0f);
                DrawText(entry.Rating.ToString(), RatingX, y, color, 1f);
                DrawText(entry.Matches.ToString(), MatchesX, y, color, 1f);

                y += LineHeight;
            }
        }

        private void DrawPager()
        {
            if (PageCount <= 1)
            {
                return;
            }

            var label = IsAroundPage ? "AROUND YOU" : $"{page + 1} / {TopPageCount}";
            Draw.OutlineTextCentered(TFGame.Font, label, new Vector2(160f, PagerY), MutedColor, Color.Black);

            if (page > 0)
            {
                DrawArrow(new Vector2(PanelX - ArrowGap, PanelY + PanelHeight / 2f), MathHelper.Pi);
            }

            if (page < PageCount - 1)
            {
                DrawArrow(new Vector2(PanelX + PanelWidth + ArrowGap, PanelY + PanelHeight / 2f), 0f);
            }
        }

        private void DrawFooter()
        {
            if (own == null)
            {
                Draw.OutlineTextCentered(TFGame.Font, "NOT RANKED YET", new Vector2(160f, FooterY), MutedColor, Color.Black);
                return;
            }

            Draw.OutlineTextCentered(TFGame.Font, $"CURRENT RANK {own.Rank}   RATING {own.Rating}", new Vector2(160f, FooterY), TitleColor, Color.Black);
        }

        private static Color RankColor(int rank)
        {
            return rank switch
            {
                1 => TitleColor,
                2 => SilverColor,
                3 => BronzeColor,
                _ => MutedColor,
            };
        }

        internal static string DisplayName(string name)
        {
            var upper = (name ?? "").ToUpperInvariant();

            return new string([.. upper.Select(c => c >= ' ' && c <= '~' ? c : '?')]);
        }

        private static void DrawText(string text, float x, float y, Color color, float justifyX)
        {
            Draw.OutlineTextJustify(TFGame.Font, text, new Vector2(x, y), color, Color.Black, new Vector2(justifyX, 0f));
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

        protected override void OnConfirm()
        {
            var rows = GetPageRows();

            if (state != LoadState.Loaded || cursor >= rows.Count)
            {
                return;
            }

            OpenProfile(rows[cursor].Rank);
        }

        protected override void OnSelect()
        {
        }

        protected override void OnDeselect()
        {
        }
    }
}
