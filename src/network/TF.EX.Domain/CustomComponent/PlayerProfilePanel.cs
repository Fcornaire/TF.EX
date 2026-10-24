using Microsoft.Xna.Framework;
using Monocle;
using System.Globalization;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.WebSocket.Server;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class PlayerProfilePanel : TowerFall.MenuItem
    {
        private const float PanelX = 15f;
        private const float PanelY = 20f;
        private const float PanelWidth = 290f;
        private const float PanelHeight = 180f;
        private const float Padding = 14f;

        private const float ContentLeft = PanelX + Padding;
        private const float ContentRight = PanelX + PanelWidth - Padding;
        private const float ContentTop = PanelY + Padding;
        private const float ContentWidth = ContentRight - ContentLeft;

        private const float TitleY = 10f;
        private const float LineHeight = 10f;
        private const float RowHeight = 12f;
        private const float NameLineHeight = 9f;
        private const float BigTextScale = 2f;
        private const float PagerY = PanelY + PanelHeight - Padding - 6f;
        private const float ArrowGap = 7f;

        private const float TileLabelY = ContentTop + 21f;
        private const float OverviewRuleY = ContentTop + 34f;
        private const float SectionTop = ContentTop + 38f;
        private const float PortraitSize = 50f;
        private const float PortraitGap = 8f;
        private const float PortraitTop = SectionTop + 10f;
        private const float RightColumnLeft = ContentLeft + 174f;
        private const float TowerScale = 2f;
        private const float TowerBlockHalf = 30f;

        private const float AwardIconScale = 1.5f;
        private const float AwardIconY = ContentTop + 21f;
        private const float AwardNameY = ContentTop + 36f;
        private const float RecordsTop = ContentTop + 64f;

        private const float MonthHeaderY = ContentTop + 13f;
        private const float MonthRowsTop = ContentTop + 27f;
        private const float GamesX = ContentLeft + 121f;
        private const float WinsX = ContentLeft + 166f;
        private const float KillsX = ContentLeft + 211f;

        private const int OverviewPage = 0;
        private const int AwardsPage = 1;
        private const int MonthsPage = 2;
        private const int PageCount = 3;
        private const int ShownCount = 3;
        private const int ShownMonths = 9;
        private const float FramesPerSecond = 60f;
        private const string ConsolationPrizeName = "Consolation Prize";

        private const double RequestTimeoutSeconds = 6;

        private static readonly Color TitleColor = Calc.HexToColor("FFDC6B");
        private static readonly Color MutedColor = Calc.HexToColor("9A9AB0");
        private static readonly string[] MonthFilterLabels = ["ALL", "RANKED", "CASUAL"];
        private static readonly string[] MonthFilterEmptyTexts = ["NO GAMES YET", "NO RANKED GAMES YET", "NO CASUAL GAMES YET"];

        private enum LoadState
        {
            Loading,
            Loaded,
            Missing,
            Failed,
        }

        private enum MonthFilter
        {
            All,
            Ranked,
            Casual,
        }

        private readonly SineWave _arrowWave;
        private readonly DateTime _requestDeadline;
        private readonly Action _onFailed;
        private readonly List<(Sprite<int> Icon, string[] Name)> _awards = [];
        private List<Image> _towerGraphics = [];
        private TowerPlays _bestTower;
        private LoadState _state = LoadState.Loading;
        private PlayerProfile _profile;
        private int _page;
        private MonthFilter _monthFilter = MonthFilter.All;

        public PlayerProfilePanel(Action onFailed) : base(new Vector2(160f, PanelY))
        {
            this._onFailed = onFailed;
            _requestDeadline = DateTime.UtcNow.AddSeconds(RequestTimeoutSeconds);

            _arrowWave = new SineWave(80);
            Add(_arrowWave);
        }

        public void OnLoaded(PlayerProfile loaded)
        {
            if (_state != LoadState.Loading)
            {
                return;
            }

            _profile = loaded;
            _state = loaded == null ? LoadState.Missing : LoadState.Loaded;

            if (loaded != null)
            {
                AddAwardIcons();
                AddTowerGraphics();
            }
        }

        public void OnFailed()
        {
            if (_state != LoadState.Loading)
            {
                return;
            }

            _state = LoadState.Failed;
            _onFailed?.Invoke();
        }

        private void AddAwardIcons()
        {
            var (archerIndex, archerType) = GetMainArcher();
            var topAwards = (_profile.Awards ?? [])
                .Select(award => GetAwardAt(award.Award))
                .Where(award => award != null && award.Name != ConsolationPrizeName)
                .Take(ShownCount);

            foreach (var award in topAwards)
            {
                var icon = award.GetSprite(false);
                icon.Scale = Vector2.One * AwardIconScale;
                icon.Visible = false;
                Add(icon);

                _awards.Add((icon, award.GetDrawName(archerIndex, archerType).Select(LeaderboardPanel.DisplayName).ToArray()));
            }
        }

        private void AddTowerGraphics()
        {
            var towers = GameData.VersusTowers;

            _bestTower = (_profile.BestTowers ?? [])
                .FirstOrDefault(tower => towers != null && tower.MapId >= 0 && tower.MapId < towers.Count && towers[tower.MapId]?.Theme?.Icon != null);

            if (_bestTower == null)
            {
                return;
            }

            _towerGraphics = MapButton.InitVersusGraphics(_bestTower.MapId);

            foreach (var graphic in _towerGraphics)
            {
                graphic.Scale = Vector2.One * TowerScale;
                graphic.Visible = false;
                Add(graphic);
            }
        }

        public override void Update()
        {
            base.Update();

            if (DateTime.UtcNow >= _requestDeadline)
            {
                OnFailed();
            }

            if (!Selected || _state != LoadState.Loaded)
            {
                return;
            }

            if ((MenuInput.Left || MenuInput.Up) && _page > 0)
            {
                ShowPage(_page - 1);
            }
            else if ((MenuInput.Right || MenuInput.Down) && _page < PageCount - 1)
            {
                ShowPage(_page + 1);
            }
        }

        private void ShowPage(int next)
        {
            _page = next;
            Sounds.ui_move1.Play();

            if (_page == MonthsPage)
            {
                MainMenu?.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, "FILTER");
            }
            else
            {
                MainMenu?.ButtonGuideA.Clear();
            }
        }

        public override void Render()
        {
            base.Render();

            var title = _state == LoadState.Loaded ? LeaderboardPanel.DisplayName(_profile.Name) : "PROFILE";
            Draw.OutlineTextCentered(TFGame.Font, title, new Vector2(160f, TitleY), TitleColor, Color.Black);

            MenuPanel.DrawPanel(PanelX, PanelY, PanelWidth, PanelHeight);

            switch (_state)
            {
                case LoadState.Loading:
                    DrawMessage("LOADING...");
                    return;
                case LoadState.Failed:
                    DrawMessage("COULD NOT REACH THE SERVER");
                    return;
                case LoadState.Missing:
                    DrawMessage("PROFILE NOT AVAILABLE");
                    return;
            }

            switch (_page)
            {
                case OverviewPage:
                    DrawTiles();
                    DrawArchers();
                    DrawTower();
                    break;
                case AwardsPage:
                    DrawAwards();
                    DrawRecords();
                    break;
                default:
                    DrawMonths();
                    break;
            }

            DrawPager();
        }

        private void DrawTiles()
        {
            var tiles = new (string Value, string Label)[]
            {
                (_profile.Rank?.ToString() ?? "-", "RANK"),
                (_profile.Rating?.ToString() ?? "-", "RATING"),
                (_profile.Games.ToString(), "GAMES"),
                (_profile.Wins.ToString(), "WINS"),
                (KillDeathRatio(_profile.Kills, _profile.Deaths), "K/D"),
            };
            var tileWidth = ContentWidth / tiles.Length;

            for (var index = 0; index < tiles.Length; index++)
            {
                var x = ContentLeft + tileWidth * (index + 0.5f);

                Draw.OutlineTextJustify(TFGame.Font, tiles[index].Value, new Vector2(x, ContentTop), TitleColor, Color.Black, new Vector2(0.5f, 0f), BigTextScale);
                DrawText(tiles[index].Label, x, TileLabelY, MutedColor, 0.5f);
            }

            Draw.Rect(ContentLeft, OverviewRuleY, ContentWidth, 1f, TitleColor * 0.35f);
        }

        private void DrawArchers()
        {
            DrawText("MAIN ARCHERS", ContentLeft, SectionTop, MutedColor, 0f);

            var archers = (_profile.Archers ?? [])
                .Where(plays => !string.IsNullOrEmpty(plays.CustomArcherId) || ResolveArcher(plays) != null)
                .Take(ShownCount)
                .ToList();

            if (archers.Count == 0)
            {
                DrawText("-", ContentLeft, SectionTop + LineHeight + 1f, MutedColor, 0f);
                return;
            }

            for (var index = 0; index < archers.Count; index++)
            {
                var plays = archers[index];
                var portrait = ResolveArcher(plays)?.Portraits.Win;
                var center = new Vector2(ContentLeft + PortraitSize / 2f + index * (PortraitSize + PortraitGap), PortraitTop + PortraitSize / 2f);

                if (portrait == null)
                {
                    DrawMissingArcher(center, plays.CustomArcherId);
                    continue;
                }

                var scale = Math.Min(1f, PortraitSize / Math.Max(portrait.Width, portrait.Height));

                Draw.Texture(portrait, center, Color.White, new Vector2(portrait.Width, portrait.Height) / 2f, scale, 0f);
                DrawText($"{plays.Games} GAMES", center.X, PortraitTop + PortraitSize + 2f, Color.White, 0.5f);
            }
        }

        private static void DrawMissingArcher(Vector2 center, string customArcherId)
        {
            var corner = center - new Vector2(PortraitSize / 2f);
            var skinName = LeaderboardPanel.DisplayName(customArcherId.Substring(customArcherId.LastIndexOf('/') + 1));

            Draw.Rect(corner.X, corner.Y, PortraitSize, PortraitSize, Color.Black * 0.4f);
            Draw.HollowRect(corner.X, corner.Y, PortraitSize, PortraitSize, MutedColor);
            Draw.OutlineTextJustify(TFGame.Font, "?", center, MutedColor, Color.Black, new Vector2(0.5f, 0.5f), BigTextScale);
            DrawText(Fit(skinName, PortraitSize + PortraitGap - 2f), center.X, PortraitTop + PortraitSize + 2f, MutedColor, 0.5f);
        }

        private void DrawTower()
        {
            var centerX = (RightColumnLeft + ContentRight) / 2f;

            DrawText("BEST TOWER", centerX, SectionTop, MutedColor, 0.5f);

            if (_bestTower == null)
            {
                DrawText("-", centerX, SectionTop + LineHeight + 1f, MutedColor, 0.5f);
                return;
            }

            foreach (var graphic in _towerGraphics)
            {
                RenderGraphic(graphic, new Vector2(centerX, PortraitTop + TowerBlockHalf));
            }

            var name = LeaderboardPanel.DisplayName(GameData.VersusTowers[_bestTower.MapId].Theme.Name);
            DrawText(Fit(name, ContentRight - RightColumnLeft), centerX, PortraitTop + 2f * TowerBlockHalf + 4f, Color.White, 0.5f);
        }

        private void DrawAwards()
        {
            DrawText("TOP AWARDS", ContentLeft, ContentTop, MutedColor, 0f);

            if (_awards.Count == 0)
            {
                DrawText("-", ContentLeft, ContentTop + LineHeight + 1f, MutedColor, 0f);
                return;
            }

            var slotWidth = ContentWidth / ShownCount;

            for (var index = 0; index < _awards.Count; index++)
            {
                var (icon, name) = _awards[index];
                var x = ContentLeft + slotWidth * (index + 0.5f);

                RenderGraphic(icon, new Vector2(x, AwardIconY));

                for (var line = 0; line < name.Length; line++)
                {
                    DrawText(Fit(name[line], slotWidth - 4f), x, AwardNameY + line * NameLineHeight, Color.White, 0.5f);
                }
            }
        }

        private void DrawRecords()
        {
            var y = RecordsTop + RowHeight;
            var mostKills = GetBestStat("Kills.Kills");

            Draw.Rect(ContentLeft, RecordsTop - 6f, ContentWidth, 1f, TitleColor * 0.35f);
            DrawText("RECORDS", ContentLeft, RecordsTop, MutedColor, 0f);
            DrawRecord("LONGEST SHOT", ToSeconds(GetBestStat("LongestShot") / FramesPerSecond), y);
            DrawRecord("FASTEST KILL", ToSeconds(TimeSpan.FromTicks((long)GetBestStat("FastestKill")).TotalSeconds), y + RowHeight);
            DrawRecord("MOST KILLS IN A GAME", mostKills > 0 ? mostKills.ToString("0", CultureInfo.InvariantCulture) : "-", y + 2 * RowHeight);
            DrawRecord("TIME PLAYED", ToPlayTime(_profile.Seconds), y + 3 * RowHeight);
        }

        private void DrawMonths()
        {
            var tabWidth = ContentWidth / MonthFilterLabels.Length;

            for (var index = 0; index < MonthFilterLabels.Length; index++)
            {
                DrawText(MonthFilterLabels[index], ContentLeft + tabWidth * (index + 0.5f), ContentTop, index == (int)_monthFilter ? TitleColor : MutedColor, 0.5f);
            }

            DrawText("MONTH", ContentLeft, MonthHeaderY, MutedColor, 0f);
            DrawText("GAMES", GamesX, MonthHeaderY, MutedColor, 1f);
            DrawText("WINS", WinsX, MonthHeaderY, MutedColor, 1f);
            DrawText("KILLS", KillsX, MonthHeaderY, MutedColor, 1f);
            DrawText("K/D", ContentRight, MonthHeaderY, MutedColor, 1f);
            Draw.Rect(ContentLeft, MonthHeaderY + LineHeight, ContentWidth, 1f, TitleColor * 0.35f);

            var months = (FilteredMonths() ?? []).Take(ShownMonths).ToList();

            if (months.Count == 0)
            {
                DrawText(MonthFilterEmptyTexts[(int)_monthFilter], ContentLeft, MonthRowsTop, MutedColor, 0f);
                return;
            }

            for (var index = 0; index < months.Count; index++)
            {
                var month = months[index];
                var y = MonthRowsTop + index * RowHeight;

                DrawText(GetMonthName(month.Month), ContentLeft, y, Color.White, 0f);
                DrawText(month.Games.ToString(), GamesX, y, Color.White, 1f);
                DrawText(month.Wins.ToString(), WinsX, y, Color.White, 1f);
                DrawText(month.Kills.ToString(), KillsX, y, Color.White, 1f);
                DrawText(KillDeathRatio(month.Kills, month.Deaths), ContentRight, y, Color.White, 1f);
            }
        }

        private List<MonthSummary> FilteredMonths()
        {
            return _monthFilter switch
            {
                MonthFilter.Ranked => _profile.RankedMonths,
                MonthFilter.Casual => _profile.CasualMonths,
                _ => _profile.Months,
            };
        }

        private void DrawPager()
        {
            Draw.OutlineTextCentered(TFGame.Font, $"{_page + 1} / {PageCount}", new Vector2(160f, PagerY), MutedColor, Color.Black);

            if (_page > 0)
            {
                DrawArrow(new Vector2(PanelX - ArrowGap, PanelY + PanelHeight / 2f), MathHelper.Pi);
            }

            if (_page < PageCount - 1)
            {
                DrawArrow(new Vector2(PanelX + PanelWidth + ArrowGap, PanelY + PanelHeight / 2f), 0f);
            }
        }

        private double GetBestStat(string stat)
        {
            return _profile.Stats != null && _profile.Stats.TryGetValue(stat, out var total) ? total.Best : 0;
        }

        private (int Index, ArcherData.ArcherTypes Type) GetMainArcher()
        {
            var main = (_profile.Archers ?? []).FirstOrDefault(plays => ResolveArcherIndex(plays) >= 0);

            return main == null ? (0, ArcherData.ArcherTypes.Normal) : (ResolveArcherIndex(main), (ArcherData.ArcherTypes)main.ArcherAlt);
        }

        private static int ResolveArcherIndex(ArcherPlays plays)
        {
            var isCustom = !string.IsNullOrEmpty(plays.CustomArcherId);
            var index = isCustom ? ArcherDataExtensions.ResolveCustomArcher(plays.CustomArcherId) : plays.Archer;

            return (!isCustom && index >= ArcherDataExtensions.VanillaArcherCount) || !ArcherDataExtensions.Exists(index, plays.ArcherAlt) ? -1 : index;
        }

        private static ArcherData ResolveArcher(ArcherPlays plays)
        {
            var index = ResolveArcherIndex(plays);

            return index < 0 ? null : ArcherData.Get(index, (ArcherData.ArcherTypes)plays.ArcherAlt);
        }

        private static AwardInfo GetAwardAt(int id)
        {
            var awards = VersusAwards.Awards;

            return awards != null && id >= 0 && id < awards.Length ? awards[id] : null;
        }

        private static string KillDeathRatio(int kills, int deaths)
        {
            if (kills == 0 && deaths == 0)
            {
                return "-";
            }

            return (kills / (double)Math.Max(1, deaths)).ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string ToSeconds(double seconds)
        {
            return seconds > 0 ? seconds.ToString("0.0", CultureInfo.InvariantCulture) + "S" : "-";
        }

        private static string ToPlayTime(double seconds)
        {
            if (seconds <= 0)
            {
                return "-";
            }

            var time = TimeSpan.FromSeconds(seconds);

            return time.TotalHours >= 1 ? $"{(int)time.TotalHours}H {time.Minutes}M" : $"{time.Minutes}M";
        }

        private static string GetMonthName(int month)
        {
            var name = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(Math.Max(1, Math.Min(12, month % 100)));

            return $"{name.ToUpperInvariant()} {month / 100}";
        }

        private static string Fit(string text, float maxWidth)
        {
            while (text.Length > 1 && TextWidth(text) > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text;
        }

        private static float TextWidth(string text)
        {
            return TFGame.Font.MeasureString(text).X;
        }

        private void RenderGraphic(GraphicsComponent graphic, Vector2 center)
        {
            graphic.Position = center - Position;
            graphic.Render();
        }

        private static void DrawMessage(string text)
        {
            Draw.OutlineTextCentered(TFGame.Font, text, new Vector2(160f, PanelY + PanelHeight / 2f), MutedColor, Color.Black);
        }

        private static void DrawRecord(string label, string value, float y)
        {
            DrawText(label, ContentLeft, y, Color.White, 0f);
            DrawText(value, ContentRight, y, TitleColor, 1f);
        }

        private static void DrawText(string text, float x, float y, Color color, float justifyX)
        {
            Draw.OutlineTextJustify(TFGame.Font, text, new Vector2(x, y), color, Color.Black, new Vector2(justifyX, 0f));
        }

        private void DrawArrow(Vector2 center, float angle)
        {
            var vec = Calc.AngleToVector(angle, _arrowWave.Value * 2f);

            Draw.Texture(TFGame.MenuAtlas["portraits/arrow"], center + vec, TitleColor, new Vector2(5f, 5f), 1f, angle);
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
            if (_state != LoadState.Loaded || _page != MonthsPage)
            {
                return;
            }

            _monthFilter = (MonthFilter)(((int)_monthFilter + 1) % MonthFilterLabels.Length);
            Sounds.ui_click.Play();
        }

        protected override void OnSelect()
        {
        }

        protected override void OnDeselect()
        {
        }
    }
}
