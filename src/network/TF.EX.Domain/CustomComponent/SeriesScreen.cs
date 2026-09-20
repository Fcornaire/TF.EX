using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.WebSocket;
using TowerFall;
using Player = TF.EX.Domain.Models.WebSocket.Player;

namespace TF.EX.Domain.CustomComponent
{
    public class SeriesScreen : Entity
    {
        private const int READY_SECONDS = 60;
        private const int START_HOLD_SECONDS = 2;
        private const int TOWER_COLUMNS = 8;
        private const float STRIP_LEFT = 34f;
        private const float STRIP_TOP = 156f;
        private const float STRIP_PITCH = 36f;
        private const float FACE_Y = 76f;
        private const float FACE_HALF = 22.5f;
        private const float FACE_SCALE = 0.9f;
        private const float NAME_Y = 106f;
        private const float STATUS_Y = 116f;
        private const float PANEL_HEIGHT = 78f;
        private const float TOWER_HEADER_Y = 130f;
        private const float UNDER_CURSOR_Y = 218f;
        private const float LEFT_FACE_X = 48f;
        private const float RIGHT_FACE_X = 272f;
        private const float LEFT_PAIR_CENTER_X = 62f;
        private const float RIGHT_PAIR_CENTER_X = 258f;
        private const float PAIR_GAP = 51f;
        private const int TEAM_NAME_LENGTH = 8;
        private const float CENTER_X = 160f;

        private static readonly Color WinBorder = Calc.HexToColor("FFE23F");
        private static readonly Color LoseBorder = Calc.HexToColor("896B61");
        private static readonly Color BlueTeam = Calc.HexToColor("6CB6FF");
        private static readonly Color RedTeam = Calc.HexToColor("FF6B6B");
        private static readonly Color Gold = Calc.HexToColor("FFDC6B");
        private static readonly Color ReadyGreen = Calc.HexToColor("8CE68C");
        private static readonly Color Muted = Color.White * 0.35f;
        private static readonly Vector2 LeftAligned = new Vector2(0f, 0.5f);
        private static readonly Vector2 RightAligned = new Vector2(1f, 0.5f);

        private static Subtexture coin;

        private enum Focus { Tower, Face }

        private readonly MainMenu menu;
        private readonly DateTime deadline = DateTime.UtcNow.AddSeconds(READY_SECONDS);
        private readonly Wiggler cursorWiggler;
        private readonly SineWave arrowWave;

        private Focus focus;
        private int towerCursor;
        private int archerIndex;
        private int altIndex;
        private DateTime busyUntil = DateTime.MinValue;
        private DateTime? startAt;
        private int? pendingPick;
        private bool isStarting;
        private bool isLeaving;

        public SeriesScreen(MainMenu menu) : base(-1)
        {
            this.menu = menu;
            Position = Vector2.Zero;
            Depth = -100;

            cursorWiggler = Wiggler.Create(20, 4f);
            Add(cursorWiggler);
            arrowWave = new SineWave(40);
            Add(arrowWave);

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var series = matchmakingService.GetOwnLobby().Series;
            var own = OwnPlayer(matchmakingService);

            archerIndex = own?.ArcherIndex ?? 0;
            altIndex = own?.ArcherAltIndex ?? 0;
            towerCursor = FirstAvailableTower(series);
            focus = matchmakingService.IsSeriesPicker() && own != null && series?.PickOf(own.Seat) == null ? Focus.Tower : Focus.Face;
        }

        private bool IsBusy => DateTime.UtcNow < busyUntil;

