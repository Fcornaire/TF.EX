using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Engine;
using TowerFall;

namespace TF.EX.Patchs.Layer
{
    internal class GameplayLayerPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly IInputService _inputService;
        private readonly INetplayStateMachine _netplayStateMachine;
        private bool _hasShowedDesynch = false;

        public GameplayLayerPatch(INetplayManager netplayManager, IInputService inputService, INetplayStateMachine netplayStateMachine)
        {
            _netplayManager = netplayManager;
            _inputService = inputService;
            _netplayStateMachine = netplayStateMachine;
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

            if (_netplayManager.IsReplayMode() && TFGame.GameLoaded)
            {
                if (TFGamePatch.ReplayInputRenderers == null)
                {
                    TFGamePatch.SetupReplayInputRenderer();
                }

                var inputRenderers = TFGamePatch.ReplayInputRenderers;

                for (int i = 0; i < inputRenderers.Length; i++)
                {
                    if (inputRenderers[i] != null)
                    {
                        InputState state = _inputService.GetCurrentInputs().ToArray()[i];
                        inputRenderers[i].Render(state);
                    }
                }

                return;
            }

            if (TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay())
            {
                var latency = _netplayManager.GetNetworkStats().ping;
                Draw.OutlineTextCentered(TFGame.Font, $"{latency} MS", new Vector2(20f, 8f), GetColor(latency), 1f);

                if (_netplayManager.GetNetplayMode() != TF.EX.Domain.Models.NetplayMode.Test)
                {
                    if (_netplayManager.HasDesynchronized())
                    {
                        Draw.OutlineTextCentered(TFGame.Font, "DESYNCH DETECTED!", new Vector2(160f, 20f), Color.Red, 2f);

                        if (!_hasShowedDesynch)
                        {
                            Sounds.ui_invalid.Play();
                            _hasShowedDesynch = true;
                        }
                    }

                    if (_netplayManager.IsDisconnected())
                    {
                        Draw.OutlineTextCentered(TFGame.Font, "DISCONNECTED", new Vector2(160f, 20f), Color.Red, 2f);
                    }
                    else if (_netplayManager.IsAttemptingToReconnect())
                    {
                        Draw.OutlineTextCentered(TFGame.Font, "TRYING TO SYNC", new Vector2(160f, 20f), Color.Yellow, 2f);
                    }
                    else if (_netplayManager.IsSyncing())
                    {
                        Draw.OutlineTextCentered(TFGame.Font, "SYNCHRONIZING...", new Vector2(160f, 20f), Color.Green, 2f);
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
