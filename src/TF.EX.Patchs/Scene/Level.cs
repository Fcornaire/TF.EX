using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using System.Xml;
using TF.EX.Domain;
using TF.EX.Domain.Externals;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    [HarmonyPatch(typeof(Level))]
    public class LevelPatch
    {
        private static Random random = new Random();

        [HarmonyPostfix]
        [HarmonyPatch("CoreRender")]
        public static void Level_CoreRender(Level __instance)
        {
            var dynLevel = DynamicData.For(__instance);
            var debugLayer = dynLevel.Get<DebugLayer>("DebugLayer");

            if (debugLayer.Visible)
            {
                var playerColliders = __instance[GameTags.PlayerCollider].ToList();
                if (playerColliders.Count > 0 && __instance.Layers.TryGetValue(0, out var layer))
                {
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, layer.BlendState, layer.SamplerState, DepthStencilState.None, RasterizerState.CullNone, layer.Effect, Matrix.Lerp(Matrix.Identity, __instance.Camera.Matrix, layer.CameraMultiplier));
                    foreach (var collidable in playerColliders)
                    {
                        collidable.DebugRender();
                    }
                    Draw.SpriteBatch.End();
                }


                debugLayer.Render();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch([typeof(Session), typeof(XmlElement)])]
        public static void Level_ctor(Level __instance)
        {
            var debugLayer = new DebugLayer();
            var dynLevel = DynamicData.For(__instance);
            dynLevel.Set("DebugLayer", debugLayer);
            __instance.SetLayer(42, debugLayer);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void Level_Update__Prefix(Level __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (netplayManager.HaveFramesToReSimulate())
            {
                var dynTFGame = DynamicData.For(TFGame.Instance);
                dynTFGame.Set("TimeMult", TFGame.TimeRate); //In fixed timestep, TimeMult = TimeRate
            }

            AddPlayersIndicators(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Level_Update__Postfix(Level __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var sfxService = ServiceCollections.ResolveSFXService();

            netplayManager.SetIsRollbackFrame(false); //Mark the end of the First RBF

            ///Always Update the layer entity list
            ///Normally it should happen naturally at the start of the next frame
            ///But since we need to update the game state with all entities (we don't care about spawned or will be spawned)
            ///It doesn't alter the game because it's done after the original update
            ///Which means the entities that will be spawned next frame won't update in this frame
            UpdateLayersEntityList(__instance);

            if (netplayManager.IsReplayMode())
            {
                netplayManager.UpdateFramesToReSimulate(0);
            }

            if (!netplayManager.HaveFramesToReSimulate() && netplayManager.IsSynchronized())
            {
                sfxService.Synchronize(GGRSFFI.netplay_current_frame(), netplayManager.IsTestMode());
            }

            SkipLevelLoaderIfNeeded();
        }

        [HarmonyPrefix]
        [HarmonyPatch("HandlePausing")]
        public static bool Level_HandlePausing(Level __instance)
        {
            var matchMakingService = ServiceCollections.ResolveMatchmakingService();

            var lobby = matchMakingService.GetOwnLobby();
            if (lobby != null && (matchMakingService.IsLobbyReady() || !lobby.IsEmpty))
            {
                return false;
            }

            return true;
        }

        private static void UpdateLayersEntityList(Level level)
        {
            var gameplayLayer = level.GetGameplayLayer();
            var dynGameplayLayer = DynamicData.For(gameplayLayer);
            dynGameplayLayer.Invoke("UpdateEntityList");

            var versusStartLayer = level.GetHUDLayer();
            var dynVersusStartLayer = DynamicData.For(versusStartLayer);
            dynVersusStartLayer.Invoke("UpdateEntityList");
        }

        private static void AddPlayersIndicators(Level self)
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

        private static void SkipLevelLoaderIfNeeded()
        {
            var dynEngine = DynamicData.For(TowerFall.TFGame.Instance);
            var nextScene = dynEngine.Get<Monocle.Scene>("nextScene");
            if (nextScene is LevelLoaderXML)
            {
                var sfxService = ServiceCollections.ResolveSFXService();
                sfxService.Clear();
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