        public override void Update()
        {
            base.Update();

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var lobby = matchmakingService.GetOwnLobby();
            var series = lobby.Series;

            if (lobby.IsEmpty || isLeaving || isStarting)
            {
                return;
            }

            if (series == null || series.IsAborted)
            {
                isLeaving = true;
                menu.State = MainMenu.MenuState.Rollcall;
                return;
            }

            // hold the screen a bit so we can see the last choice
            if (matchmakingService.IsLobbyReady())
            {
                startAt ??= DateTime.UtcNow.AddSeconds(START_HOLD_SECONDS);
                menu.ButtonGuideA.Clear();
                menu.ButtonGuideB.Clear();

                if (DateTime.UtcNow >= startAt.Value)
                {
                    StartNextGame(matchmakingService, lobby);
                }

                return;
            }

            UpdateGuides(matchmakingService, series);

            if (matchmakingService.IsSpectator())
            {
                if (MenuInput.Back)
                {
                    LeaveLobby(matchmakingService);
                }

                return;
            }

            var own = OwnPlayer(matchmakingService);

            if (own == null)
            {
                return;
            }

            if (own.Ready && (archerIndex != own.ArcherIndex || altIndex != own.ArcherAltIndex))
            {
                archerIndex = own.ArcherIndex;
                altIndex = own.ArcherAltIndex;
            }

            if (focus == Focus.Tower && (series.IsFinished || pendingPick.HasValue && series.PickOf(own.Seat) == pendingPick))
            {
                focus = Focus.Face;
                pendingPick = null;
                busyUntil = DateTime.MinValue;
            }

            if (IsBusy)
            {
                return;
            }

            if (focus == Focus.Tower)
            {
                UpdateTowerPick(matchmakingService, series);
            }
            else
            {
                UpdateFace(matchmakingService, lobby, series, own);
            }
        }

        private void UpdateTowerPick(Ports.IMatchmakingService matchmakingService, Series series)
        {
            if (MenuInput.Left)
            {
                MoveCursor(-1);
            }
            else if (MenuInput.Right)
            {
                MoveCursor(1);
            }
            else if (MenuInput.Up || MenuInput.Down)
            {
                MoveCursor(TOWER_COLUMNS);
            }
            else if (MenuInput.Confirm)
            {
                if (series.PlayedMapIds.Contains(towerCursor))
                {
                    Sounds.ui_invalid.Play();
                    return;
                }

                Sounds.ui_click.Play();
                busyUntil = DateTime.UtcNow.AddSeconds(3);

                var mapId = towerCursor;
                pendingPick = mapId;
                Task.Run(() => matchmakingService.PickSeriesMap(mapId));
            }
            else if (MenuInput.Back)
            {
                LeaveLobby(matchmakingService);
            }
        }

        private void MoveCursor(int delta)
        {
            var count = TowerCount();

            if (count == 0)
            {
                return;
            }

            towerCursor = ((towerCursor + delta) % count + count) % count;
            cursorWiggler.Start();
            Sounds.ui_move2.Play();
        }

        private void UpdateFace(Ports.IMatchmakingService matchmakingService, Lobby lobby, Series series, Player own)
        {
            if (own.Ready)
            {
                if (MenuInput.Back)
                {
                    SendOwn(matchmakingService, own, false);
                }

                return;
            }

            if (MenuInput.Left)
            {
                ChangeArcher(lobby, own, -1);
            }
            else if (MenuInput.Right)
            {
                ChangeArcher(lobby, own, 1);
            }
            else if (MenuInput.Alt && TowerFall.GameData.DarkWorldDLC)
            {
                ToggleAlt();
            }
            else if (MenuInput.Confirm)
            {
                SendOwn(matchmakingService, own, true);
            }
            else if (MenuInput.Back)
            {
                if (series.IsInProgress && matchmakingService.IsSeriesPicker() && !series.AwaitingResult)
                {
                    Sounds.ui_clickBack.Play();
                    focus = Focus.Tower;
                    pendingPick = null;
                    towerCursor = series.PickOf(own.Seat) ?? series.NextMapId ?? towerCursor;
                }
                else
                {
                    LeaveLobby(matchmakingService);
                }
            }
        }

        //TODO: handle custom archers
        private void ChangeArcher(Lobby lobby, Player own, int direction)
        {
            var count = ArcherDataExtensions.VanillaArcherCount;
            var candidate = archerIndex < count ? archerIndex : (direction > 0 ? -1 : count);

            for (int step = 0; step < count; step++)
            {
                candidate = ((candidate + direction) % count + count) % count;

                if (!IsSelectable(candidate, lobby, own))
                {
                    continue;
                }

                archerIndex = candidate;
                altIndex = ArcherDataExtensions.Exists(candidate, altIndex) ? altIndex : (int)ArcherData.ArcherTypes.Normal;
                Sounds.ui_move2.Play();
                return;
            }

            Sounds.ui_invalid.Play();
        }

