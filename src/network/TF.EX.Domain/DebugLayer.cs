using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Domain
{
    public class DebugLayer : Layer
    {
        private readonly IReplayService _replayService;
        private readonly INetplayManager _netplayManager;

        public DebugLayer()
            : base()
        {
            Visible = false;
            _replayService = ServiceCollections.ResolveReplayService();
            _netplayManager = ServiceCollections.ResolveNetplayManager();
        }

        public override void Render()
        {
            base.Render();

            int index = 10;

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState, SamplerState, DepthStencilState.None, RasterizerState.CullNone, Effect, Matrix.Lerp(Matrix.Identity, Scene.Camera.Matrix, CameraMultiplier));
            Draw.OutlineTextCentered(TFGame.Font, "DEBUG !(F1 TO HIDE/UNHIDE)", new Vector2(100, index), Color.White, 1.5f);

            var mode = _netplayManager.GetNetplayMode();

            if (mode == NetplayMode.Replay || mode == NetplayMode.Server)
            {
                index += 10;

                var state = _replayService.GetCurrentStateBytes();

                if (state != null)
                {
                    Draw.OutlineTextCentered(TFGame.Font, $"FRAME {StateApi.Current.GetFrameOf(state).ToString().ToUpper()}", new Vector2(30, index), Color.White, 1f);
                    index += 20;

                    DebugPlayers(StateApi.Current.DescribePlayers(state), index);
                }
            }

            Draw.SpriteBatch.End();
        }

        private void DebugPlayers(string[] described, int index)
        {
            foreach (var line in described)
            {
                var parts = line.Split(';');

                if (parts.Length < 4 || !int.TryParse(parts[0], out var playerIndex))
                {
                    continue;
                }

                var color = ArcherData.GetColorA(playerIndex);

                Draw.OutlineTextCentered(TFGame.Font, $"ARCHER {playerIndex.ToString().ToUpper()}", new Vector2(30, index), color, 1f);
                index += 10;
                Draw.OutlineTextCentered(TFGame.Font, $"    POSITION {parts[1].ToUpper()}", new Vector2(60, index), color, 1f);
                index += 10;
                Draw.OutlineTextCentered(TFGame.Font, $"    SPEED {parts[2].ToUpper()}", new Vector2(60, index), color, 1f);
                index += 10;
                Draw.OutlineTextCentered(TFGame.Font, $"    STATE {parts[3].ToUpper()}", new Vector2(60, index), color, 1f);

                index += 20;
            }
        }
    }
}
