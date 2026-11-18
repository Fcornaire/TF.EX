using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class RankedMatchCard : Entity
    {
        private const float ScreenCenterX = 160f;
        private const float MinPanelWidth = 150f;
        private const float PanelTextMargin = 24f;
        private const float PanelHeight = 180f;
        private const float PanelTop = 40f;
        private const float BackdropAlpha = 0.85f;
        private const float TitleOffsetY = 8f;
        private const float PortraitCenterY = 71f;
        private const float PortraitSize = 50f;
        private const float NameY = 105f;
        private const float RatingY = 118f;
        private const float RatingScale = 1.5f;
        private const float PingY = 131f;
        private const float PingIconGap = 3f;
        private const float StatusY = 146f;
        private const float FirstOptionY = 162f;
        private const float OptionSpacing = 12f;
        private const float OptionsCenterY = FirstOptionY + OptionSpacing / 2f;
        private const float FirstMentionY = 191f;
        private const float MentionSpacing = 10f;
        private const int UrgentSeconds = 5;
        private const string DeclineNoPenaltyLabel = "DECLINE (NO PENALTY)";
        private const string DodgeMentionLine = "DECLINING COUNTS AS A DODGE";
        private const string RedConnectionMentionLine = "UNLESS THE CONNECTION IS BAD (RED)";

        private static readonly Color SelectionA = Calc.HexToColor("F87858");
        private static readonly Color SelectionB = Calc.HexToColor("F7BB59");
        private static readonly Color NoSelection = Calc.HexToColor("F8F8F8");
        private static readonly Color Gold = Calc.HexToColor("FFDC6B");
        private static readonly Vector2 LeftAligned = new(0f, 0.5f);
        private static readonly Color MentionColor = Calc.HexToColor("9A9AB0");

        private readonly Ports.IMatchmakingService _matchmakingService;
        private readonly Action _onAccept;
        private readonly Action _onDecline;
        private readonly PlayerInput[] _originalInputs;
        private readonly PlayerInput[] _blockedInputs;
        private readonly Wiggler _selectionWiggler;
        private readonly float _panelWidth;

        private DateTime? _deadline;
        private bool _isDeclineFree;
        private string[] _options = ["ACCEPT", "DECLINE"];
        private int _optionIndex;
        private bool _isFlashOn;
        private bool _isAccepted;
        private bool _isDeclined;
        private ConfirmDialog _declineDialog;

        public RankedMatchCard(Ports.IMatchmakingService matchmakingService, Action onAccept, Action onDecline) : base(0)
        {
            _matchmakingService = matchmakingService;
            _onAccept = onAccept;
            _onDecline = onDecline;
            _panelWidth = Math.Max(MinPanelWidth, new[] { DodgeMentionLine, RedConnectionMentionLine, DeclineNoPenaltyLabel }.Max(text => TFGame.Font.MeasureString(text).X) + 2f * PanelTextMargin);
            Depth = -10000;

            _originalInputs = MenuInput.MenuInputs;
            _blockedInputs = new PlayerInput[_originalInputs.Length];
            MenuInput.MenuInputs = _blockedInputs;

            Add(_selectionWiggler = Wiggler.Create(20, 4f));
        }

        private bool IsDecided => _isAccepted || _isDeclined;

        private bool IsOpen => _deadline.HasValue;

        private string DeclineDetail => _isDeclineFree ? "NO PENALTY" : "COUNTS AS A DODGE";

        public void Open(int seconds, bool isDeclineFree)
        {
            if (!IsOpen)
            {
                _deadline = DateTime.UtcNow.AddSeconds(seconds);
                _optionIndex = 0;
                Sounds.ui_click.Play();
            }

            _isDeclineFree = isDeclineFree;
            _options = ["ACCEPT", isDeclineFree ? DeclineNoPenaltyLabel : "DECLINE"];
            _declineDialog?.SetDetail(DeclineDetail);
        }

        public void Close()
        {
            CloseDeclineDialog();
            RestoreInputs();

            if (Scene != null)
            {
                RemoveSelf();
            }
        }

        public override void SceneEnd()
        {
            base.SceneEnd();
            RestoreInputs();
        }

        private void RestoreInputs()
        {
            if (MenuInput.MenuInputs == _blockedInputs)
            {
                MenuInput.MenuInputs = _originalInputs;
            }
        }

        public override void Update()
        {
            base.Update();

            if (Scene.OnInterval(4))
            {
                _isFlashOn = !_isFlashOn;
            }

            if (IsDecided)
            {
                return;
            }

            if (FindOpponent(_matchmakingService.GetOwnLobby()) == null)
            {
                CloseDeclineDialog();
                return;
            }

            if (!IsOpen)
            {
                return;
            }

            if (_declineDialog != null)
            {
                UpdateDeclineDialog();
                return;
            }

            if (SecondsLeft() <= 0)
            {
                Decline();
                return;
            }

            if (IsPressed(RawMenuButton.Back))
            {
                MoveCursor(1);
                return;
            }

            var step = IsPressed(RawMenuButton.Down) ? 1 : IsPressed(RawMenuButton.Up) ? -1 : 0;

            if (step != 0)
            {
                MoveCursor(_optionIndex + step);
                return;
            }

            if (IsPressed(RawMenuButton.Confirm))
            {
                if (_optionIndex == 0)
                {
                    Accept();
                }
                else
                {
                    OpenDeclineDialog();
                }
            }
        }

        private void UpdateDeclineDialog()
        {
            if (SecondsLeft() <= 0)
            {
                CloseDeclineDialog();
                Decline();
            }
            else if (_declineDialog.IsCancelled)
            {
                Sounds.ui_clickBack.Play();
                CloseDeclineDialog();
            }
            else if (_declineDialog.IsConfirmed)
            {
                CloseDeclineDialog();
                Decline();
            }
        }

        private void OpenDeclineDialog()
        {
            Sounds.ui_click.Play();
            _declineDialog = new ConfirmDialog("DECLINE?", DeclineDetail, "DECLINE", _originalInputs) { Depth = Depth - 1 };
            Scene.Add(_declineDialog);
        }

        private void CloseDeclineDialog()
        {
            _declineDialog?.Close();
            _declineDialog = null;
        }

        private void MoveCursor(int next)
        {
            next = Math.Clamp(next, 0, _options.Length - 1);

            if (next == _optionIndex)
            {
                return;
            }

            Sounds.ui_move1.Play();
            _optionIndex = next;
            _selectionWiggler.Start();
        }

        private void Accept()
        {
            if (IsDecided)
            {
                return;
            }

            _isAccepted = true;
            Sounds.ui_click.Play();
            _onAccept?.Invoke();
        }

        private void Decline()
        {
            if (IsDecided)
            {
                return;
            }

            _isDeclined = true;
            Sounds.ui_clickBack.Play();
            _onDecline?.Invoke();
        }

        private int SecondsLeft()
        {
            if (!IsOpen)
            {
                return 0;
            }

            return Math.Max(0, (int)Math.Ceiling((_deadline.Value - DateTime.UtcNow).TotalSeconds));
        }

        private Models.WebSocket.Player FindOpponent(Models.WebSocket.Lobby lobby)
        {
            var localPeerId = _matchmakingService.GetRoomPeerId();

            return lobby.Players.Count < 2 ? null : lobby.Players.FirstOrDefault(player => player.RoomPeerId != localPeerId);
        }

        public override void Render()
        {
            var lobby = _matchmakingService.GetOwnLobby();
            var opponent = FindOpponent(lobby);
            var isOpponentPresent = opponent != null;

            Draw.Rect(-ScreenCenterX, 0f, ScreenCenterX * 4f, 240f, Color.Black * BackdropAlpha);
            MenuPanel.DrawPanel(ScreenCenterX - _panelWidth / 2f, PanelTop, _panelWidth, PanelHeight);

            base.Render();

            var titleAt = new Vector2(ScreenCenterX, PanelTop - TitleOffsetY);

            Draw.TextureCentered(TFGame.MenuAtlas["questResults/arrow"], titleAt + new Vector2(0f, -2f), Color.White);
            Draw.OutlineTextCentered(TFGame.Font, "OPPONENT FOUND", titleAt, Color.White, 2f);

            if (isOpponentPresent)
            {
                DrawOpponent(lobby, opponent);

                if (!_isDeclined)
                {
                    DrawOptions();
                }
            }

            DrawStatus(isOpponentPresent);
            DrawMentions();
        }

        private static void DrawMentions()
        {
            Draw.TextCentered(TFGame.Font, DodgeMentionLine, new Vector2(ScreenCenterX, FirstMentionY), MentionColor);
            Draw.TextCentered(TFGame.Font, RedConnectionMentionLine, new Vector2(ScreenCenterX, FirstMentionY + MentionSpacing), MentionColor);
        }

        private void DrawOpponent(Models.WebSocket.Lobby lobby, Models.WebSocket.Player opponent)
        {
            DrawPortrait(lobby, opponent);

            Draw.OutlineTextCentered(TFGame.Font, opponent.Name, new Vector2(ScreenCenterX, NameY), ArcherData.GetColorA(opponent.Seat), 1f);

            DrawRating(lobby, opponent);
            DrawPing(opponent);
        }

        private static void DrawPortrait(Models.WebSocket.Lobby lobby, Models.WebSocket.Player opponent)
        {
            var center = new Vector2(ScreenCenterX, PortraitCenterY);
            var corner = center - new Vector2(PortraitSize / 2f);
            var favorite = lobby.RankedSeats.FirstOrDefault(entry => entry.Seat == opponent.Seat)?.FavoriteArcher;
            var archer = favorite == null ? null : ArcherDataExtensions.ResolvePlayedArcher(favorite.Archer, favorite.ArcherAlt, favorite.CustomArcherId);
            var portrait = archer?.Portraits.Win;

            if (portrait == null)
            {
                Draw.Rect(corner.X, corner.Y, PortraitSize, PortraitSize, Color.Black * 0.4f);
                Draw.OutlineTextJustify(TFGame.Font, "?", center, Color.LightGray, Color.Black, new Vector2(0.5f, 0.5f), 2f);
            }
            else
            {
                var scale = Math.Min(1f, PortraitSize / Math.Max(portrait.Width, portrait.Height));

                Draw.Texture(portrait, center, Color.White, new Vector2(portrait.Width, portrait.Height) / 2f, scale, 0f);
            }

            Draw.HollowRect(corner.X, corner.Y, PortraitSize, PortraitSize, Color.LightGray);
        }

        private static void DrawRating(Models.WebSocket.Lobby lobby, Models.WebSocket.Player opponent)
        {
            var rating = lobby.RankedSeats.FirstOrDefault(entry => entry.Seat == opponent.Seat);

            if (rating != null)
            {
                Draw.OutlineTextCentered(TFGame.Font, rating.Provisional ? "?" : rating.Rating.ToString(), new Vector2(ScreenCenterX, RatingY), Gold, RatingScale);
            }
        }

        private void DrawPing(Models.WebSocket.Player opponent)
        {
            var latency = _matchmakingService.GetPingTo(opponent);
            var text = latency is int ms ? $"{ms} MS" : "... MS";
            var color = _matchmakingService.GetConnectionQualityTo(opponent)?.ToColor() ?? Color.LightGray;
            var isRelayed = _matchmakingService.IsRelayedWith(opponent);
            var width = (isRelayed.HasValue ? ConnectionIcon.Size + PingIconGap : 0f) + TFGame.Font.MeasureString(text).X;
            var left = (float)Math.Floor(ScreenCenterX - width / 2f);

            if (isRelayed is bool relayed)
            {
                ConnectionIcon.Render(new Vector2(left, (float)Math.Floor(PingY - ConnectionIcon.Size / 2f)), relayed, color);
                left += ConnectionIcon.Size + PingIconGap;
            }

            Draw.OutlineTextJustify(TFGame.Font, text, new Vector2(left, PingY), color, Color.Black, LeftAligned);
        }

        private void DrawOptions()
        {
            if (!IsOpen)
            {
                DrawCheckingStatus();
                return;
            }

            if (_isAccepted)
            {
                Draw.TextCentered(TFGame.Font, "WAITING FOR OPPONENT...", new Vector2(ScreenCenterX, OptionsCenterY), Color.White);
                return;
            }

            for (int i = 0; i < _options.Length; i++)
            {
                var at = new Vector2(ScreenCenterX, FirstOptionY + i * OptionSpacing);
                var isSelected = i == _optionIndex;
                var wiggle = isSelected ? _selectionWiggler.Value * 3f : 0f;
                var color = !isSelected ? NoSelection : _isFlashOn ? SelectionB : SelectionA;

                Draw.TextCentered(TFGame.Font, (isSelected ? "> " : string.Empty) + _options[i], at + new Vector2(wiggle, 0f), color);
            }
        }

        private void DrawStatus(bool isOpponentPresent)
        {
            var at = new Vector2(ScreenCenterX, StatusY);

            if (!isOpponentPresent)
            {
                Draw.TextCentered(TFGame.Font, IsOpen ? "OPPONENT DECLINED" : "OPPONENT LEFT", at, Color.Red);
                return;
            }

            if (!IsOpen)
            {
                return;
            }

            var secondsLeft = SecondsLeft();
            var color = secondsLeft <= UrgentSeconds ? Color.Red : Color.White;

            Draw.OutlineTextCentered(TFGame.Font, secondsLeft.ToString(), at, color, 2f);
        }

        private static void DrawCheckingStatus()
        {
            const string label = "CHECKING CONNECTION";
            var dots = (int)(DateTime.UtcNow.TimeOfDay.TotalSeconds * 3d) % 4;
            var left = new Vector2(ScreenCenterX - TFGame.Font.MeasureString(label).X / 2f, OptionsCenterY);

            Draw.TextJustify(TFGame.Font, label + new string('.', dots), left, Color.White, LeftAligned);
        }

        private bool IsPressed(RawMenuButton button)
        {
            return _originalInputs == null
                ? RawMenuInput.IsButtonPressed(button)
                : _originalInputs.Any(input => input != null && IsPressed(input, button));
        }

        private static bool IsPressed(PlayerInput input, RawMenuButton button)
        {
            return button switch
            {
                RawMenuButton.Confirm => input.MenuConfirm,
                RawMenuButton.Back => input.MenuBack,
                RawMenuButton.Up => input.MenuUp,
                RawMenuButton.Down => input.MenuDown,
                _ => false,
            };
        }
    }
}
