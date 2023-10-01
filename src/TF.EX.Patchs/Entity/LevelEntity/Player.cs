using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TF.EX.TowerFallExtensions;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class PlayerPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;

        public PlayerPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.Player.DoWrapRender += Player_DoWrapRender;
            On.TowerFall.Player.Update += Player_Update;
        }

        public void Unload()
        {
            On.TowerFall.Player.DoWrapRender -= Player_DoWrapRender;
            On.TowerFall.Player.Update -= Player_Update;
        }

        private void Player_Update(On.TowerFall.Player.orig_Update orig, TowerFall.Player self)
        {
            orig(self);

            if (_netplayManager.IsInit())
            {
                var dynPlayer = DynamicData.For(self);

                // Sprite update are done in the DoWrapRender method, so we manually call it here to have it in the game state
                var gameplayLayer = self.Level.GetGameplayLayer();
                var blendState = gameplayLayer.BlendState;
                var samplerState = gameplayLayer.SamplerState;
                var effect = gameplayLayer.Effect;
                var cameraMultiplier = gameplayLayer.CameraMultiplier;

                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, blendState, samplerState, DepthStencilState.None, RasterizerState.CullNone, effect, Matrix.Lerp(Matrix.Identity, self.Scene.Camera.Matrix, cameraMultiplier));
                dynPlayer.Invoke("DoWrapRender");
                Draw.SpriteBatch.End();
            }
        }

        private void Player_DoWrapRender(On.TowerFall.Player.orig_DoWrapRender orig, TowerFall.Player self)
        {
            if (_netplayManager.IsTestMode() || _netplayManager.IsReplayMode())
            {
                self.DebugRender();
            }
            orig(self);
        }

    }
}
