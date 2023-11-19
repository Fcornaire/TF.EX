using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Engine;
using TowerFall;

namespace TF.EX.Patchs.Layer
{
    internal class GameplayLayerPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly IReplayService _replayService;
        private readonly IInputService _inputService;
        private readonly IMatchmakingService _matchmakingService;

        public GameplayLayerPatch(INetplayManager netplayManager, IInputService inputService, IReplayService replayService, IMatchmakingService matchmakingService)
        {
            _netplayManager = netplayManager;
            _inputService = inputService;
            _replayService = replayService;
            _matchmakingService = matchmakingService;
        }

        public void Load()
        {
            On.TowerFall.GameplayLayer.BatchedRender += GameplayLayer_BatchedRender;
        }

        public void Unload()
        {
            On.TowerFall.GameplayLayer.BatchedRender -= GameplayLayer_BatchedRender;
        }

        private void GameplayLayer_BatchedRender(On.TowerFall.GameplayLayer.orig_BatchedRender orig, TowerFall.GameplayLayer self)
        {
            orig(self);

            if ((_netplayManager.IsReplayMode() || _netplayManager.IsSpectatorMode()) && TFGame.GameLoaded)
            {
                if (TFGamePatch.CustomInputRenderers == null)
                {
                    if (_netplayManager.IsReplayMode())
                    {
                        var replay = _replayService.GetReplay();
                        if (replay.Record.Count > 0)
                        {
                            TFGamePatch.SetupCustomInputRenderer(replay.Record[0].Inputs.Count);
                        }
                    }
                    else
                    {
                        TFGamePatch.SetupCustomInputRenderer(_netplayManager.GetNumPlayers());
                    }
                }

                var inputRenderers = TFGamePatch.CustomInputRenderers;

                for (int i = 0; i < inputRenderers.Length; i++)
                {
                    if (inputRenderers[i] != null)
                    {
                        InputState state = _inputService.GetCurrentInputs().ToArray()[i].ToTFInput();
                        inputRenderers[i].Render(state);
                    }
                }
            }

            if (TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay() || _netplayManager.GetNetplayMode() == NetplayMode.Local)
            {
                if (_matchmakingService.IsSpectator())
                {
                    var lobby = _matchmakingService.GetOwnLobby();
                    Draw.OutlineTextCentered(TFGame.Font, $"SPECTATORS : {lobby.Spectators.Count}", new Vector2(30f, 20f), Color.White, Color.Black);
                }

                var latency = _netplayManager.GetNetworkStats().ping;
                Draw.OutlineTextCentered(TFGame.Font, $"{latency} MS", new Vector2(20f, 10f), GetColor(latency), 1f);

                if (_netplayManager.GetNetplayMode() != TF.EX.Domain.Models.NetplayMode.Test)
                {
                    if (_netplayManager.IsDisconnected())
                    {
                        Draw.OutlineTextCentered(TFGame.Font, "DISCONNECTED", new Vector2(160f, 20f), Color.Red, 2f);
                    }
                }
            }
        }

        private Color GetColor(uint latency)
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