        private static bool IsSelectable(int candidate, Lobby lobby, Player own)
        {
            var isTaken = lobby.Players.Any(pl => pl.Seat != own.Seat && !pl.HasCustomArcher && pl.ArcherIndex == candidate);

            return !isTaken
                && SaveData.Instance.Unlocks.GetArcherUnlocked(candidate)
                && ArcherDataExtensions.Exists(candidate, (int)ArcherData.ArcherTypes.Normal);
        }

        private void ToggleAlt()
        {
            var next = altIndex == (int)ArcherData.ArcherTypes.Normal
                ? (int)ArcherData.ArcherTypes.Alt
                : (int)ArcherData.ArcherTypes.Normal;

            if (!ArcherDataExtensions.Exists(archerIndex, next))
            {
                Sounds.ui_invalid.Play();
                return;
            }

            altIndex = next;
            Sounds.ui_altCostumeShift.Play(CENTER_X);
        }

        private void SendOwn(Ports.IMatchmakingService matchmakingService, Player own, bool ready)
        {
            own.ArcherIndex = archerIndex;
            own.ArcherAltIndex = altIndex;
            own.CustomArcherId = ArcherDataExtensions.GetCustomArcherId(archerIndex, altIndex);
            own.Ready = ready;

            busyUntil = DateTime.UtcNow.AddSeconds(3);

            if (ready)
            {
                Sounds.ui_click.Play();
            }
            else
            {
                Sounds.ui_clickBack.Play();
            }

            _ = matchmakingService.UpdatePlayer(own,
                () => busyUntil = DateTime.MinValue,
                () =>
                {
                    busyUntil = DateTime.MinValue;
                    matchmakingService.RunOnGameThread(() => Sounds.ui_invalid.Play());
                });
        }

        private void LeaveLobby(Ports.IMatchmakingService matchmakingService)
        {
            isLeaving = true;
            Sounds.ui_clickBack.Play();

            var back = matchmakingService.GetOwnLobby().IsPrivate
                ? Models.MenuState.NetplaySelect
                : Models.MenuState.LobbyBrowser;

            Task.Run(() => matchmakingService.LeaveLobby(() => { }, () => { }));

            matchmakingService.ResetPeer();
            matchmakingService.ResetLobby();
            menu.State = back.ToTFModel();
        }

        private void StartNextGame(Ports.IMatchmakingService matchmakingService, Lobby lobby)
        {
            isStarting = true;

            var netplayManager = ServiceCollections.ResolveNetplayManager();

            matchmakingService.RestoreArchersFromLobbyIfNeeded();

            for (int seat = 0; seat < TFGame.Players.Length; seat++)
            {
                TFGame.Players[seat] = lobby.Players.Any(pl => pl.Seat == seat);
            }

            if (lobby.Spectators.Any())
            {
                netplayManager.AddSpectators(lobby.Spectators);
            }

            netplayManager.UpdateNumPlayers(lobby.Players.Count);

            TFGame.Instance.Commands.Clear();
            TFGame.Instance.Commands.Open = false;

            menu.FadeAction = MainMenu.GotoVersusLevelSelect;
            menu.State = MainMenu.MenuState.Fade;
        }

        private void UpdateGuides(Ports.IMatchmakingService matchmakingService, Series series)
        {
            var own = OwnPlayer(matchmakingService);

            if (matchmakingService.IsSpectator() || own == null)
            {
                menu.ButtonGuideA.Clear();
                menu.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, "LEAVE");
                return;
            }

            if (series.IsFinished)
            {
                SetGuides(own.Ready ? null : "NEW SERIES", own.Ready ? "UNREADY" : "LEAVE");
                return;
            }

