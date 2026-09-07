using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs
{
    public static class InputDelayAdvisor
    {
        private static readonly TimeSpan PingWaitTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ResetHoldDuration = TimeSpan.FromSeconds(1.5);
        private static readonly TimeSpan EnabledStabilityWindow = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan StepRepeatDelay = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan StepRepeatInterval = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan CursorLinger = TimeSpan.FromSeconds(2);
        private const double FrameMs = 1000.0 / Constants.NETPLAY_FPS;

        private const float LabelY = 7f;
        private const float ProposedY = 16f;
        private const float TrackY = 25f;
        private const float TrackHeight = 3f;
        private const float TrackMaxWidth = 180f;
        private const float TrackMinLeft = 209f;
        private const float TrackRightMargin = 6f;
        private const float HeadWidth = 2f;
        private const float HeadHeight = 6f;
        private const float ButtonMaxY = 229f;
        private const float GuideBottom = 238f;
        private const float GuideRightMargin = 2f;
        private const float RingMargin = 3f;
        private const float ArrowSize = 10f;
        private static readonly Color ProposedColor = Color.CornflowerBlue;
        private static readonly Color HoverColor = Color.LightGray;

        private static string roomId = "";
        private static string ownSignature = "";
        private static DateTime pingWaitStart;
        private static DateTime? enabledPendingSince;
        private static bool displaying;
        private static int? proposedDelay;
        private static int currentDelay;
        private static int? appliedDelay;

        private static bool pressActive;
        private static bool awaitRelease;
        private static bool adjusted;
        private static DateTime holdStart;
        private static DateTime nextRepeat;
        private static bool leftWasHeld;
        private static bool rightWasHeld;

        private static bool mouseSeen;
        private static int lastRawMouseX;
        private static int lastRawMouseY;
        private static DateTime lastMouseMove = DateTime.MinValue;
        private static Vector2? cursor;
        private static int? hoverDelay;
        private static bool dragging;

        public static bool ConsumedAlt2 { get; private set; }
        public static bool TapReleased { get; private set; }

        private static int MinFrames => NetplayPreferences.ToFrames(NetplayPreferences.MinInputDelay);
        private static int MaxFrames => NetplayPreferences.ToFrames(NetplayPreferences.MaxInputDelay);
        private static float OriginX => ServiceCollections.ResolveWiderSetModApi()?.UIXOffset ?? 0f;
        private static float ScreenRight => 320f + OriginX * 2f;
        private static float TrackRight => ScreenRight - TrackRightMargin;
        private static float TrackLeft => Math.Max(OriginX + TrackMinLeft, TrackRight - TrackMaxWidth);
        private static float TrackWidth => TrackRight - TrackLeft;

        public static bool CapturesLeftRight(TowerFall.PlayerInput input) => displaying && input.MenuAlt2Check;

        public static void Update(MainMenu mainMenu)
        {
            ConsumedAlt2 = false;
            TapReleased = false;
            displaying = false;
            hoverDelay = null;

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var lobby = matchmakingService.GetOwnLobby();

            if (lobby.IsEmpty)
            {
                if (roomId != "" || appliedDelay != null)
                {
                    Reset(netplayManager);
                    roomId = "";
                }

                return;
            }

            var mode = NetplayPreferences.AutoAdjustInputDelay;

            if (mode == AutoAdjustInputDelayMode.Disabled || matchmakingService.IsSpectator() || mainMenu.State != MainMenu.MenuState.Rollcall)
            {
                ClearInteraction();
                return;
            }

            if (lobby.RoomId != roomId)
            {
                Reset(netplayManager);
                roomId = lobby.RoomId;
            }

            UpdateProposal(matchmakingService, lobby);
            currentDelay = appliedDelay ?? NetplayPreferences.InputDelayFrames;

            if (mode == AutoAdjustInputDelayMode.Enabled)
            {
                ClearInteraction();
                UpdateEnabled(netplayManager);
                return;
            }

            displaying = true;

            UpdateGesture(netplayManager);
            UpdateMouse(netplayManager);
        }

        private static void UpdateProposal(Domain.Ports.IMatchmakingService matchmakingService, Lobby lobby)
        {
            var localPeerId = matchmakingService.GetRoomPeerId();
            var remotes = lobby.Players.Where(player => player.RoomPeerId != localPeerId).ToArray();

            var signature = string.Join(",", remotes.Select(player => player.RoomPeerId).OrderBy(id => id));
            if (signature != ownSignature)
            {
                ownSignature = signature;
                pingWaitStart = DateTime.UtcNow;
            }

            if (remotes.Length == 0 || remotes.Any(player => player.Ping == 0) && DateTime.UtcNow - pingWaitStart < PingWaitTimeout)
            {
                proposedDelay = null;
                return;
            }

            var laggiest = remotes.Max(player => matchmakingService.GetPingTo(player));
            var uncoveredMs = laggiest / 2.0 - NetplayPreferences.AcceptedRollbackFrames * NetplayPreferences.RollbackFrameMs;

            proposedDelay = Math.Clamp((int)Math.Ceiling(uncoveredMs / FrameMs), MinFrames, MaxFrames);
        }

        private static void UpdateEnabled(INetplayManager netplayManager)
        {
            if (proposedDelay is not int proposed || proposed <= currentDelay && proposed >= currentDelay - 1)
            {
                enabledPendingSince = null;
                return;
            }

            enabledPendingSince ??= DateTime.UtcNow;

            if (appliedDelay == null || DateTime.UtcNow - enabledPendingSince >= EnabledStabilityWindow)
            {
                enabledPendingSince = null;
                Apply(netplayManager, proposed);
                Sounds.ui_click.Play();
            }
        }

        private static void UpdateGesture(INetplayManager netplayManager)
        {
            var input = LocalInput();
            var alt2Held = input?.MenuAlt2Check ?? MenuInput.Alt2Check;

            if (awaitRelease)
            {
                ConsumedAlt2 = true;

                if (!alt2Held)
                {
                    awaitRelease = false;
                }

                return;
            }

            if (!pressActive)
            {
                if (!alt2Held)
                {
                    return;
                }

                pressActive = true;
                adjusted = false;
                holdStart = DateTime.UtcNow;
                leftWasHeld = false;
                rightWasHeld = false;
            }

            ConsumedAlt2 = true;

            if (!alt2Held)
            {
                pressActive = false;
                TapReleased = !adjusted;
                return;
            }

            var leftHeld = input?.MenuLeftCheck ?? MenuInput.LeftCheck;
            var rightHeld = input?.MenuRightCheck ?? MenuInput.RightCheck;
            var step = StepFrom(leftHeld, rightHeld, out var edge);

            if (step != 0)
            {
                adjusted = true;

                if (Move(netplayManager, currentDelay + step) && edge)
                {
                    Sounds.ui_move2.Play();
                }
            }

            if (!adjusted && DateTime.UtcNow - holdStart >= ResetHoldDuration)
            {
                pressActive = false;
                awaitRelease = true;
                ResetOverride(netplayManager);
            }
        }

        private static int StepFrom(bool leftHeld, bool rightHeld, out bool edge)
        {
            var leftEdge = leftHeld && !leftWasHeld;
            var rightEdge = rightHeld && !rightWasHeld;
            leftWasHeld = leftHeld;
            rightWasHeld = rightHeld;

            edge = leftEdge || rightEdge;

            if (edge)
            {
                nextRepeat = DateTime.UtcNow + StepRepeatDelay;
                return rightEdge ? 1 : -1;
            }

            var direction = (rightHeld ? 1 : 0) - (leftHeld ? 1 : 0);

            if (direction == 0 || DateTime.UtcNow < nextRepeat)
            {
                return 0;
            }

            nextRepeat = DateTime.UtcNow + StepRepeatInterval;
            return direction;
        }

        private static void UpdateMouse(INetplayManager netplayManager)
        {
            var raw = Microsoft.Xna.Framework.Input.Mouse.GetState();

            if (raw.X != lastRawMouseX || raw.Y != lastRawMouseY)
            {
                if (mouseSeen)
                {
                    lastMouseMove = DateTime.UtcNow;
                }

                mouseSeen = true;
                lastRawMouseX = raw.X;
                lastRawMouseY = raw.Y;
            }

            if (!MInput.Mouse.LeftCheck)
            {
                dragging = false;
            }

            if (!dragging && DateTime.UtcNow - lastMouseMove > CursorLinger)
            {
                cursor = null;
                return;
            }

            var position = GameMousePosition();
            cursor = position;

            var overTrack = TrackContains(position);

            if (MInput.Mouse.LeftPressed && overTrack)
            {
                dragging = true;
            }

            if (!dragging && !overTrack)
            {
                return;
            }

            hoverDelay = DelayAt(position.X);

            if (dragging && Move(netplayManager, hoverDelay.Value) && MInput.Mouse.LeftPressed)
            {
                Sounds.ui_click.Play();
            }
        }

        private static bool Move(INetplayManager netplayManager, int target)
        {
            target = Math.Clamp(target, MinFrames, MaxFrames);

            if (target == currentDelay)
            {
                return false;
            }

            Apply(netplayManager, target);
            return true;
        }

        private static void ResetOverride(INetplayManager netplayManager)
        {
            if (appliedDelay == null)
            {
                return;
            }

            appliedDelay = null;
            netplayManager.ClearSessionInputDelay();
            currentDelay = NetplayPreferences.InputDelayFrames;
            Sounds.ui_clickBack.Play();
        }

        private static void Apply(INetplayManager netplayManager, int frames)
        {
            netplayManager.SetSessionInputDelay(frames);
            appliedDelay = frames;
            currentDelay = frames;
        }

        private static void ClearInteraction()
        {
            pressActive = false;
            awaitRelease = false;
            adjusted = false;
            dragging = false;
            cursor = null;
        }

        private static void Reset(INetplayManager netplayManager)
        {
            ownSignature = "";
            pingWaitStart = DateTime.UtcNow;
            enabledPendingSince = null;
            proposedDelay = null;
            displaying = false;
            appliedDelay = null;
            ClearInteraction();
            netplayManager.ClearSessionInputDelay();
        }

        public static void Render()
        {
            if (!displaying)
            {
                if (appliedDelay != null)
                {
                    Draw.OutlineTextCentered(TFGame.Font, $"INPUT DELAY : {Ms(appliedDelay.Value)}", new Vector2(OriginX + 160f, 235f), Color.White, Color.Black);
                }

                return;
            }

            RenderTrack();
            RenderGuides();

            if (cursor.HasValue)
            {
                RenderCursor(cursor.Value);
            }
        }

        private static void RenderTrack()
        {
            var left = TrackLeft;
            var width = TrackWidth;
            var centerX = left + width / 2f;
            var progress = (float)currentDelay / MaxFrames;

            Draw.OutlineTextCentered(TFGame.Font, $"DELAY : {Ms(currentDelay)}", new Vector2(centerX, LabelY), Color.White, Color.Black);

            if (hoverDelay is int hover)
            {
                Draw.OutlineTextCentered(TFGame.Font, $">> {Ms(hover)}", new Vector2(centerX, ProposedY), HoverColor, Color.Black);
            }
            else if (proposedDelay is int proposed)
            {
                Draw.OutlineTextCentered(TFGame.Font, $"PROPOSED : {Ms(proposed)}", new Vector2(centerX, ProposedY), ProposedColor, Color.Black);
            }

            Draw.Rect(left - 2f, TrackY - 2f, width + 4f, TrackHeight + 4f, Color.White * 0.55f);
            Draw.Rect(left - 1f, TrackY - 1f, width + 2f, TrackHeight + 2f, Color.Black);
            Draw.Rect(left, TrackY, width, TrackHeight, Color.White * 0.3f);
            Draw.Rect(left, TrackY, width * progress, TrackHeight, Color.White);

            if (proposedDelay is int mark)
            {
                RenderMarker(left + width * mark / MaxFrames, ProposedColor);
            }

            if (hoverDelay is int hovered)
            {
                RenderMarker(left + width * hovered / MaxFrames, HoverColor);
            }

            var headX = left + width * progress - HeadWidth / 2f;
            var headY = TrackY + TrackHeight / 2f - HeadHeight / 2f;

            Draw.Rect(headX - 1f, headY - 1f, HeadWidth + 2f, HeadHeight + 2f, Color.Black * 0.75f);
            Draw.Rect(headX, headY, HeadWidth, HeadHeight, Color.White);
        }

        private static void RenderMarker(float x, Color color)
        {
            Draw.Rect(x - 1f, TrackY - 4f, 3f, TrackHeight + 8f, Color.Black * 0.75f);
            Draw.Rect(x, TrackY - 3f, 1f, TrackHeight + 6f, color);
        }

        private static void RenderGuides()
        {
            var input = LocalInput();
            if (input == null)
            {
                return;
            }

            var icon = input.Alt2Icon;
            var keys = input.LeftIcon != null && input.RightIcon != null;
            var ringRadius = MathF.Ceiling(Math.Max(icon.Width, icon.Height) / 2f) + RingMargin;
            var arrowsWidth = keys ? input.LeftIcon.Width + 1f + input.RightIcon.Width : ArrowSize * 2f + 1f;
            var topRowWidth = Measure("+") + 3f + arrowsWidth + 4f + Measure("ADJUST");
            var guideX = ScreenRight - GuideRightMargin - Math.Max(topRowWidth, Measure("HOLD TO RESET"));
            var button = new Vector2(guideX - 4f - ringRadius, Math.Min(ButtonMaxY, GuideBottom - ringRadius));
            var topY = button.Y - 6f;
            var bottomY = button.Y + 7f;

            if (pressActive && !adjusted)
            {
                var fraction = (float)Math.Clamp((DateTime.UtcNow - holdStart) / ResetHoldDuration, 0.0, 1.0);

                RenderRing(button, ringRadius - 1f, 1f, Color.Black * 0.75f);
                RenderRing(button, ringRadius, 1f, Color.Black * 0.75f);
                RenderRing(button, ringRadius + 1f, 1f, Color.Black * 0.75f);
                RenderRing(button, ringRadius, fraction, Color.White);
            }

            Draw.OutlineTextureCentered(icon, button, Color.White);

            var x = DrawText("+", guideX, topY) + 3f;

            if (keys)
            {
                x = DrawIcon(input.LeftIcon, x, topY) + 1f;
                x = DrawIcon(input.RightIcon, x, topY) + 4f;
            }
            else
            {
                x = DrawArrows(x, topY) + 4f;
            }

            DrawText("ADJUST", x, topY);
            DrawText("HOLD TO RESET", guideX, bottomY);
        }

        private static float Measure(string text) => TFGame.Font.MeasureString(text).X;

        private static float DrawText(string text, float left, float centerY)
        {
            var width = Measure(text);

            Draw.OutlineTextCentered(TFGame.Font, text, new Vector2(left + width / 2f, centerY), Color.White, Color.Black);

            return left + width;
        }

        private static float DrawIcon(Subtexture icon, float left, float centerY)
        {
            Draw.OutlineTextureCentered(icon, new Vector2(left + icon.Width / 2f, centerY), Color.White);

            return left + icon.Width;
        }

        private static float DrawArrows(float left, float centerY)
        {
            var arrow = TFGame.MenuAtlas["portraits/arrow"];

            DrawOutlinedArrow(arrow, new Vector2(left + ArrowSize / 2f, centerY), SpriteEffects.FlipHorizontally);
            DrawOutlinedArrow(arrow, new Vector2(left + ArrowSize * 1.5f + 1f, centerY), SpriteEffects.None);

            return left + ArrowSize * 2f + 1f;
        }

        private static void DrawOutlinedArrow(Subtexture arrow, Vector2 center, SpriteEffects effects)
        {
            var origin = new Vector2(arrow.Width / 2f, arrow.Height / 2f);

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        Draw.Texture(arrow, center + new Vector2(dx, dy), Color.Black, origin, 1f, 0f, effects);
                    }
                }
            }

            Draw.Texture(arrow, center, Color.White, origin, 1f, 0f, effects);
        }

        private static void RenderRing(Vector2 center, float radius, float fraction, Color color)
        {
            var samples = (int)Math.Ceiling(MathHelper.TwoPi * radius * 1.5f);
            var steps = (int)Math.Round(fraction * samples);

            for (var i = 0; i < steps; i++)
            {
                var angle = -MathHelper.PiOver2 + MathHelper.TwoPi * i / samples;
                var x = MathF.Round(center.X + MathF.Cos(angle) * radius);
                var y = MathF.Round(center.Y + MathF.Sin(angle) * radius);

                Draw.Rect(x, y, 1f, 1f, color);
            }
        }

        private static void RenderCursor(Vector2 at)
        {
            if (at.X < -4f || at.X > 324f + OriginX * 2f || at.Y < -4f || at.Y > 244f)
            {
                return;
            }

            Draw.Rect(at.X - 4f, at.Y - 1f, 9f, 3f, Color.Black * 0.75f);
            Draw.Rect(at.X - 1f, at.Y - 4f, 3f, 9f, Color.Black * 0.75f);
            Draw.Rect(at.X - 3f, at.Y, 7f, 1f, Color.White);
            Draw.Rect(at.X, at.Y - 3f, 1f, 7f, Color.White);
        }

        private static bool TrackContains(Vector2 point)
            => point.X >= TrackLeft - 2f && point.X <= TrackLeft + TrackWidth + 2f
               && point.Y >= TrackY - 8f && point.Y <= TrackY + TrackHeight + 8f;

        private static int DelayAt(float x)
        {
            var ratio = (x - TrackLeft) / TrackWidth;

            return Math.Clamp((int)MathF.Round(ratio * MaxFrames), MinFrames, MaxFrames);
        }

        private static Vector2 GameMousePosition()
        {
            var game = TFGame.Instance;
            var screen = game?.Screen;

            if (screen == null || screen.Scale <= 0f)
            {
                return Vector2.Zero;
            }

            var raw = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var backBuffer = game.GraphicsDevice.PresentationParameters;
            var client = game.Window.ClientBounds;

            var toBackBufferX = client.Width > 0 ? backBuffer.BackBufferWidth / (float)client.Width : 1f;
            var toBackBufferY = client.Height > 0 ? backBuffer.BackBufferHeight / (float)client.Height : 1f;

            var letterboxY = (backBuffer.BackBufferHeight - screen.ScaledHeight) / 2f;

            return new Vector2(
                (raw.X * toBackBufferX - screen.DrawRect.X) / screen.Scale,
                (raw.Y * toBackBufferY - letterboxY - screen.DrawRect.Y) / screen.Scale);
        }

        private static string Ms(int delay) => $"{Math.Round(delay * FrameMs)}MS";

        private static TowerFall.PlayerInput LocalInput()
        {
            var inputService = ServiceCollections.ResolveInputService();
            var localIndex = inputService.GetLocalPlayerInputIndex();

            TowerFall.PlayerInput fallback = null;

            foreach (var input in TFGame.PlayerInputs)
            {
                if (input == null || input is FakeController)
                {
                    continue;
                }

                if (inputService.GetInputIndex(input) == localIndex)
                {
                    return input;
                }

                fallback ??= input;
            }

            return fallback;
        }
    }
}
