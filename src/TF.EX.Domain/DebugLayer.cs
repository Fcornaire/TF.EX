using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Domain
{
    public class DebugLayer : Layer
    {
        private readonly IReplayService _replayService;
        private readonly NetplayMode _netplayMode;
        private readonly IRngService _rngService;

        public DebugLayer()
            : base()
        {
            Visible = false;
            _replayService = ServiceCollections.ResolveReplayService();
            _netplayMode = ServiceCollections.ResolveNetplayManager().GetNetplayMode();
            _rngService = ServiceCollections.ResolveRngService();
        }

        public override void Update()
        {
            base.Update();
            if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.F1))
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

            index += 10;

            if (_netplayMode == NetplayMode.Replay)
            {
                index += 10;
                var gs = _replayService.GetCurrentRecord()?.GameState;

                Draw.OutlineTextCentered(TFGame.Font, $"FRAME {gs.Frame.ToString().ToUpper()}", new Vector2(30, index), Color.White, 1f);

                index += 20;

                foreach (var player in gs.Entities.Players)
                {
                    var t = (TFGame.Instance.Scene as Level)[GameTags.Player][player.Index] as TowerFall.Player;

                    Draw.OutlineTextCentered(TFGame.Font, $"ARCHER {player.Index.ToString().ToUpper()}", new Vector2(30, index), ArcherData.GetColorA(player.Index), 1f);
                    index += 10;
                    Draw.OutlineTextCentered(TFGame.Font, $"    POSITION {player.Position.ToTFVector().ToString().ToUpper()}", new Vector2(60, index), ArcherData.GetColorA(player.Index), 1f);
                    index += 10;
                    Draw.OutlineTextCentered(TFGame.Font, $"    SPEED {player.Speed.ToTFVector().ToString().ToUpper()}", new Vector2(60, index), ArcherData.GetColorA(player.Index), 1f);
                    index += 10;
                    Draw.OutlineTextCentered(TFGame.Font, $"    POSITION COUNTER {player.PositionCounter.ToTFVector().ToString().ToUpper()}", new Vector2(60, index), ArcherData.GetColorA(player.Index), 1f);
                    index += 10;
                    Draw.OutlineTextCentered(TFGame.Font, $"    STATE {player.State.CurrentState.ToTFModel().ToString().ToUpper()}", new Vector2(60, index), ArcherData.GetColorA(player.Index), 1f);

                    index += 20;
                }
            }

            if (_netplayMode == NetplayMode.Server)
            {
                var rng = _rngService.Get();

                Draw.OutlineTextCentered(TFGame.Font, $"{rng.Debug()}", new Vector2(30, index), Color.White, 1.5f);
                index += 10;

                var levelSystem = (TFGame.Instance.Scene as Level).Session.MatchSettings.LevelSystem as VersusLevelSystem;
                var dynLevelSystem = DynamicData.For(levelSystem);
                string lastLevel = dynLevelSystem.Get<string>("lastLevel");

                Draw.OutlineTextCentered(TFGame.Font, $"CURRENT LEVEL {lastLevel.ToString().ToUpper()}", new Vector2(30, index), Color.White, 1.5f);
            }

            Draw.SpriteBatch.End();

        }
    }
}