            if (focus == Focus.Tower)
            {
                SetGuides("PICK TOWER", "LEAVE");
            }
            else if (own.Ready)
            {
                SetGuides(null, "UNREADY");
            }
            else
            {
                SetGuides("READY", matchmakingService.IsSeriesPicker() ? "CHANGE TOWER" : "LEAVE");
            }
        }

        private void SetGuides(string confirm, string back)
        {
            if (confirm == null)
            {
                menu.ButtonGuideA.Clear();
            }
            else
            {
                menu.ButtonGuideA.SetDetails(MenuButtonGuide.ButtonModes.Confirm, confirm);
            }

            menu.ButtonGuideB.SetDetails(MenuButtonGuide.ButtonModes.Back, back);
        }

        public override void Render()
        {
            base.Render();

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var lobby = matchmakingService.GetOwnLobby();
            var series = lobby.Series;

            if (lobby.IsEmpty || series == null || series.Sides.Count < 2)
            {
                return;
            }

            DrawHeader(matchmakingService, series);
            MenuPanel.DrawPanel(10f, 44f, 300f, PANEL_HEIGHT);
            DrawSide(matchmakingService, lobby, series, 0);
            DrawSide(matchmakingService, lobby, series, 1);
            DrawTally(lobby, series);

            if (series.IsFinished)
            {
                DrawFinished(lobby, series);
            }
            else
            {
                DrawTowers(matchmakingService, lobby, series);
            }
        }

        private void DrawHeader(Ports.IMatchmakingService matchmakingService, Series series)
        {
            Draw.OutlineTextJustify(TFGame.Font, $"BEST OF {series.BestOf}", new Vector2(12f, 38f), Gold, Color.Black, LeftAligned);
            Draw.OutlineTextCentered(TFGame.Font, GetPlayerInstruction(matchmakingService, series), new Vector2(CENTER_X, 38f), Color.White, 1f);

            if (!series.IsInProgress)
            {
                return;
            }

            var secondsLeft = Math.Max(0, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds));

