using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Patchs.Engine;
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
            var replayService = ServiceCollections.ResolveReplayService();
            var inputService = ServiceCollections.ResolveInputService();
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if ((netplayManager.IsReplayMode() || netplayManager.IsSpectatorMode()) && TFGame.GameLoaded)
            {
                if (TFGamePatch.CustomInputRenderers == null)
                {
                    if (netplayManager.IsReplayMode())
                    {
                        var replay = replayService.GetReplay();
                        if (replay.Record.Count > 0)
                        {
                            TFGamePatch.SetupCustomInputRenderer(replay.Record[0].Inputs.Count);
                        }
                    }
                    else
                    {
                        TFGamePatch.SetupCustomInputRenderer(netplayManager.GetNumPlayers());
                    }
                }

                var inputRenderers = TFGamePatch.CustomInputRenderers;
                var player1Index = netplayManager.GetPlayerDraw() == PlayerDraw.Player1 ? 0 : 1;
                var player2Index = netplayManager.GetPlayerDraw() == PlayerDraw.Player1 ? 1 : 0;

                for (int i = 0; i < inputRenderers.Length; i++)
                {
                    if (inputRenderers[i] != null)
                    {
                        var index = i == 0 ? player1Index : player2Index;
                        InputState state = inputService.GetCurrentInputs().ToArray()[index].ToTFInput();
                        inputRenderers[i].Render(state);
                    }
                }
            }

            if (TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay() || netplayManager.GetNetplayMode() == NetplayMode.Local)
            {
                if (matchmakingService.IsSpectator())
                {
                    var lobby = matchmakingService.GetOwnLobby();
                    Draw.OutlineTextCentered(TFGame.Font, $"SPECTATORS : {lobby.Spectators.Count}", new Vector2(30f, 20f), Color.White, Color.Black);
                }

                var latency = netplayManager.GetNetworkStats().ping;
                Draw.OutlineTextCentered(TFGame.Font, $"{latency} MS", new Vector2(20f, 10f), GetColor(latency), 1f);

                if (netplayManager.GetNetplayMode() != TF.EX.Domain.Models.NetplayMode.Test)
                {
                    if (netplayManager.IsDisconnected())
                    {
                        Draw.OutlineTextCentered(TFGame.Font, "DISCONNECTED", new Vector2(160f, 20f), Color.Red, 2f);
                    }
                }
            }
        }

        private static Color GetColor(uint latency)
        {
            var color = Color.White;
            switch (latency)
            {
                case var n when (n >= 0 && n < 60):
                    color = Color.LightGreen;
                    break;
                case var n when (n >= 60 && n < 120):
                    color = Color.GreenYellow;
                    break;
                case var n when (n >= 120 && n < 150):
                    color = Color.OrangeRed;
                    break;
                case var n when (n >= 150):
                    color = Color.Red;
                    break;
                default:
                    break;
            }

            return color;
        }
    }
}
