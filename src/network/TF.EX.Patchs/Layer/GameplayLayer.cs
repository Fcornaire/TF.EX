using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs.Layer
{
    [HarmonyPatch(typeof(GameplayLayer))]
    internal class GameplayLayerPatch
    {
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
                    SpectatorInputDisplay.RenderGuide(ServiceCollections.ResolveWiderSetModApi()?.UIXOffset ?? 0f);

                    var framesBehind = GGRSFFI.netplay_frames_behind();
                    if (framesBehind > 120)
                    {
                        var chasing = netplayManager.IsSpectatorCatchupEnabled();
                        Draw.OutlineTextCentered(
                            TFGame.Font,
                            chasing ? $"CATCHING UP : {framesBehind}" : $"DELAYED : {framesBehind}",
                            new Vector2(160f, 30f),
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

        private static void RenderPings(INetplayManager netplayManager, IMatchmakingService matchmakingService)
        {
            var line = 0;

            if (!matchmakingService.IsSpectator())
            {
                var spectatorCount = matchmakingService.GetOwnLobby().Spectators.Count;
                if (spectatorCount > 0)
                {
                    DrawPingLine(line, $"{spectatorCount}", Color.White, Color.LightGray, "SPECTATORS");
                    line++;
                }
            }

            var perSeat = netplayManager.GetNetworkStatsPerSeat();

            if (perSeat.Count == 0)
            {
                var latency = netplayManager.GetNetworkStats().ping;
                DrawPingLine(line, $"{latency} MS", Color.White, GetColor(latency));
                return;
            }

            foreach (var seat in perSeat.Keys.OrderBy(seat => seat))
            {
                var latency = perSeat[seat].ping;

                DrawPingLine(line, $"{latency} MS", GetArcherColor(seat), GetColor(latency), $"P{seat + 1}");
                line++;
            }
        }

        private static void DrawPingLine(int line, string value, Color labelColor, Color valueColor, string label = null)
        {
            const float LeftEdge = 10f;

            var y = 10f + line * 12f;
            var x = LeftEdge;

            if (!string.IsNullOrEmpty(label))
            {
                var labelText = $"{label} : ";
                var labelWidth = TFGame.Font.MeasureString(labelText).X;

                Draw.OutlineTextCentered(TFGame.Font, labelText, new Vector2(x + labelWidth / 2f, y), labelColor, 1f);
                x += labelWidth;
            }

            var valueWidth = TFGame.Font.MeasureString(value).X;

            Draw.OutlineTextCentered(TFGame.Font, value, new Vector2(x + valueWidth / 2f, y), valueColor, 1f);
        }

        private static Color GetColor(uint latency)
        {
            switch (latency)
            {
                case var n when (n < 60):
                    return Color.LightGreen;
                case var n when (n < 120):
                    return Color.GreenYellow;
                case var n when (n < 150):
                    return Color.OrangeRed;
                default:
                    return Color.Red;
            }
        }

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