            Draw.OutlineTextJustify(TFGame.Font, $"STARTS IN {secondsLeft}", new Vector2(308f, 38f), secondsLeft <= 10 ? Color.Red : Color.White, Color.Black, RightAligned);
        }

        private string GetPlayerInstruction(Ports.IMatchmakingService matchmakingService, Series series)
        {
            if (matchmakingService.IsSpectator())
            {
                return "SPECTATING";
            }

            var own = OwnPlayer(matchmakingService);

            if (own == null)
            {
                return string.Empty;
            }

            if (startAt.HasValue)
            {
                return "STARTING";
            }

            if (own.Ready)
            {
                return "LOCKED IN";
            }

            if (series.IsFinished)
            {
                return "READY FOR A NEW SERIES?";
            }

            return focus == Focus.Tower ? "CHOOSE THE NEXT TOWER" : "CHOOSE YOUR ARCHER";
        }

        private void DrawSide(Ports.IMatchmakingService matchmakingService, Lobby lobby, Series series, int side)
        {
            var seats = series.Sides[side];
            var isCompact = seats.Count > 1;
            var center = side == 0 ?
                    isCompact ?
                        LEFT_PAIR_CENTER_X : LEFT_FACE_X
                    : isCompact ?
                        RIGHT_PAIR_CENTER_X : RIGHT_FACE_X;

            for (int i = 0; i < seats.Count; i++)
            {
                DrawFace(matchmakingService, lobby, series, side, seats[i], center + (i - (seats.Count - 1) / 2f) * PAIR_GAP, isCompact);
            }
        }

        private void DrawFace(Ports.IMatchmakingService matchmakingService, Lobby lobby, Series series, int side, int seat, float x, bool isCompact)
        {
            var player = lobby.Players.FirstOrDefault(pl => pl.Seat == seat);

            if (player == null)
            {
                Draw.OutlineTextCentered(TFGame.Font, "EMPTY", new Vector2(x, FACE_Y), Color.Gray, 1f);
                return;
            }

            var isLocal = player.RoomPeerId == matchmakingService.GetRoomPeerId();
            var index = isLocal ? archerIndex : player.ArcherIndex;
            var alt = isLocal ? altIndex : player.ArcherAltIndex;
            var lastGame = series.Games.LastOrDefault();
            var hasWonLast = lastGame == null || lastGame.WinnerSide == side;
            var archer = ArcherDataExtensions.Exists(index, alt) ? ArcherData.Get(index, (ArcherData.ArcherTypes)alt) : ArcherData.Get(0, ArcherData.ArcherTypes.Normal);
            var face = hasWonLast ? archer.Portraits.Win : archer.Portraits.Lose;
            var isSelecting = isLocal && !player.Ready && (series.IsFinished || focus == Focus.Face);
            var border = hasWonLast ? WinBorder : LoseBorder;

            if (isSelecting && isCompact)
            {
                border = Color.Lerp(border, Color.White, 0.5f + arrowWave.Value * 0.5f);
            }

            Draw.Rect(x - FACE_HALF - 2f, FACE_Y - FACE_HALF - 2f, FACE_HALF * 2f + 4f, FACE_HALF * 2f + 4f, Color.Black);
            Draw.Rect(x - FACE_HALF - 1f, FACE_Y - FACE_HALF - 1f, FACE_HALF * 2f + 2f, FACE_HALF * 2f + 2f, border);
            Draw.Texture(face, new Vector2(x - FACE_HALF, FACE_Y - FACE_HALF), Color.White, Vector2.Zero, FACE_SCALE, 0f);

            if (isSelecting && !isCompact)
            {
                DrawChoiceArrows(x, FACE_Y, FACE_HALF + 6f, WinBorder);
            }

            Draw.OutlineTextCentered(TFGame.Font, isCompact ? Shorten(player.Name) : player.Name, new Vector2(x, NAME_Y), ArcherData.GetColorA(seat), 1f);

            var (status, color) = StatusOf(series, player, side, isSelecting, isCompact);

            Draw.OutlineTextCentered(TFGame.Font, status, new Vector2(x, STATUS_Y), color, 1f);
        }

        private static (string Text, Color Tint) StatusOf(Series series, Player player, int side, bool isSelecting, bool isCompact)
        {
            if (player.Ready)
            {
                return ("READY", ReadyGreen);
            }

            if (series.IsInProgress && series.PickerSide == side && !series.NextMapId.HasValue)
            {
                return series.PickOf(player.Seat).HasValue ?
                        ("PICKED", Gold)
                        : (isCompact ?
                            "PICKING" : "PICKING...", Gold);
            }

            if (isSelecting)
            {
                return (isCompact ? "ARCHER?" : "PICK ARCHER", Color.LightGray);
            }

            return ("...", Color.Gray);
        }

        private void DrawChoiceArrows(float x, float y, float distance, Color tint)
        {
            var arrow = TFGame.MenuAtlas["portraits/arrow"];
            var bob = arrowWave.Value * 2f;

            Draw.Texture(arrow, new Vector2(x - distance - bob, y - 5f), tint, Vector2.Zero, 1f, 0f, SpriteEffects.FlipHorizontally);
            Draw.Texture(arrow, new Vector2(x + distance - 10f + bob, y - 5f), tint, Vector2.Zero, 1f, 0f, SpriteEffects.None);
        }

        private void DrawRowArrows(Vector2 center)
        {
            var arrow = TFGame.MenuAtlas["portraits/arrow"];
            var bob = arrowWave.Value * 2f;
            var origin = new Vector2(5f, 5f);

            Draw.Texture(arrow, new Vector2(center.X, center.Y - 24f - bob), WinBorder, origin, 1f, -MathHelper.PiOver2);
            Draw.Texture(arrow, new Vector2(center.X, center.Y + 24f + bob), WinBorder, origin, 1f, MathHelper.PiOver2);
        }

        private static void DrawTally(Lobby lobby, Series series)
        {
            Draw.OutlineTextCentered(TFGame.Font, $"{series.Wins(0)}  -  {series.Wins(1)}", new Vector2(CENTER_X, 62f), Color.White, 2f);

            coin ??= new Subtexture(TFGame.Atlas["pickups/coin"], 0, 0, 8, 10);

            for (int i = 0; i < series.WinsNeeded; i++)
            {
                Draw.TextureCentered(coin, new Vector2(CENTER_X - 12f - i * 10f, 80f), i < series.Wins(0) ? Color.White : Muted);
                Draw.TextureCentered(coin, new Vector2(CENTER_X + 12f + i * 10f, 80f), i < series.Wins(1) ? Color.White : Muted);
            }

            var last = series.Games.LastOrDefault();

            if (last == null)
            {
                Draw.OutlineTextCentered(TFGame.Font, "FIRST GAME", new Vector2(CENTER_X, 98f), Color.LightGray, 1f);
                return;
            }

            if (series.IsInProgress)
            {
                Draw.OutlineTextCentered(TFGame.Font, $"{SideName(lobby, series, last.WinnerSide)} WINS GAME {series.Games.Count}", new Vector2(CENTER_X, 98f), SideColor(series, last.WinnerSide), 1f);
            }
        }

        private void DrawTowers(Ports.IMatchmakingService matchmakingService, Lobby lobby, Series series)
        {
            var isPicker = matchmakingService.IsSeriesPicker();
            var header = TowerHeader(matchmakingService, lobby, series, isPicker);

            Draw.OutlineTextCentered(TFGame.Font, header, new Vector2(CENTER_X, TOWER_HEADER_Y), series.NextMapId.HasValue ? Gold : Color.White, 1f);

            var shouldShowCursor = isPicker && focus == Focus.Tower;
            var count = TowerCount();
            Vector2? cursorCenter = null;

            for (int id = 0; id < count; id++)
            {
                var theme = TowerFall.GameData.VersusTowers[id].Theme;
                var center = new Vector2(STRIP_LEFT + id % TOWER_COLUMNS * STRIP_PITCH, STRIP_TOP + id / TOWER_COLUMNS * STRIP_PITCH);
                var played = series.Games.FirstOrDefault(game => game.MapId == id);
                var isSelected = series.NextMapId == id;
                var cursor = shouldShowCursor && towerCursor == id;
                var alpha = played != null ? 0.35f : 1f;
                var scale = cursor ?
                    1.15f + cursorWiggler.Value * 0.1f : isSelected
                        ? 1.1f : 1f;

                if (isSelected)
                {
                    var half = 17f * scale;
                    Draw.Rect(center.X - half, center.Y - half, half * 2f, half * 2f, Gold);
                }

                Draw.Texture(MapButton.GetBlockTexture(theme.TowerType), center, Color.White * alpha, new Vector2(15f, 15f), scale, 0f);
                Draw.Texture(theme.Icon, center, MapButton.GetTint(theme.TowerType) * alpha, new Vector2(8f, 8f), scale, 0f);

                var pickers = series.Picks.Where(pick => pick.MapId == id).ToList();

                for (int p = 0; p < pickers.Count; p++)
                {
                    var half = 15f * scale - 1f - p * 2f;
                    Draw.HollowRect(center.X - half, center.Y - half, half * 2f, half * 2f, ArcherData.GetColorA(pickers[p].Seat));
                }

                if (played != null)
                {
                    Draw.Texture(TFGame.MenuAtlas["trials/check"], center, SideColor(series, played.WinnerSide), new Vector2(10f, 10f), 1f, 0f);
                }

                if (cursor)
                {
                    cursorCenter = center;
                }
            }

            if (cursorCenter.HasValue)
            {
                DrawChoiceArrows(cursorCenter.Value.X, cursorCenter.Value.Y, 24f, WinBorder);
                DrawRowArrows(cursorCenter.Value);
            }

            if (shouldShowCursor)
            {
                var underCursor = series.PlayedMapIds.Contains(towerCursor) ? $"{TowerName(towerCursor)} - ALREADY PLAYED" : TowerName(towerCursor);
                Draw.OutlineTextCentered(TFGame.Font, underCursor, new Vector2(CENTER_X, UNDER_CURSOR_Y), series.PlayedMapIds.Contains(towerCursor) ? Color.Red : Color.White, 1f);
            }
        }

        private static string TowerHeader(Ports.IMatchmakingService matchmakingService, Lobby lobby, Series series, bool isPicker)
        {
            if (series.NextMapId.HasValue)
            {
                return $"NEXT TOWER: {TowerName(series.NextMapId.Value)}";
            }

            if (series.AwaitingResult)
            {
                return "GAME IN PROGRESS";
            }

            if (!isPicker)
            {
                var pickerName = series.PickerSide.HasValue ? SideName(lobby, series, series.PickerSide.Value) : "";

                return $"{pickerName} PICKS THE NEXT TOWER";
            }

            var pickerSeats = series.Sides[series.PickerSide.Value].Count;

            return series.PickOf(matchmakingService.GetLocalSeat()).HasValue && series.Picks.Count < pickerSeats ? "TEAMMATE IS PICKING" : "PICK THE NEXT TOWER";
        }

        private static void DrawFinished(Lobby lobby, Series series)
        {
            if (!series.WinnerSide.HasValue)
            {
                return;
            }

            var winner = series.WinnerSide.Value;
            var loser = winner == 0 ? 1 : 0;

            Draw.OutlineTextCentered(TFGame.Font, $"{SideName(lobby, series, winner)} WINS THE SERIES {series.Wins(winner)} - {series.Wins(loser)}", new Vector2(CENTER_X, 140f), Gold, 1.5f);

            var y = 160f;

            foreach (var (game, number) in series.Games.Select((game, number) => (game, number + 1)))
            {
                Draw.OutlineTextCentered(TFGame.Font, $"GAME {number}  {TowerName(game.MapId)}  {ScoreLine(series, game)}", new Vector2(CENTER_X, y), SideColor(series, game.WinnerSide), 1f);
                y += 10f;
            }

        }

        private static string ScoreLine(Series series, SeriesGame game)
        {
            return $"{ScoreOf(0, series, game)} - {ScoreOf(1, series, game)}";
        }

        private static string ScoreOf(int side, Series series, SeriesGame game)
        {
            var seat = series.Sides[side].FirstOrDefault();

            return seat >= 0 && seat < game.Scores.Count ? game.Scores[seat].ToString() : "?";
        }

        private static bool IsTeamSeries(Series series)
        {
            return series.Sides.Any(side => side.Count > 1);
        }

        private static string SideName(Lobby lobby, Series series, int side)
        {
            if (IsTeamSeries(series))
            {
                return side == 0 ? "BLUE" : "RED";
            }

            var seat = series.Sides[side].FirstOrDefault();

            return lobby.Players.FirstOrDefault(pl => pl.Seat == seat)?.Name ?? $"P{seat + 1}";
        }

        private static Color SideColor(Series series, int side)
        {
            return IsTeamSeries(series) ?
                    side == 0 ?
                        BlueTeam : RedTeam
                    : ArcherData.GetColorA(series.Sides[side].FirstOrDefault());
        }

        private static string Shorten(string name)
        {
            return name.Length <= TEAM_NAME_LENGTH ? name : name.Substring(0, TEAM_NAME_LENGTH);
        }

        private static string TowerName(int mapId)
        {
            return mapId >= 0 && mapId < TowerFall.GameData.VersusTowers.Count ? TowerFall.GameData.VersusTowers[mapId].Theme.Name : "RANDOM";
        }

        private static int TowerCount()
        {
            return Math.Min(Models.Constants.NETPLAY_SAFE_MAP.Count(), TowerFall.GameData.VersusTowers.Count);
        }

        private static int FirstAvailableTower(Series series)
        {
            var played = series?.PlayedMapIds.ToList() ?? [];

            for (int id = 0; id < TowerCount(); id++)
            {
                if (!played.Contains(id))
                {
                    return id;
                }
            }

            return 0;
        }

        private static Player OwnPlayer(Ports.IMatchmakingService matchmakingService)
        {
            var peerId = matchmakingService.GetRoomPeerId();

            return matchmakingService.GetOwnLobby().Players.FirstOrDefault(pl => pl.RoomPeerId == peerId);
        }
    }
}
