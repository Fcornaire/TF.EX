using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Chest;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;
using TF.EX.Domain.Models.State.Layer;
using TF.EX.TowerFallExtensions.Entity.LevelEntity;
using TF.EX.TowerFallExtensions.Layer;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.TowerFallExtensions
{
    public static class LevelExtensions
    {
        //TODO: remove and use Scene extension instead
        public static void Spawn<T>(this Level level, T toSpawn, double actualDepth) where T : Monocle.Entity
        {
            var dynEntity = DynamicData.For(toSpawn);
            dynEntity.Set("Scene", level);
            dynEntity.Set("Level", level);
            dynEntity.Set("actualDepth", actualDepth);

            level.GetGameplayLayer().Entities.Add(toSpawn);
            toSpawn.Added();
        }

        public static void Delete<T>(this Level level) where T : Monocle.Entity
        {
            var entity = level.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is T) as T;

            if (entity != null)
            {
                level.Layers.FirstOrDefault(layer => layer.Value.Index == entity.LayerIndex).Value.Entities.Remove(entity);
                entity.Removed();
            }
        }

        //TODO: remove and use Scene extension instead
        public static void DeleteAll<T>(this Level level) where T : Monocle.Entity
        {
            var entities = level.Layers.SelectMany(layer => layer.Value.Entities)
                .Where(ent => ent is T).Select(ent => ent as T).ToList();

            if (entities.Count > 0)
            {
                entities.ForEach(entity =>
                {
                    level.Layers.FirstOrDefault(layer => layer.Value.Index == entity.LayerIndex).Value.Entities.Remove(entity);
                    entity.Removed();
                });
            }
        }

        //TODO: remove and use Scene extension instead
        public static T Get<T>(this Level level) where T : Monocle.Entity
        {
            return level.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is T) as T;
        }

        //TODO: remove and use Scene extension instead
        public static IEnumerable<T> GetAll<T>(this Level level) where T : Monocle.Entity
        {
            return level.Layers.SelectMany(layer => layer.Value.Entities)
                .Where(ent => ent is T).Select(ent => ent as T);
        }


        public static void SortGamePlayLayer(this Level level, Comparison<Monocle.Entity> comparison)
        {
            level.GetGameplayLayer().Entities.Sort(comparison);
        }

        public static void SortHUDLayer(this Level level, Comparison<Monocle.Entity> comparison)
        {
            level.GetHUDLayer().Entities.Sort(comparison);
        }

        public static Monocle.Layer GetGameplayLayer(this Level level)
        {
            return level.Layers.FirstOrDefault(l => l.Value.IsGameplayLayer()).Value;
        }

        public static Monocle.Layer GetHUDLayer(this Level level)
        {
            return level.Layers.FirstOrDefault(l => l.Value.IsHUDLayer()).Value;
        }

        public static void ResetState(this Level level)
        {
            level.DeleteAll<TowerFall.Arrow>();
            level.DeleteAll<TreasureChest>();
            level.DeleteAll<TowerFall.Pickup>();
            level.DeleteAll<TowerFall.PlayerCorpse>();
        }

        public static GameState GetState(this Level self)
        {
            var gameState = new GameState();

            var hudService = ServiceCollections.ResolveHUDService();
            var rngService = ServiceCollections.ResolveRngService();
            var sfxService = ServiceCollections.ResolveSFXService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();

            gameState.AddJumpPadsState(self);

            if (!netplayManager.IsTestMode())
            {
                gameState.SFXs = sfxService.Get().Select(sfx => new SFXState
                {
                    Frame = sfx.Frame,
                    Name = sfx.Name,
                    Pan = sfx.Pan,
                    Pitch = sfx.Pitch,
                    Volume = sfx.Volume,
                });
            }
            gameState.AddRoundLogicState(self);
            gameState.Entities.Hud = hudService.Get();
            gameState.AddPlayersState(self);
            gameState.AddPlayersCorpseState(self);
            gameState.AddArrowState(self);
            gameState.AddChestsState(self);
            gameState.AddPickupsState(self);
            gameState.AddSessionState(self);
            gameState.AddOrbState(self);
            gameState.AddLanternState(self);
            gameState.AddChainState(self);
            gameState.AddLayerState(self);
            gameState.ScreenOffset = TFGame.Instance.Screen.Offset.ToModel();
            gameState.AddOrbsState(self);
            gameState.AddLavaControlState(self);
            gameState.AddBGTorches(self);
            gameState.Rng = rngService.Get();
            gameState.Frame = GGRSFFI.netplay_current_frame();

            if (netplayManager.IsTestMode())
            {
                gameState.MatchStats = new TF.EX.Domain.Models.State.MatchStats[]
                {
                    self.Session.MatchStats[0].ToDomain(), self.Session.MatchStats[1].ToDomain(),
                };
            }
            else
            {
                gameState.MatchStats = new TF.EX.Domain.Models.State.MatchStats[]
               {
                   self.Session.MatchStats[inputService.GetLocalPlayerInputIndex()].ToDomain(),
                   self.Session.MatchStats[inputService.GetRemotePlayerInputIndex()].ToDomain(),
               };
            }

            gameState.AddCrackedPlatform(self);
            gameState.AddSpikeball(self);
            gameState.AddExplosions(self);

            gameState.AdditionnalData = TF.EX.Domain.ServiceCollections.ResolveAPIManager().GetStates();

            return gameState;
        }

        public static void LoadState(this Level level, GameState gameState)
        {
            var hudService = ServiceCollections.ResolveHUDService();
            var arrowService = ServiceCollections.ResolveArrowService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var sessionService = ServiceCollections.ResolveSessionService();
            var rngService = ServiceCollections.ResolveRngService();
            var sfxService = ServiceCollections.ResolveSFXService();
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();
            var inputService = ServiceCollections.ResolveInputService();

            sfxService.UpdateLastRollbackFrame(gameState.Frame);
            sfxService.Load(gameState.SFXs.Select(sfx => new Domain.Models.State.SFX
            {
                Frame = sfx.Frame,
                Name = sfx.Name,
                Pan = sfx.Pan,
                Pitch = sfx.Pitch,
                Volume = sfx.Volume,
                Data = null,
            }));

            //JumpPads load
            var inGamejumpPads = level.GetAll<TowerFall.JumpPad>();

            foreach (var jumpPad in gameState.Entities.JumpPads)
            {
                var jumpPadToLoad = inGamejumpPads.FirstOrDefault(jp =>
                {
                    var dynJp = DynamicData.For(jp);
                    var actualDepth = dynJp.Get<double>("actualDepth");
                    return actualDepth == jumpPad.ActualDepth;
                });

                if (jumpPadToLoad != null)
                {
                    jumpPadToLoad.LoadState(jumpPad);
                }
            }

            //Round levels load
            var dynLevelSystem = DynamicData.For(level.Session.MatchSettings.LevelSystem);
            var roundLevels = gameState.RoundLogic.RoundLevels.Nexts.ToList();
            dynLevelSystem.Set("levels", roundLevels);
            dynLevelSystem.Set("lastLevel", gameState.RoundLogic.RoundLevels.Last);

            //Session load
            var session = new TF.EX.Domain.Models.State.Session
            {
                IsEnding = gameState.Session.IsEnding,
                RoundStarted = gameState.Session.RoundStarted,
                RoundEndCounter = gameState.Session.RoundEndCounter,
                RoundIndex = gameState.Session.RoundIndex,
                IsDone = gameState.Session.IsDone,
                Miasma = new Domain.Models.State.Miasma
                {
                    CoroutineTimer = gameState.Session.Miasma.CoroutineTimer,
                    Counter = gameState.Session.Miasma.Counter,
                    DissipateTimer = gameState.Session.Miasma.DissipateTimer,
                    IsDissipating = gameState.Session.Miasma.IsDissipating,
                    Percent = gameState.Session.Miasma.Percent,
                    SideWeight = gameState.Session.Miasma.SideWeight,
                },
                Scores = gameState.Session.Scores.ToArray(),
                OldScores = gameState.Session.OldScores.ToArray(),
            };

            sessionService.SaveSession(session);

            level.Session.Scores = session.Scores.ToArray();
            level.Session.OldScores = session.OldScores.ToArray();

            if (session.RoundIndex != level.Session.RoundIndex)
            {
                roundLevels.Insert(0, gameState.RoundLogic.RoundLevels.Last);
                dynLevelSystem.Set("levels", roundLevels);

                var dynSession = DynamicData.For(level.Session);
                dynSession.Set("RoundIndex", session.RoundIndex);

                LevelLoaderXML loaderXML = new LevelLoaderXML(level.Session);

                var dynEngine = DynamicData.For(TFGame.Instance);
                dynEngine.Set("scene", loaderXML);

                while (!loaderXML.Finished)
                {
                    loaderXML.Update();
                }

                dynEngine.Set("scene", dynEngine.Get<Monocle.Scene>("nextScene"));
                level = loaderXML.Level;

                if (level.Session.RoundIndex != 0)
                {
                    foreach (var chest in gameState.Entities.Chests)
                    {
                        var dummyChest = new TreasureChest(Vector2.Zero, TreasureChest.Types.AutoOpen, TreasureChest.AppearModes.Time, TowerFall.Pickups.Arrows);

                        var dynChest = DynamicData.For(dummyChest);
                        dynChest.Set("Scene", level);
                        dummyChest.Added();

                        level.GetGameplayLayer().Entities.Insert(0, dummyChest);
                    }
                }
            }

            var dynGameplayLayer = DynamicData.For(level.GetGameplayLayer());
            dynGameplayLayer.Set("actualDepthLookup", gameState.Layer.GameplayLayerActualDepthLookup);

            var dynScene = DynamicData.For(level as Monocle.Scene);
            dynScene.Set("FrameCounter", gameState.Frame);
            level.EndScreenShake();

            level.Frozen = gameState.IsLevelFrozen;

            var dynRoundLogic = DynamicData.For(level.Session.RoundLogic);
            dynRoundLogic.Set("RoundStarted", session.RoundStarted);
            dynRoundLogic.Set("done", session.IsDone);
            dynRoundLogic.Set("Time", gameState.RoundLogic.Time);

            if (currentMode.IsNetplay() || netplayManager.IsTestMode())
            {
                var endCounter = dynRoundLogic.Get<RoundEndCounter>("roundEndCounter");
                DynamicData.For(endCounter).Set("endCounter", session.RoundEndCounter);
            }

            level.Session.CurrentLevel.Ending = session.IsEnding;

            var dynCounter = dynRoundLogic.Get<Counter>("miasmaCounter");
            var dynamicMiasmaCounter = DynamicData.For(dynCounter);
            dynamicMiasmaCounter.Set("counter", gameState.Session.Miasma.Counter);

            level.Delete<TowerFall.Miasma>();

            var sess = sessionService.GetSession();

            if (gameState.Session.Miasma.IsDissipating)
            {
                var miasma = level.AddMiasmaToGameplayLayer(gameState.Session.Miasma.ActualDepth);

                miasma.Percent = gameState.Session.Miasma.Percent;
                miasma.SideWeight = gameState.Session.Miasma.SideWeight;
                sess.Miasma.DissipateTimer = 0;

                miasma.Dissipate();

                for (int i = 0; i < gameState.Session.Miasma.DissipateTimer; i++)
                {
                    miasma.Update();
                }
            }
            else
            {
                if (gameState.Session.Miasma.CoroutineTimer > 0)
                {
                    sess.Miasma.CoroutineTimer = 0;

                    var miasma = level.AddMiasmaToGameplayLayer(gameState.Session.Miasma.ActualDepth);

                    for (int i = 0; i < gameState.Session.Miasma.CoroutineTimer; i++)
                    {
                        miasma.Update();
                    }
                }
            }

            //Event logs
            dynRoundLogic.Set("Events", gameState.RoundLogic.EventLogs.ToTFModel());

            //HUD (VersusRoundResults && VersusStart)
            hudService.Update(new Domain.Models.State.Entity.HUD.HUD());

            //VersusRoundResults
            level.Delete<VersusRoundResults>();
            level.Delete<HUDFade>();

            if (gameState.Entities.Hud.VersusRoundResults.CoroutineState > 0)
            {
                var hudFade = new TowerFall.HUDFade();
                var versusRoundResults = new TowerFall.VersusRoundResults(level.Session, gameState.RoundLogic.EventLogs.ToTFModel());
                var dynVersusRoundResults = DynamicData.For(versusRoundResults);
                dynVersusRoundResults.Set("Scene", level);
                dynVersusRoundResults.Set("Level", level);

                var hudLayer = level.Layers.FirstOrDefault(l => l.Value.Index == versusRoundResults.LayerIndex).Value;
                hudLayer.Entities.Add(versusRoundResults);
                hudLayer.Entities.Add(hudFade);
                versusRoundResults.Added();
                hudFade.Added();

                if (level.Session.GetWinner() != -1)
                {
                    var vsMatchResults = new VersusMatchResults(level.Session, versusRoundResults);

                    var dynVsMatchResults = DynamicData.For(vsMatchResults);

                    dynVsMatchResults.Set("Scene", level);
                    dynVsMatchResults.Set("Level", level);
                    level.Layers.FirstOrDefault(l => l.Value.Index == vsMatchResults.LayerIndex).Value.Entities.Add(vsMatchResults);
                    vsMatchResults.Added();

                    versusRoundResults.MatchResults = vsMatchResults;
                }

                for (int i = 0; i < gameState.Entities.Hud.VersusRoundResults.CoroutineState; i++)
                {
                    versusRoundResults.Update();
                    hudFade.Update();
                }
            }

            //VersusStart
            level.Delete<VersusStart>();

            //This is to remove variant sequence entity
            level.DeleteAllByDepth(-10001);

            if (gameState.Entities.Hud.VersusStart.CoroutineState > 0)
            {
                var versusStart = new TowerFall.VersusStart(level.Session);
                var dynVersusStart = DynamicData.For(versusStart);
                dynVersusStart.Set("Scene", level);
                dynVersusStart.Set("Level", level);

                level.Layers.FirstOrDefault(l => l.Value.Index == versusStart.LayerIndex).Value.Entities.Add(versusStart);
                versusStart.Added();

                for (int i = 0; i < gameState.Entities.Hud.VersusStart.CoroutineState; i++)
                {
                    versusStart.Update();
                }
            }

            hudService.Update(gameState.Entities.Hud);

            //Players
            foreach (Domain.Models.State.Entity.LevelEntity.Player.Player toLoad in gameState.Entities.Players.ToArray())
            {
                var gamePlayer = level.GetPlayer(toLoad.Index);
                if (gamePlayer != null)
                {
                    gamePlayer.LoadState(toLoad);
                }
                else
                {
                    TowerFall.Player player =
                    new TowerFall.Player(
                        toLoad.Index,
                        toLoad.Position.ToTFVector(),
                        Allegiance.Neutral,
                        Allegiance.Neutral,
                        level.Session.GetPlayerInventory(toLoad.Index),
                        level.Session.GetSpawnHatState(toLoad.Index),
                        frozen: false,
                        flash: false,
                    indicator: false);

                    player.LoadState(toLoad);
                    level.GetGameplayLayer().Entities.Insert(0, player);
                }
            }

            //PlayerCorpses
            level.DeleteAll<TowerFall.PlayerCorpse>();
            var corpsesToLoad = gameState.Entities.PlayerCorpses.ToArray();

            foreach (Domain.Models.State.Entity.LevelEntity.Player.PlayerCorpse toLoad in corpsesToLoad)
            {
                var cachedPlayerCorpse = ServiceCollections.GetCachedEntity<TowerFall.PlayerCorpse>(toLoad.ActualDepth);

                cachedPlayerCorpse.LoadState(toLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedPlayerCorpse);
            }

            //RoundLogic Spotlights
            dynRoundLogic.Set("wasFinalKill", gameState.RoundLogic.WasFinalKill);
            var dynLightingLayer = DynamicData.For(level.LightingLayer);
            dynLightingLayer.Set("spotlight", null);
            List<LevelEntity> spotlight = new List<LevelEntity>();
            foreach (var spotlightDepth in gameState.RoundLogic.SpotlightDephts)
            {
                var entity = level.GetEntityByDepth(spotlightDepth);
                if (entity != null)
                {
                    spotlight.Add(entity as LevelEntity);
                }
            }

            if (spotlight.Count > 0)
            {
                dynLightingLayer.Set("spotlight", spotlight.ToArray());
            }

            //Arrows
            if (level[GameTags.Arrow] != null)
            {
                level.DeleteAll<TowerFall.Arrow>();

                foreach (Domain.Models.State.Entity.LevelEntity.Arrows.Arrow toLoad in gameState.Entities.Arrows.ToArray())
                {
                    TowerFall.LevelEntity entityHavingArrow = level.GetPlayerOrCorpse(toLoad.PlayerIndex);
                    if (entityHavingArrow == null)
                    {
                        throw new InvalidOperationException("Can't find the original player that shoot the current arrow being loaded");
                    }

                    var arrow = TowerFall.Arrow.Create(toLoad.ArrowType.ToTFModel(), entityHavingArrow, toLoad.Position.ToTFVector(), toLoad.Direction);
                    var dynArrow = DynamicData.For(arrow);
                    dynArrow.Set("StuckTo", arrowService.GetPlatformStuck(toLoad.Position));
                    arrow.LoadState(toLoad);

                    level.GetGameplayLayer().Entities.Insert(0, arrow);
                }
            }


            //Chests load
            if (level != null && level[GameTags.TreasureChest] != null && level[GameTags.TreasureChest].Count > 0)
            {
                level.DeleteAll<TowerFall.TreasureChest>();

                foreach (var chestToLoad in gameState.Entities.Chests.ToArray())
                {
                    var cachedChest = ServiceCollections.GetCachedEntity<TreasureChest>(chestToLoad.ActualDepth);

                    cachedChest.LoadState(chestToLoad);

                    level.GetGameplayLayer().Entities.Insert(0, cachedChest);
                }
            }

            //Pickups load
            level.DeleteAll<TowerFall.Pickup>();

            foreach (var pickupToLoad in gameState.Entities.Pickups.ToArray())
            {
                var cachedPickup = ServiceCollections.GetCachedEntity<TowerFall.Pickup>(pickupToLoad.ActualDepth);

                if (cachedPickup == null)
                {
                    cachedPickup = TowerFall.Pickup.CreatePickup(pickupToLoad.Position.ToTFVector(), pickupToLoad.TargetPosition.ToTFVector(), pickupToLoad.Type.ToTFModel(), pickupToLoad.PlayerIndex);
                }

                cachedPickup.LoadState(pickupToLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedPickup);
            }

            TFGame.Instance.Screen.Offset = gameState.ScreenOffset.ToTFVector();

            //Orbs load
            var orb = gameState.Entities.OrbLogic;
            level.OrbLogic.LoadState(orb);

            //Lantern load
            foreach (Domain.Models.State.Entity.LevelEntity.Lantern toLoad in gameState.Entities.Lanterns.ToArray())
            {
                var gameLantern = level.GetEntityByDepth(toLoad.ActualDepth) as TowerFall.Lantern;

                if (gameLantern != null)
                {
                    gameLantern.LoadState(toLoad);
                }
            }

            //Orbload
            gameState.LoadOrb(level);

            //Chain load
            foreach (Domain.Models.State.Entity.LevelEntity.Chain toLoad in gameState.Entities.Chains.ToArray())
            {
                var gameChain = level.GetEntityByDepth(toLoad.ActualDepth) as TowerFall.Chain;

                if (gameChain != null)
                {
                    gameChain.LoadState(toLoad);
                }
            }

            //Lava load
            gameState.LoadLavaControl(level);

            //CrackedPlatform load
            gameState.LoadCrackedPlatform(level);

            //SpikeBall load
            gameState.LoadSpikeBall(level);

            //Explosion load
            gameState.LoadExplosions(level);

            //BGTorches load
            gameState.LoadBGTorches(level);

            //Background load
            foreach (BackgroundElement toLoad in gameState.Layer.BackgroundElements.ToArray())
            {
                var gameBackground = level.GetBGElementByIndex(toLoad.index);

                if (gameBackground != null && gameBackground is Background.ScrollLayer)
                {
                    (gameBackground as Background.ScrollLayer).Image.Position = toLoad.Position.ToTFVector();
                }
            }

            //Foreground load
            foreach (ForegroundElement toLoad in gameState.Layer.ForegroundElements.ToArray())
            {
                var gameForeground = level.GetFGElementByIndex(toLoad.index);

                if (gameForeground != null && gameForeground is Background.WavyLayer)
                {
                    var dynForegroundElement = DynamicData.For(gameForeground);
                    dynForegroundElement.Set("counter", toLoad.counter);
                }
            }

            var sine = dynLightingLayer.Get<SineWave>("sine");
            sine.UpdateAttributes(gameState.Layer.LightingLayerSine);

            //Rng
            var rng = gameState.Rng;
            rng.ResetRandom(ref Monocle.Calc.Random);
            rngService.UpdateState(rng.Gen_type);

            var matchStats = gameState.MatchStats.ToArray();

            if (netplayManager.IsTestMode())
            {
                level.Session.MatchStats[0] = matchStats[0].ToTF();
                level.Session.MatchStats[1] = matchStats[1].ToTF();
            }
            else
            {
                level.Session.MatchStats[0] = matchStats[inputService.GetLocalPlayerInputIndex()].ToTF();
                level.Session.MatchStats[1] = matchStats[inputService.GetRemotePlayerInputIndex()].ToTF();
            }

            level.PostLoad(gameState);

            TF.EX.Domain.ServiceCollections.ResolveAPIManager().LoadStates(gameState.AdditionnalData);

            level.SortGamePlayLayer(CompareDepth);
            level.SortHUDLayer(CompareDepth);
        }

        private static int CompareDepth(Monocle.Entity a, Monocle.Entity b)
        {
            var aDepth = DynamicData.For(a).Get<double>("actualDepth");
            var bDepth = DynamicData.For(b).Get<double>("actualDepth");

            return Math.Sign(bDepth - aDepth);
        }

        /// <summary>
        /// Stuff to do after the state is loaded
        /// <para>From now, this is a hack about loading properly player death arrow and arrow cannot hit target</para>
        /// <para>Player need arrow loaded to set death arrow</para>
        /// <para>Arrow need player loaded to set cannot hit target</para>
        /// <para>the hack is to let them load first normally and them finish the remaining piece (Death arrow + CannotHit + PlayerCorpse)</para>
        /// </summary>
        private static void PostLoad(this Level self, GameState gs)
        {
            foreach (Domain.Models.State.Entity.LevelEntity.Player.Player toLoad in gs.Entities.Players.ToArray())
            {
                var gamePlayer = self.GetPlayer(toLoad.Index);

                gamePlayer.LoadDeathArrow(toLoad.DeathArrowDepth);
            }

            foreach (Domain.Models.State.Entity.LevelEntity.Arrows.Arrow toLoad in gs.Entities.Arrows.ToArray())
            {
                var arrow = self.GetEntityByDepth(toLoad.ActualDepth) as TowerFall.Arrow;
                if (arrow != null)
                {
                    arrow.LoadCannotHit(toLoad.HasUnhittableEntity, toLoad.PlayerIndex);
                }
            }

            foreach (var playerCorpse in gs.Entities.PlayerCorpses.ToArray())
            {
                var gamePlayerCorpse = self.GetEntityByDepth(playerCorpse.ActualDepth) as TowerFall.PlayerCorpse;
                gamePlayerCorpse.LoadArrowCushion(playerCorpse);
            }
        }

        private static Background.BGElement GetBGElementByIndex(this Level level, int index) { return level.Background.GetBGElements()[index]; }

        private static Background.BGElement GetFGElementByIndex(this Level level, int index) { return level.Foreground.GetBGElements()[index]; }


        public static Monocle.Entity GetEntityByDepth(this Level self, double actualDepth)
        {
            Monocle.Entity entity = null;
            foreach (Monocle.Entity ent in self.GetGameplayLayer().Entities.ToArray())
            {
                var dynEnt = DynamicData.For(ent);
                var entActualDepth = dynEnt.Get<double>("actualDepth");
                if (entActualDepth == actualDepth)
                {
                    entity = ent;
                    break;
                }
            }

            return entity;
        }

        private static void AddJumpPadsState(this GameState state, Level level)
        {
            var jumpPads = level.GetAll<TowerFall.JumpPad>();
            foreach (var jumpPad in jumpPads)
            {
                state.Entities.JumpPads.Add(jumpPad.GetState());
            }
        }

        private static void AddRoundLogicState(this GameState gameState, Level level)
        {
            var dynLevelSystem = DynamicData.For(level.Session.MatchSettings.LevelSystem);
            var levels = dynLevelSystem.Get<List<string>>("levels");
            var lastLevel = dynLevelSystem.Get<string>("lastLevel");

            gameState.RoundLogic.RoundLevels.Nexts = levels.ToList();
            gameState.RoundLogic.RoundLevels.Last = lastLevel;

            gameState.IsLevelFrozen = level.Frozen;

            var dynRoundLogic = DynamicData.For(level.Session.RoundLogic);
            gameState.RoundLogic.WasFinalKill = dynRoundLogic.Get<bool>("wasFinalKill");
            gameState.RoundLogic.Time = level.Session.RoundLogic.Time;

            var dynLightingLayer = DynamicData.For(level.LightingLayer);
            var spotlights = dynLightingLayer.Get<LevelEntity[]>("spotlight");
            if (spotlights != null)
            {
                foreach (var spotlight in spotlights)
                {
                    var dynLevelEntity = DynamicData.For(spotlight);
                    gameState.RoundLogic.SpotlightDephts.Add(dynLevelEntity.Get<double>("actualDepth"));
                }
            }

            //EventLogs
            gameState.RoundLogic.EventLogs = level.Session.RoundLogic.Events.ToModel();
        }

        private static void AddPlayersState(this GameState gameState, Level level)
        {
            if (level[GameTags.Player] != null)
            {
                var players = level[GameTags.Player];
                players.Sort(CompareDepth); //making sure we use the same order for the game state

                foreach (TowerFall.Player player in players)
                {
                    if (!player.Dead)
                    {
                        gameState.Entities.Players.Add(player.GetState());
                    }
                }
            }
        }

        private static void AddPlayersCorpseState(this GameState gameState, Level level)
        {
            if (level[GameTags.Corpse] != null && level[GameTags.Corpse].Count > 0)
            {
                var gamePlayerCorpses = level[GameTags.Corpse];
                foreach (TowerFall.PlayerCorpse playerCorpse in gamePlayerCorpses)
                {
                    var pc = playerCorpse.GetState();
                    gameState.Entities.PlayerCorpses.Add(pc);
                    ServiceCollections.AddEntityToCache(pc.ActualDepth, playerCorpse);
                }
            }
        }

        private static void AddArrowState(this GameState gameState, Level level)
        {
            var arrowService = ServiceCollections.ResolveArrowService();

            if (level[GameTags.Arrow] != null && level[GameTags.Arrow].Count > 0)
            {
                var arrows = level[GameTags.Arrow].ToArray();

                foreach (TowerFall.Arrow arrow in arrows)
                {
                    var dynArrow = DynamicData.For(arrow);
                    dynArrow.Set("counter", Vector2.Zero);//For some weird reason, the counter is not reseted an ctor, so we do it here
                                                          //Oh i think i found why, it's might be du to the arrow cache 

                    if (arrow.StuckTo != null && arrow.State == TowerFall.Arrow.ArrowStates.Stuck)
                    {
                        arrowService.AddStuckArrow(arrow.Position.ToModel(), arrow.StuckTo);
                    }
                    gameState.Entities.Arrows.Add(arrow.GetState());
                }
            }
        }

        private static void AddChestsState(this GameState gameState, Level level)
        {
            if (level[GameTags.TreasureChest] != null && level[GameTags.TreasureChest].Count > 0)
            {
                var chests = level[GameTags.TreasureChest].ToArray();

                foreach (TowerFall.TreasureChest chest in chests)
                {
                    var che = chest.GetState();
                    gameState.Entities.Chests.Add(che);
                    ServiceCollections.AddEntityToCache(che.ActualDepth, chest);
                }
            }
        }

        private static void AddPickupsState(this GameState gameState, Level level)
        {
            var gamePickups = level.GetAll<TowerFall.Pickup>().ToArray();
            if (gamePickups != null && gamePickups.Length > 0)
            {
                foreach (TowerFall.Pickup pickup in gamePickups)
                {
                    var pick = pickup.GetState();
                    gameState.Entities.Pickups.Add(pick);
                    ServiceCollections.AddEntityToCache(pick.ActualDepth, pickup);
                }
            }
        }

        private static void AddSessionState(this GameState gameState, Level level)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var sessionService = ServiceCollections.ResolveSessionService();

            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            var dynRoundLogic = DynamicData.For(level.Session.RoundLogic);

            if (currentMode.IsNetplay() || netplayManager.IsTestMode())
            {
                var roundEndCounter = dynRoundLogic.Get<RoundEndCounter>("roundEndCounter");
                var endCounter = DynamicData.For(roundEndCounter).Get<float>("endCounter");
                var roundStarted = level.Session.RoundLogic.RoundStarted;
                var done = dynRoundLogic.Get<bool>("done");
                var roundIndex = level.Session.RoundIndex;

                var isEnding = level.Session.CurrentLevel.Ending;

                var miasmaCounter = dynRoundLogic.Get<Counter>("miasmaCounter");
                var counter = DynamicData.For(miasmaCounter).Get<float>("counter");

                var stateSession = sessionService.GetSession();
                stateSession.RoundStarted = roundStarted;
                stateSession.RoundEndCounter = endCounter;
                stateSession.IsEnding = isEnding;
                stateSession.Miasma.Counter = counter;
                stateSession.IsDone = done;
                stateSession.RoundIndex = roundIndex;
                var session = new Domain.Models.State.Session
                {
                    IsEnding = stateSession.IsEnding,
                    RoundEndCounter = stateSession.RoundEndCounter,
                    IsDone = stateSession.IsDone,
                    RoundIndex = stateSession.RoundIndex,
                    RoundStarted = stateSession.RoundStarted,
                    Miasma = new Domain.Models.State.Miasma
                    {
                        Counter = stateSession.Miasma.Counter,
                        CoroutineTimer = stateSession.Miasma.CoroutineTimer,
                        DissipateTimer = stateSession.Miasma.DissipateTimer,
                        IsDissipating = stateSession.Miasma.IsDissipating,
                        Percent = stateSession.Miasma.Percent,
                        SideWeight = stateSession.Miasma.SideWeight,
                    },
                    Scores = level.Session.Scores.ToArray(),
                    OldScores = level.Session.OldScores.ToArray(),
                };

                sessionService.SaveSession(stateSession);
                gameState.Session = session;
            }
        }

        private static void AddOrbState(this GameState gameState, Level level)
        {
            var orb = level.OrbLogic.GetState();
            gameState.Entities.OrbLogic = orb;
        }

        private static void AddLanternState(this GameState gameState, Level level)
        {
            var gameLanterns = level.GetAll<TowerFall.Lantern>().ToArray();
            if (gameLanterns != null && gameLanterns.Length > 0)
            {
                foreach (TowerFall.Lantern lantern in gameLanterns)
                {
                    var lan = lantern.GetState();
                    gameState.Entities.Lanterns.Add(lan);
                }
            }
        }

        private static void AddChainState(this GameState gameState, Level level)
        {
            if (level[GameTags.Chain] != null && level[GameTags.Chain].Count > 0)
            {
                var gameChains = level[GameTags.Chain].ToArray();
                foreach (TowerFall.Chain chain in gameChains)
                {
                    var ch = chain.GetState();
                    gameState.Entities.Chains.Add(ch);
                }
            }
        }

        private static void AddOrbsState(this GameState gameState, Level level)
        {
            var gameOrbs = level.GetAll<TowerFall.Orb>().ToArray();
            if (gameOrbs != null && gameOrbs.Length > 0)
            {
                foreach (TowerFall.Orb orb in gameOrbs)
                {
                    var orbState = orb.GetState();
                    gameState.Entities.Orbs.Add(orbState);
                }
            }
        }

        private static void AddLavaControlState(this GameState gameState, Level level)
        {
            var gameLavaControl = level.Get<LavaControl>();
            if (gameLavaControl != null)
            {
                gameState.Entities.LavaControl = gameLavaControl.GetState();
            }

        }

        private static void AddBGTorches(this GameState gameState, Level level)
        {
            var bgTorches = level.GetAll<TowerFall.BGTorch>().ToArray();

            foreach (TowerFall.BGTorch torch in bgTorches)
            {
                var torchState = torch.GetState();
                gameState.Entities.BGTorches.Add(torchState);
            }
        }

        private static void AddLayerState(this GameState gameState, Level level)
        {
            var sessionService = ServiceCollections.ResolveSessionService();

            //Background save
            var bgElements = level.Background.GetBGElements().ToArray();
            List<BackgroundElement> bgs = new List<BackgroundElement>();
            for (int i = 0; i < bgElements.Length; i++)
            {
                TowerFall.Background.BGElement bg = bgElements[i];
                if (bg is TowerFall.Background.ScrollLayer)
                {
                    var bgModel = (bg as TowerFall.Background.ScrollLayer).GetState(i);
                    bgs.Add(bgModel);
                }
            }
            gameState.Layer.BackgroundElements = bgs;

            //Foreground save
            if (level.Foreground != null)
            {
                var fgElements = level.Foreground.GetBGElements().ToArray();
                List<ForegroundElement> fgs = new List<ForegroundElement>();
                for (int i = 0; i < fgElements.Length; i++)
                {
                    TowerFall.Background.BGElement fg = fgElements[i];
                    if (fg is TowerFall.Background.WavyLayer)
                    {
                        var fgModel = (fg as TowerFall.Background.WavyLayer).GetState(i);
                        fgs.Add(fgModel);
                    }
                }
                gameState.Layer.ForegroundElements = fgs;
            }

            var dynLightingLayer = DynamicData.For(level.LightingLayer);

            var sineWave = dynLightingLayer.Get<SineWave>("sine");
            gameState.Layer.LightingLayerSine = sineWave.Counter;

            level.DeleteAll<TowerFall.Hat>(); //TODO: Remove and save hats

            var dynGameplayLayer = DynamicData.For(level.GetGameplayLayer());
            var actualDepthLookup = dynGameplayLayer.Get<Dictionary<int, double>>("actualDepthLookup");
            actualDepthLookup.Remove(-52);

            actualDepthLookup.Remove(-600); //TODO: Miasma

            sessionService.SaveGamePlayLayerActualDepthLookup(actualDepthLookup);
            gameState.Layer.GameplayLayerActualDepthLookup = actualDepthLookup;
        }

        public static void AddCrackedPlatform(this GameState gameState, TowerFall.Level level)
        {
            var crackedPlatforms = level.GetAll<TowerFall.CrackedPlatform>().ToArray();
            if (crackedPlatforms != null && crackedPlatforms.Length > 0)
            {
                foreach (TowerFall.CrackedPlatform orb in crackedPlatforms)
                {
                    var crackedPlatState = orb.GetState();
                    gameState.Entities.CrackedPlatforms.Add(crackedPlatState);
                }
            }
        }

        public static void AddSpikeball(this GameState gameState, TowerFall.Level self)
        {
            var spikeball = self.Get<Spikeball>();

            if (spikeball != null)
            {
                gameState.Entities.Spikeball = spikeball.GetState();
            }
        }

        private static void AddExplosions(this GameState game, Level level)
        {
            var explosions = level.GetAll<TowerFall.Explosion>().ToArray();
            if (explosions != null && explosions.Length > 0)
            {
                foreach (TowerFall.Explosion explosion in explosions)
                {
                    var exp = explosion.GetState();
                    game.Entities.Explosions.Add(exp);
                    ServiceCollections.AddEntityToCache(exp.ActualDepth, explosion);
                }
            }
        }

        private static void LoadLavaControl(this GameState gameState, Level level)
        {
            var gameLavaControl = level.Get<LavaControl>();
            var lavaControl = gameState.Entities.LavaControl;
            var dynOrbLogic = DynamicData.For(level.OrbLogic);

            if (lavaControl == null && gameLavaControl != null)
            {

                var lavas = level.GetAll<TowerFall.Lava>();

                foreach (var lava in lavas.ToList())
                {
                    level.GetGameplayLayer().Entities.Remove(lava);
                    lava.Removed();
                }

                level.GetGameplayLayer().Entities.Remove(gameLavaControl);
                gameLavaControl.Removed();

                dynOrbLogic.Set("control", null);
            }

            if (lavaControl != null)
            {
                if (gameLavaControl != null)
                {
                    gameLavaControl.LoadState(lavaControl);

                    var inGameLavaControl = dynOrbLogic.Get<LavaControl>("control");
                    if (inGameLavaControl == null)
                    {
                        dynOrbLogic.Set("control", gameLavaControl);
                    }
                }
                else
                {
                    var lavaC = new LavaControl((TowerFall.LavaControl.LavaMode)lavaControl.Mode, lavaControl.OwnerIndex);
                    var dynLavaC = DynamicData.For(lavaC);
                    var lavas = dynLavaC.Get<TowerFall.Lava[]>("lavas");
                    lavas = new TowerFall.Lava[lavaControl.Lavas.Count()];

                    int ind = 0;
                    foreach (var lava in lavaControl.Lavas)
                    {
                        var lavaToAdd = new TowerFall.Lava(lavaC, (TowerFall.Lava.LavaSide)lava.Side);
                        var dynLavaToAdd = DynamicData.For(lavaToAdd);
                        dynLavaToAdd.Set("Scene", level);

                        level.GetGameplayLayer().Entities.Add(lavaToAdd);
                        lavaToAdd.Added();

                        lavas[ind] = lavaToAdd;
                        ind++;
                    }
                    dynLavaC.Set("lavas", lavas);

                    lavaC.LoadState(lavaControl);
                    dynOrbLogic.Set("control", gameLavaControl);
                }
            }
        }

        private static void LoadOrb(this GameState gameState, Level level)
        {
            foreach (Domain.Models.State.Entity.LevelEntity.Orb toLoad in gameState.Entities.Orbs.ToArray())
            {
                var gameOrb = level.GetEntityByDepth(toLoad.ActualDepth) as TowerFall.Orb;

                if (gameOrb == null)
                {
                    var orb = new TowerFall.Orb(toLoad.Position.ToTFVector(), false);
                    level.Spawn(orb, toLoad.ActualDepth);
                    gameOrb = orb;
                }

                gameOrb.LoadState(toLoad);
            }
        }

        public static void LoadCrackedPlatform(this GameState gameState, TowerFall.Level level)
        {
            var gameCrackedPlatforms = level.GetAll<TowerFall.CrackedPlatform>().ToArray();
            if (gameCrackedPlatforms != null && gameCrackedPlatforms.Length > 0)
            {
                foreach (TowerFall.CrackedPlatform crackedPlatform in gameCrackedPlatforms)
                {
                    var dynCrackedPlatform = DynamicData.For(crackedPlatform);
                    var actualDepth = dynCrackedPlatform.Get<double>("actualDepth");

                    var currentCrackedPlatform = gameState.Entities.CrackedPlatforms.FirstOrDefault(cp => cp.ActualDepth == actualDepth);
                    if (currentCrackedPlatform != null)
                    {
                        crackedPlatform.LoadState(currentCrackedPlatform);
                    }
                }
            }
        }

        public static void LoadSpikeBall(this GameState gameState, TowerFall.Level level)
        {
            var spikeball = level.Get<Spikeball>();

            if (spikeball != null)
            {
                var spikeballState = gameState.Entities.Spikeball;
                spikeball.LoadState(spikeballState);
            }
        }

        public static void LoadExplosions(this GameState gameState, TowerFall.Level level)
        {
            level.DeleteAll<TowerFall.Explosion>();

            var explosionsToLoad = gameState.Entities.Explosions.ToArray();

            foreach (var toLoad in explosionsToLoad)
            {
                var cachedExplosion = ServiceCollections.GetCachedEntity<TowerFall.Explosion>(toLoad.ActualDepth);

                cachedExplosion.LoadState(toLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedExplosion);
            }
        }

        private static void LoadBGTorches(this GameState gameState, Level level)
        {
            var gameBGTorches = level.GetAll<TowerFall.BGTorch>().ToArray();
            if (gameBGTorches != null && gameBGTorches.Length > 0)
            {
                foreach (TowerFall.BGTorch torch in gameBGTorches)
                {
                    var dynTorch = DynamicData.For(torch);
                    var actualDepth = dynTorch.Get<double>("actualDepth");

                    var currentTorch = gameState.Entities.BGTorches.FirstOrDefault(cp => cp.ActualDepth == actualDepth);
                    if (currentTorch != null)
                    {
                        torch.LoadState(currentTorch);
                    }
                }
            }
        }

        private static TowerFall.Miasma AddMiasmaToGameplayLayer(this TowerFall.Level level, double actualDepth)
        {
            var miasma = new TowerFall.Miasma(TowerFall.Miasma.Modes.Versus);
            var dynMiasma = DynamicData.For(miasma);
            dynMiasma.Set("Scene", level);
            dynMiasma.Set("Level", level);
            dynMiasma.Set("actualDepth", actualDepth);

            level.GetGameplayLayer().Entities.Add(miasma);
            miasma.Added();

            var dynamicRounlogic = DynamicData.For(level.Session.RoundLogic);
            dynamicRounlogic.Set("miasma", miasma);

            return miasma;
        }
    }
}
