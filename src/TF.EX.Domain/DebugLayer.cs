using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;
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

        public override void Update()
        {
            base.Update();
            if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.F1) && (_netplayManager.IsReplayMode() || _netplayManager.IsTestMode()))
            {
                Visible = !Visible;
            }
        }

        public override void Render()
        {
            base.Render();

            int index = 10;

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState, SamplerState, DepthStencilState.None, RasterizerState.CullNone, Effect, Matrix.Lerp(Matrix.Identity, Scene.Camera.Matrix, CameraMultiplier));
            Draw.OutlineTextCentered(TFGame.Font, "DEBUG !(F1 TO HIDE/UNHIDE)", new Vector2(100, index), Color.White, 1.5f);

            if (_netplayManager.GetNetplayMode() == NetplayMode.Replay)
            {
                index += 10;

                var record = _replayService.GetCurrentRecord();
                var gs = record?.GameState;

                Draw.OutlineTextCentered(TFGame.Font, $"FRAME {gs.Frame.ToString().ToUpper()}", new Vector2(30, index), Color.White, 1f);
                index += 20;

                DebugPlayer(gs.Entities.Players, index);
            }

            if (_netplayManager.GetNetplayMode() == NetplayMode.Server)
            {
                index += 10;
                var record = _netplayManager.GetLastRecord();

                if (record != null)
                {
                    var gs = record?.GameState;

                    Draw.OutlineTextCentered(TFGame.Font, $"FRAME {gs.Frame.ToString().ToUpper()}", new Vector2(30, index), Color.White, 1f);

                    index += 20;
                    DebugPlayer(gs.Entities.Players, index);
                }
            }

            Draw.SpriteBatch.End();

        }

        private void DebugPlayer(IEnumerable<TF.EX.Domain.Models.State.Entity.LevelEntity.Player.Player> players, int index)
        {
            foreach (var player in players)
            {
                //var inGamePlayer = (TFGame.Instance.Scene as Level).GetPlayer(player.Index);

                Draw.OutlineTextCentered(TFGame.Font, $"ARCHER {player.Index.ToString().ToUpper()}", new Vector2(30, index), ArcherData.GetColorA(player.Index), 1f);
                index += 10;
                Draw.OutlineTextCentered(TFGame.Font, $"    POSITION {player.Position.ToTFVector().ToString().ToUpper()}", new Vector2(60, index), ArcherData.GetColorA(player.Index), 1f);
                index += 10;
                Draw.OutlineTextCentered(TFGame.Font, $"    SPEED {player.Speed.ToTFVector().ToString().ToUpper()}", new Vector2(60, index), ArcherData.GetColorA(player.Index), 1f);
                index += 10;
                Draw.OutlineTextCentered(TFGame.Font, $"    STATE {player.State.CurrentState.ToTFModel().ToString().ToUpper()}", new Vector2(60, index), ArcherData.GetColorA(player.Index), 1f);

                index += 20;
            }
        }
    }
}