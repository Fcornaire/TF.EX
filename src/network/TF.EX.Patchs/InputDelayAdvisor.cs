using Microsoft.Xna.Framework;
using TF.EX.Domain;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Patchs
{
    public static class InputDelayAdvisor
    {
        private static readonly TimeSpan PingWaitTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ResetHoldDuration = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan EnabledStabilityWindow = TimeSpan.FromSeconds(2);
        private const double FrameMs = 1000.0 / Constants.NETPLAY_FPS;
        private const int Scale = Constants.NETPLAY_FPS / Constants.VANILLA_FPS;

        private static string roomId = "";
        private static string ownSignature = "";
        private static DateTime pingWaitStart;
        private static DateTime holdStart;
        private static DateTime? enabledPendingSince;
        private static bool pressActive;
        private static bool awaitRelease;
        private static TowerFall.PlayerInput gestureInput;
        private static bool displaying;
        private static int proposedDelay;
        private static int currentDelay;
        private static int? appliedDelay;

        public static bool ConsumedAlt2 { get; private set; }

        public static void Update(MainMenu mainMenu)
        {
            ConsumedAlt2 = false;

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

            displaying = false;

            var mode = NetplayPreferences.AutoAdjustInputDelay;

            if (mode == AutoAdjustInputDelayMode.Disabled || matchmakingService.IsSpectator())
            {
                return;
            }

            if (mainMenu.State != MainMenu.MenuState.Rollcall)
            {
                return;
            }

            if (lobby.RoomId != roomId)
            {
                Reset(netplayManager);
                roomId = lobby.RoomId;
            }

            var localPeerId = matchmakingService.GetRoomPeerId();
            var remotes = lobby.Players.Where(player => player.RoomPeerId != localPeerId).ToArray();

            var signature = string.Join(",", remotes.Select(player => player.RoomPeerId).OrderBy(id => id));
            if (signature != ownSignature)
            {
                ownSignature = signature;
                pingWaitStart = DateTime.UtcNow;
            }

            if (remotes.Length == 0)
            {
                return;
            }

            if (remotes.Any(player => player.Ping == 0) && DateTime.UtcNow - pingWaitStart < PingWaitTimeout)
            {
                return;
            }

            var laggiest = remotes.Max(player => matchmakingService.GetPingTo(player));
            proposedDelay = Math.Clamp((int)Math.Ceiling(laggiest / 2.0 / FrameMs), NetplayPreferences.MinInputDelay * Scale, NetplayPreferences.MaxInputDelay * Scale);
            currentDelay = appliedDelay ?? NetplayPreferences.InputDelay * Scale;

            if (mode == AutoAdjustInputDelayMode.Enabled)
            {
                if (proposedDelay > currentDelay || proposedDelay < currentDelay - 1)
                {
                    enabledPendingSince ??= DateTime.UtcNow;

                    if (appliedDelay == null || DateTime.UtcNow - enabledPendingSince >= EnabledStabilityWindow)
                    {
                        enabledPendingSince = null;
                        Apply(netplayManager);
                    }
                }
                else
                {
                    enabledPendingSince = null;
                }

                return;
            }

            displaying = true;

            if (awaitRelease)
            {
                ConsumedAlt2 = true;

                if (!GestureHeld())
                {
                    awaitRelease = false;
                    gestureInput = null;
                }

                return;
            }

            if (!pressActive && PressBegan())
            {
                if (proposedDelay != currentDelay || appliedDelay != null)
                {
                    ConsumedAlt2 = true;
                    pressActive = true;
                    holdStart = DateTime.UtcNow;
                }
                else
                {
                    gestureInput = null;
                }
            }

            if (!pressActive)
            {
                return;
            }

            ConsumedAlt2 = true;

            if (GestureHeld())
            {
                if (DateTime.UtcNow - holdStart >= ResetHoldDuration)
                {
                    pressActive = false;
                    awaitRelease = true;

                    if (appliedDelay != null)
                    {
                        appliedDelay = null;
                        netplayManager.ClearSessionInputDelay();
                        currentDelay = NetplayPreferences.InputDelay * Scale;
                        Sounds.ui_clickBack.Play();
                    }
                }

                return;
            }

            pressActive = false;
            gestureInput = null;

            if (proposedDelay != currentDelay)
            {
                Apply(netplayManager);
                currentDelay = proposedDelay;
            }
        }

        public static void Render()
        {
            if (displaying)
            {
                Monocle.Draw.OutlineTextCentered(TFGame.Font, $"INPUT DELAY : {Frames(currentDelay)} - PROPOSED : {Frames(proposedDelay)}", new Vector2(130f, 235f), Color.White, Color.Black);

                var input = FirstPlayerInput();
                if (input == null)
                {
                    return;
                }

                DrawGuide(input.Alt2Icon, "ACCEPT", new Vector2(272f, 225f));
                Monocle.Draw.OutlineTextCentered(TFGame.Font, "HOLD TO RESET", new Vector2(280f, 235f), Color.White, Color.Black);

                return;
            }

            if (appliedDelay != null)
            {
                Monocle.Draw.OutlineTextCentered(TFGame.Font, $"INPUT DELAY : {Frames(appliedDelay.Value)}", new Vector2(160f, 235f), Color.White, Color.Black);
            }
        }

        private static string Frames(int delay) => $"{Math.Round(delay / (double)Scale)} ({Math.Round(delay * 1000.0 / Constants.NETPLAY_FPS)}MS)";

        private static void Apply(Domain.Ports.INetplayManager netplayManager)
        {
            netplayManager.SetSessionInputDelay(proposedDelay);
            appliedDelay = proposedDelay;
            Sounds.ui_click.Play();
        }

        private static void Reset(Domain.Ports.INetplayManager netplayManager)
        {
            ownSignature = "";
            pingWaitStart = DateTime.UtcNow;
            enabledPendingSince = null;
            pressActive = false;
            awaitRelease = false;
            gestureInput = null;
            displaying = false;
            appliedDelay = null;
            netplayManager.ClearSessionInputDelay();
        }

        private static void DrawGuide(Monocle.Subtexture icon, string label, Vector2 center)
        {
            var textWidth = TFGame.Font.MeasureString(label).X;
            var totalWidth = icon.Width + 4f + textWidth;
            var left = center.X - totalWidth / 2f;

            Monocle.Draw.OutlineTextureCentered(icon, new Vector2(left + icon.Width / 2f, center.Y), Color.White);
            Monocle.Draw.OutlineTextCentered(TFGame.Font, label, new Vector2(left + icon.Width + 4f + textWidth / 2f, center.Y), Color.White, Color.Black);
        }

        private static TowerFall.PlayerInput FirstPlayerInput()
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

        private static bool PressBegan()
        {
            foreach (var input in TFGame.PlayerInputs)
            {
                if (input != null && input.MenuAlt2)
                {
                    gestureInput = input;
                    return true;
                }
            }

            if (MenuInput.Alt2)
            {
                gestureInput = null;
                return true;
            }

            return false;
        }

        private static bool GestureHeld()
        {
            if (gestureInput != null)
            {
                return gestureInput.MenuAlt2Check;
            }

            return MenuInput.Alt2Check;
        }
    }
}
