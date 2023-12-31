﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using System.Xml;
using TF.EX.Domain;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    public class LevelPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly ISFXService _sfxService;

        private Random random = new Random();

        public LevelPatch(INetplayManager netplayManager, ISFXService sFXService)
        {
            _netplayManager = netplayManager;
            _sfxService = sFXService;
        }

        public void Load()
        {
            On.TowerFall.Level.HandlePausing += Level_HandlePausing;
            On.TowerFall.Level.Update += Level_Update;
            On.TowerFall.Level.ctor += Level_ctor;
            On.TowerFall.Level.CoreRender += Level_CoreRender;
        }

        public void Unload()
        {
            On.TowerFall.Level.HandlePausing -= Level_HandlePausing;
            On.TowerFall.Level.Update -= Level_Update;
            On.TowerFall.Level.ctor -= Level_ctor;
            On.TowerFall.Level.CoreRender -= Level_CoreRender;
        }

        private void Level_CoreRender(On.TowerFall.Level.orig_CoreRender orig, Level self, RenderTarget2D canvas)
        {
            orig(self, canvas);
            var dynLevel = DynamicData.For(self);
            var debugLayer = dynLevel.Get<DebugLayer>("DebugLayer");

            if (debugLayer.Visible)
            {
                var playerColliders = self[GameTags.PlayerCollider].ToList();
                if (playerColliders.Count > 0 && self.Layers.TryGetValue(0, out var layer))
                {
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, layer.BlendState, layer.SamplerState, DepthStencilState.None, RasterizerState.CullNone, layer.Effect, Matrix.Lerp(Matrix.Identity, self.Camera.Matrix, layer.CameraMultiplier));
                    foreach (var collidable in playerColliders)
                    {
                        collidable.DebugRender();
                    }
                    Draw.SpriteBatch.End();
                }


                debugLayer.Render();
            }
        }

        private void Level_ctor(On.TowerFall.Level.orig_ctor orig, Level self, Session session, XmlElement xml)
        {
            orig(self, session, xml);
            var debugLayer = new DebugLayer();
            var dynLevel = DynamicData.For(self);
            dynLevel.Add("DebugLayer", debugLayer);
            self.SetLayer(42, debugLayer);
        }

        private void Level_Update(On.TowerFall.Level.orig_Update orig, Level self)
        {
            if (_netplayManager.HaveFramesToReSimulate())
            {
                var dynTFGame = DynamicData.For(TFGame.Instance);
                dynTFGame.Set("TimeMult", TFGame.TimeRate); //In fixed timestep, TimeMult = TimeRate
            }

            AddPlayersIndicators(self);

            orig(self);

            _netplayManager.SetIsRollbackFrame(false); //Mark the end of the First RBF

            ///Always Update the layer entity list
            ///Normally it should happen naturally at the start of the next frame
            ///But since we need to update the game state with all entities (we don't care about spawned or will be spawned)
            ///It doesn't alter the game because it's done after the original update
            ///Which means the entities that will be spawned next frame won't update in this frame
            UpdateLayersEntityList(self);

            if (_netplayManager.IsReplayMode())
            {
                _netplayManager.UpdateFramesToReSimulate(0);
            }

            if (!_netplayManager.HaveFramesToReSimulate() && _netplayManager.IsSynchronized())
            {
                _sfxService.Synchronize(GGRSFFI.netplay_current_frame(), _netplayManager.IsTestMode());
            }

            SkipLevelLoaderIfNeeded();
        }

        private void Level_HandlePausing(On.TowerFall.Level.orig_HandlePausing orig, Level self)
        {
            if (_netplayManager.IsReplayMode() || _netplayManager.IsDisconnected())
            {
                orig(self);
            }
        }

        private void UpdateLayersEntityList(Level level)
        {
            var gameplayLayer = level.GetGameplayLayer();
            var dynGameplayLayer = DynamicData.For(gameplayLayer);
            dynGameplayLayer.Invoke("UpdateEntityList");

            var versusStartLayer = level.GetHUDLayer();
            var dynVersusStartLayer = DynamicData.For(versusStartLayer);
            dynVersusStartLayer.Invoke("UpdateEntityList");
        }

        private void AddPlayersIndicators(Level self)
        {
            var players = self[GameTags.Player].ToArray();

            float indicatorCounter = -1.0f;
            var nextDouble = -1.0f;

            foreach (TowerFall.Player player in players)
            {
                if (player.Indicator == null)
                {
                    if (nextDouble == -1.0f)
                    {
                        nextDouble = (float)random.NextDouble();
                    }

                    if (nextDouble <= 0.00025f)
                    {
                        if (indicatorCounter == -1.0f)
                        {
                            indicatorCounter = Monocle.Calc.Range(random, 500.0f, 200.0f);
                        }

                        var dynPlayer = DynamicData.For(player);
                        var indicator = new PlayerIndicator(new Vector2(0f, -8f), player.PlayerIndex, false);
                        var counter = DynamicData.For(DynamicData.For(indicator).Get<Counter>("showCounter"));
                        counter.Set("counter", indicatorCounter);

                        dynPlayer.Set("Indicator", indicator);

                        player.Add(indicator);
                    }
                }
            }
        }

        private void SkipLevelLoaderIfNeeded()
        {
            var dynEngine = DynamicData.For(TowerFall.TFGame.Instance);
            var nextScene = dynEngine.Get<Monocle.Scene>("nextScene");
            if (nextScene is LevelLoaderXML)
            {
                _sfxService.Clear();
                dynEngine.Set("scene", dynEngine.Get<Monocle.Scene>("nextScene"));
                while (!(TFGame.Instance.Scene as LevelLoaderXML).Finished)
                {
                    TFGame.Instance.Scene.Update();
                }

                dynEngine.Set("scene", dynEngine.Get<Monocle.Scene>("nextScene"));
                TowerFall.TFGame.Instance.Scene.Begin();
                TowerFall.TFGame.Instance.Scene.Update();

                dynEngine.Set("FrameCounter", GGRSFFI.netplay_current_frame());
            }
        }
    }
}
