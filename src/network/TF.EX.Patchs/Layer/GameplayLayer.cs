using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs.Layer
{
    [HarmonyPatch(typeof(GameplayLayer))]
    internal class GameplayLayerPatch
    {

        private const float PingFadedAlpha = 0.2f;
        private const float PingFadeMs = 250f;
        private const float PingLineHeight = 12f;
        private const float PingAreaWidth = 120f;
        private const float PingAreaMargin = 8f;

        private static float pingAlpha = 1f;
        private static long lastPingFadeTick = Environment.TickCount64;


        [HarmonyPostfix]
        [HarmonyPatch("BatchedRender")]
        public static void GameplayLayer_BatchedRender(GameplayLayer __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (netplayManager.IsSpectatorMode() && !netplayManager.IsReplayMode() && TFGame.GameLoaded)
            {
                SpectatorInputDisplay.Render(GGRSFFI.netplay_current_frame());
            }

            if (netplayManager.IsReplayMode())
            {
                return;
            }

            if (TowerFall.MainMenu.VersusMatchSettings?.Mode.IsNetplay() == true || netplayManager.GetNetplayMode() == NetplayMode.Local)
            {
                if (matchmakingService.IsSpectator())
                {
                    var wideOffset = ServiceCollections.ResolveWiderSetModApi()?.UIXOffset ?? 0f;

                    SpectatorInputDisplay.RenderGuide(wideOffset);

                    var framesBehind = GGRSFFI.netplay_frames_behind();
                    if (framesBehind > 2 * TF.EX.Domain.Models.Constants.NETPLAY_FPS)
                    {
                        var chasing = netplayManager.IsSpectatorCatchupEnabled();
                        Draw.OutlineTextCentered(
                            TFGame.Font,
                            chasing ? $"CATCHING UP : {framesBehind}" : $"DELAYED : {framesBehind}",
                            new Vector2(160f + wideOffset, 30f),
                            chasing ? Color.Yellow : Color.LightGray,
                            1f);
                    }
                }

                RenderPings(netplayManager, matchmakingService);

                if (netplayManager.GetNetplayMode() != TF.EX.Domain.Models.NetplayMode.Test)
                {
                    if (netplayManager.IsDisconnected())
                    {
                        Draw.OutlineTextCentered(TFGame.Font, "DISCONNECTED", new Vector2(160f, 20f), Color.Red, 2f);
                    }
                }
            }
        }

        private static float UpdatePingAlpha(int lineCount)
        {
            var area = new Rectangle(
                (int)(10f - PingAreaMargin),
                (int)(10f - PingLineHeight / 2f - PingAreaMargin),
                (int)(PingAreaWidth + PingAreaMargin * 2f),
                (int)(lineCount * PingLineHeight + PingAreaMargin * 2f));

            var isCrowded = TFGame.Instance.Scene is TowerFall.Level level
                && (level[GameTags.Player].Any(player => Overlaps(area, player))
                    || level[GameTags.Arrow].OfType<TowerFall.Arrow>().Any(arrow => IsFlying(arrow) && Overlaps(area, arrow)));

            var now = Environment.TickCount64;
            var step = (now - lastPingFadeTick) / PingFadeMs;
            lastPingFadeTick = now;

            var target = isCrowded ? PingFadedAlpha : 1f;
            pingAlpha = pingAlpha < target ? Math.Min(target, pingAlpha + step) : Math.Max(target, pingAlpha - step);

            return pingAlpha;
        }

        private static bool Overlaps(Rectangle area, Monocle.Entity entity)
        {
            return area.Intersects(new Rectangle((int)entity.Left, (int)entity.Top, (int)entity.Width, (int)entity.Height));
        }

        private static bool IsFlying(TowerFall.Arrow arrow)
        {
            return arrow.State != TowerFall.Arrow.ArrowStates.Stuck && arrow.State != TowerFall.Arrow.ArrowStates.LayingOnGround;
        }

        private static void RenderPings(INetplayManager netplayManager, IMatchmakingService matchmakingService)
        {
            var line = 0;
            var perSeat = netplayManager.GetNetworkStatsPerSeat();
            var spectatorCount = matchmakingService.IsSpectator() ? 0 : matchmakingService.GetOwnLobby().Spectators.Count;
            var alpha = UpdatePingAlpha((spectatorCount > 0 ? 1 : 0) + Math.Max(perSeat.Count, 1));

            if (spectatorCount > 0)
            {
                DrawPingLine(line, $"{spectatorCount}", Color.White, Color.LightGray, alpha, "SPECTATORS");
                line++;
            }

            if (perSeat.Count == 0)
            {
                var latency = netplayManager.GetNetworkStats().ping;
                DrawPingLine(line, $"{latency} MS", Color.White, GetColor(latency), alpha);
                return;
            }

            var lobbyPlayers = matchmakingService.GetOwnLobby().Players;

            foreach (var seat in perSeat.Keys.OrderBy(seat => seat))
            {
                var latency = perSeat[seat].ping;
                var seated = lobbyPlayers.FirstOrDefault(player => player.Seat == seat);
                var isRelayed = seated == null ? null : matchmakingService.IsRelayedWith(seated);

                DrawPingLine(line, $"{latency} MS", GetArcherColor(seat), GetColor(latency), alpha, $"P{seat + 1}", isRelayed);
                line++;
            }
        }

        private static void DrawPingLine(int line, string value, Color labelColor, Color valueColor, float alpha, string label = null, bool? isRelayed = null)
        {
            const float LeftEdge = 10f;

            var y = 10f + line * PingLineHeight;
            var x = LeftEdge;

            if (!string.IsNullOrEmpty(label))
            {
                var labelText = $"{label} : ";
                var labelWidth = TFGame.Font.MeasureString(labelText).X;

                Draw.OutlineTextCentered(TFGame.Font, labelText, new Vector2(x + labelWidth / 2f, y), labelColor * alpha, Color.Black * ConnectionIcon.OutlineAlpha(alpha));
                x += labelWidth;
            }

            var valueWidth = TFGame.Font.MeasureString(value).X;

            Draw.OutlineTextCentered(TFGame.Font, value, new Vector2(x + valueWidth / 2f, y), valueColor * alpha, Color.Black * ConnectionIcon.OutlineAlpha(alpha));

            if (isRelayed is bool relayed)
            {
                ConnectionIcon.Render(new Vector2(MathF.Floor(x + valueWidth + 3f), MathF.Floor(y - ConnectionIcon.Size / 2f)), relayed, valueColor, alpha);
            }
        }

        private static Color GetColor(uint latency) => ConnectionQualities.GetQualityByLatency((int)latency).ToColor();

        private static Color GetArcherColor(int seat)
        {
            if (seat < 0 || seat >= TFGame.Characters.Length)
            {
                return Color.White;
            }

            var characterIndex = TFGame.Characters[seat];

            if (characterIndex < 0 || characterIndex >= ArcherData.Archers.Length)
            {
                return Color.White;
            }

            return ArcherData.Archers[characterIndex].ColorA;
        }
    }
}
