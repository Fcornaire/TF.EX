using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Arrows;
using TF.EX.Domain.Models.State.Layer;
using TF.EX.Domain.Models.State.LevelEntity.Chest;
using TF.EX.TowerFallExtensions.Entity.LevelEntity;
using TF.EX.TowerFallExtensions.Layer;
using TowerFall;

namespace TF.EX.TowerFallExtensions
{
    public static class LevelExtensions
    {
        public static bool IsLocalPlayerFrozen(this Level level)
        {
            if (level == null)
            {
                return false;
            }

            var players = level[GameTags.Player];
            if (players != null && players.Count > 0)
            {
                var localPlayer = (TowerFall.Player)level[GameTags.Player][0];

                if (localPlayer != null && localPlayer.State.Equals(TowerFall.Player.PlayerStates.Frozen))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This extension method removes sounds if the entity related to the sound is removed.
        /// </summary>
        /// <param name="scene"></param>
        public static void AdjustSFX(this Level level) //TODO: find a way fast fordward the sound in a rollback frame (if possible)
        {
            if (level.Get<VersusStart>() == null)
            {
                Sounds.sfx_multiStartLevel.Stop();
            }
        }

        public static void Delete<T>(this Level level) where T : Monocle.Entity
        {
            var entity = level.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is T) as T;

            if (entity != null)
            {
                level.Layers.FirstOrDefault(layer => layer.Value.Index == entity.LayerIndex).Value.Entities.Remove(entity);
                entity.Removed();

                if (entity is VersusStart)
                {
                    Sounds.sfx_multiStartLevel.Stop();
                    Sounds.sfx_trainingStartLevelStone.Stop();
                    Sounds.sfx_trainingStartLevelOut.Stop();
                }
            }
        }

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

        public static T Get<T>(this Level level) where T : Monocle.Entity
        {
            return level.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is T) as T;
        }

        public static IEnumerable<T> GetAll<T>(this Level level) where T : Monocle.Entity
        {
            return level.Layers.SelectMany(layer => layer.Value.Entities)
                .Where(ent => ent is T).Select(ent => ent as T);
        }


        public static void SortGamePlayLayer(this Level level, Comparison<Monocle.Entity> comparison)
        {
            level.GetGameplayLayer().Entities.Sort(comparison);
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

        public static GameState GetState(this Level entity)
        {
            var gameState = new GameState();
            var hudService = ServiceCollections.ResolveHUDService();
            var arrowService = ServiceCollections.ResolveArrowService();
            (_, var currentMode) = ServiceCollections.ResolveStateMachineService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var sessionService = ServiceCollections.ResolveSessionService();
            var orbService = ServiceCollections.ResolveOrbService();
            var rngService = ServiceCollections.ResolveRngService();

            //Levels
            var dynLevelSystem = DynamicData.For(entity.Session.MatchSettings.LevelSystem);
            var levels = dynLevelSystem.Get<List<string>>("levels");
            var lastLevel = dynLevelSystem.Get<string>("lastLevel");
            if (levels.Count > 0 && levels[0].Contains("00.oel"))
            {
                levels.RemoveAt(0);
            }

            gameState.RoundLevels.Nexts = levels.ToList();
            gameState.RoundLevels.Last = lastLevel;

            gameState.IsLevelFrozen = entity.Frozen;

            var dynRoundLogic = DynamicData.For(entity.Session.RoundLogic);
            gameState.RoundLogic.WasFinalKill = dynRoundLogic.Get<bool>("wasFinalKill");
            var dynLightingLayer = DynamicData.For(entity.LightingLayer);
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
            gameState.EventLogs = entity.Session.RoundLogic.Events.ToModel();

            //Versus Start
            //VersusRoundResults
            gameState.Hud = hudService.Get();

            //Players save state
            var playersState = new List<Domain.Models.State.Player.Player>();
            if (entity[GameTags.Player] != null)
            {
                var players = entity[GameTags.Player];
                players.Sort(CompareDepth); //making sure we use the same order for the game state

                foreach (TowerFall.Player player in players)
                {
                    playersState.Add(player.GetState());
                }
            }
            gameState.Players = playersState;

            //PlayerCorpse save
            var playerCorpsesState = new List<Domain.Models.State.Player.PlayerCorpse>();
            if (entity[GameTags.Corpse] != null && entity[GameTags.Corpse].Count > 0)
            {
                var gamePlayerCorpses = entity[GameTags.Corpse];
                foreach (TowerFall.PlayerCorpse playerCorpse in gamePlayerCorpses)
                {
                    var pc = playerCorpse.GetState();
                    playerCorpsesState.Add(pc);
                    ServiceCollections.AddToCache(pc.ActualDepth, playerCorpse);
                }
            }
            gameState.PlayerCorpses = playerCorpsesState;

            //Arrow save state
            var arrowsState = new List<TF.EX.Domain.Models.State.Arrows.Arrow>();
            if (entity[GameTags.Arrow] != null && entity[GameTags.Arrow].Count > 0)
            {
                var arrows = entity[GameTags.Arrow].ToArray();

                foreach (TowerFall.Arrow arrow in arrows)
                {
                    var dynArrow = DynamicData.For(arrow);
                    dynArrow.Set("counter", Vector2.Zero);//For some weird reason, the counter is not reseted an ctor, so we do it here
                                                          //Oh i think i found why, it's might be du to the arrow cache 

                    if (arrow.StuckTo != null && arrow.State == TowerFall.Arrow.ArrowStates.Stuck)
                    {
                        arrowService.AddStuckArrow(arrow.Position.ToModel(), arrow.StuckTo);
                    }
                    arrowsState.Add(arrow.GetState());
                }
            }
            gameState.Arrows = arrowsState;

            //Chests save state
            var chestsState = new List<TF.EX.Domain.Models.State.LevelEntity.Chest.Chest>();
            if (entity[GameTags.TreasureChest] != null && entity[GameTags.TreasureChest].Count > 0)
            {
                var chests = entity[GameTags.TreasureChest].ToArray();

                foreach (TowerFall.TreasureChest chest in chests)
                {
                    var che = chest.GetState();
                    chestsState.Add(che);
                    ServiceCollections.AddToCache(che.ActualDepth, chest);
                }
            }
            gameState.Chests = chestsState;

            //Pickup save state
            var pickupsState = new List<TF.EX.Domain.Models.State.LevelEntity.Chest.Pickup>();
            var gamePickups = entity.GetAll<TowerFall.Pickup>().ToArray();
            if (gamePickups != null && gamePickups.Length > 0)
            {
                foreach (TowerFall.Pickup pickup in gamePickups)
                {
                    var pick = pickup.GetState();
                    pickupsState.Add(pick);
                    ServiceCollections.AddToCache(pick.ActualDepth, pickup);
                }
            }
            gameState.Pickups = pickupsState;

            //Session save
            if (currentMode.IsNetplay() || netplayManager.IsTestMode())
            {
                var roundEndCounter = dynRoundLogic.Get<RoundEndCounter>("roundEndCounter");
                var endCounter = DynamicData.For(roundEndCounter).Get<float>("endCounter");
                var roundStarted = entity.Session.RoundLogic.RoundStarted;
                var done = dynRoundLogic.Get<bool>("done");
                var roundIndex = entity.Session.RoundIndex;

                var isEnding = entity.Session.CurrentLevel.Ending;

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
                        CoroutineTimer = stateSession.Miasma.CoroutineTimer
                    },
                    Scores = entity.Session.Scores.ToArray(),
                    OldScores = entity.Session.OldScores.ToArray(),
                };

                sessionService.SaveSession(stateSession);
                gameState.Session = session;
            }

            //Orbs save
            //var orb = orbService.GetOrb();

            var orb = entity.OrbLogic.GetState();

            //orb.Time.EngineTimeRate = TFGame.TimeRate;
            //orb.Time.EngineTimeMult = TFGame.TimeRate; //In Fixed time step, TimeRate = TimeMult

            //orbService.Save(orb);
            gameState.Orb = orb;

            //Lantern save
            var lanternsState = new List<TF.EX.Domain.Models.State.LevelEntity.Lantern>();
            var gameLanterns = entity.GetAll<TowerFall.Lantern>().ToArray();
            if (gameLanterns != null && gameLanterns.Length > 0)
            {
                foreach (TowerFall.Lantern lantern in gameLanterns)
                {
                    var lan = lantern.GetState();
                    lanternsState.Add(lan);
                }
            }
            gameState.Lanterns = lanternsState;

            //Chain save
            var chainsState = new List<TF.EX.Domain.Models.State.LevelEntity.Chain>();
            if (entity[GameTags.Chain] != null && entity[GameTags.Chain].Count > 0)
            {
                var gameChains = entity[GameTags.Chain].ToArray();
                foreach (TowerFall.Chain chain in gameChains)
                {
                    var ch = chain.GetState();
                    chainsState.Add(ch);
                }
            }
            gameState.Chains = chainsState;

            //Background save
            var bgElements = entity.Background.GetBGElements().ToArray();
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
            var fgElements = entity.Foreground.GetBGElements().ToArray();
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

            entity.DeleteAll<TowerFall.Hat>(); //TODO: Remove and save hats

            var dynGameplayLayer = DynamicData.For(entity.GetGameplayLayer());
            var actualDepthLookup = dynGameplayLayer.Get<Dictionary<int, double>>("actualDepthLookup");
            actualDepthLookup.Remove(-52);

            actualDepthLookup.Remove(-600); //TODO: Miasma

            sessionService.SaveGamePlayLayerActualDepthLookup(actualDepthLookup);
            gameState.GamePlayerLayerActualDepthLookup = actualDepthLookup;

            gameState.Rng = rngService.Get();
            gameState.Frame = (int)entity.FrameCounter;

            return gameState;
        }

        public static void LoadState(this Level level, GameState gameState)
        {
            var hudService = ServiceCollections.ResolveHUDService();
            var arrowService = ServiceCollections.ResolveArrowService();
            (_, var currentMode) = ServiceCollections.ResolveStateMachineService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var sessionService = ServiceCollections.ResolveSessionService();
            var orbService = ServiceCollections.ResolveOrbService();
            var rngService = ServiceCollections.ResolveRngService();

            //Round levels load
            var dynLevelSystem = DynamicData.For(level.Session.MatchSettings.LevelSystem);
            var roundLevels = gameState.RoundLevels.Nexts.ToList();
            dynLevelSystem.Set("levels", roundLevels);
            dynLevelSystem.Set("lastLevel", gameState.RoundLevels.Last);

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
                },
                Scores = gameState.Session.Scores.ToArray(),
                OldScores = gameState.Session.OldScores.ToArray(),
            };

            sessionService.SaveSession(session);

            level.Session.Scores = session.Scores.ToArray();
            level.Session.OldScores = session.OldScores.ToArray();

            if (session.RoundIndex != level.Session.RoundIndex)
            {
                roundLevels.Insert(0, gameState.RoundLevels.Last);
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
                    foreach (var chest in gameState.Chests)
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
            dynGameplayLayer.Set("actualDepthLookup", gameState.GamePlayerLayerActualDepthLookup);

            var dynScene = DynamicData.For(level as Monocle.Scene);
            dynScene.Set("FrameCounter", gameState.Frame);
            level.EndScreenShake();

            level.Frozen = gameState.IsLevelFrozen;

            var dynRoundLogic = DynamicData.For(level.Session.RoundLogic);
            dynRoundLogic.Set("RoundStarted", session.RoundStarted);
            dynRoundLogic.Set("done", session.IsDone);

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

            if (gameState.Session.Miasma.CoroutineTimer > 0)
            {
                var sess = sessionService.GetSession();
                sess.Miasma.CoroutineTimer = 0;
                sessionService.SaveSession(sess);

                var miasma = new TowerFall.Miasma(TowerFall.Miasma.Modes.Versus);
                var dynMiasma = DynamicData.For(miasma);
                dynMiasma.Set("Scene", level);
                dynMiasma.Set("Level", level);
                dynMiasma.Set("actualDepth", gameState.Session.Miasma.ActualDepth);

                level.GetGameplayLayer().Entities.Add(miasma);
                miasma.Added();

                var dynamicRounlogic = DynamicData.For(level.Session.RoundLogic);
                dynamicRounlogic.Set("miasma", miasma);

                for (int i = 0; i < gameState.Session.Miasma.CoroutineTimer; i++)
                {
                    miasma.Update();
                }
            }

            //Event logs
            dynRoundLogic.Set("Events", gameState.EventLogs.ToTFModel());

            hudService.Update(new Domain.Models.State.HUD.HUD());
            //VersusRoundResults
            level.Delete<VersusRoundResults>();
            Sounds.sfx_multiCoinEarned.Stop();
            level.Delete<HUDFade>();

            if (gameState.Hud.VersusRoundResults.CoroutineState > 0)
            {
                var hudFade = new TowerFall.HUDFade();
                var versusRoundResults = new TowerFall.VersusRoundResults(level.Session, gameState.EventLogs.ToTFModel());
                var dynVersusRoundResults = DynamicData.For(versusRoundResults);
                dynVersusRoundResults.Set("Scene", level);
                dynVersusRoundResults.Set("Level", level);

                var hudLayer = level.Layers.FirstOrDefault(l => l.Value.Index == hudFade.LayerIndex).Value;
                hudLayer.Entities.Add(versusRoundResults);
                hudLayer.Entities.Add(hudFade);
                versusRoundResults.Added();
                hudFade.Added();

                for (int i = 0; i < gameState.Hud.VersusRoundResults.CoroutineState; i++)
                {
                    versusRoundResults.Update();
                    hudFade.Update();
                }
            }

            //VersusStart
            level.Delete<VersusStart>();

            if (gameState.Hud.VersusStart.CoroutineState > 0)
            {
                var versusStart = new TowerFall.VersusStart(level.Session);
                var dynVersusStart = DynamicData.For(versusStart);
                dynVersusStart.Set("Scene", level);
                dynVersusStart.Set("Level", level);

                level.Layers.FirstOrDefault(l => l.Value.Index == versusStart.LayerIndex).Value.Entities.Add(versusStart);
                versusStart.Added();

                for (int i = 0; i < gameState.Hud.VersusStart.CoroutineState; i++)
                {
                    versusStart.Update();
                }
            }

            //Players
            foreach (Domain.Models.State.Player.Player toLoad in gameState.Players.ToArray())
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
            var corpsesToLoad = gameState.PlayerCorpses.ToArray();

            foreach (Domain.Models.State.Player.PlayerCorpse toLoad in corpsesToLoad)
            {
                var cachedPlayerCorpse = ServiceCollections.GetCached<TowerFall.PlayerCorpse>(toLoad.ActualDepth);

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

                foreach (TF.EX.Domain.Models.State.Arrows.Arrow toLoad in gameState.Arrows.ToArray())
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

                foreach (var chestToLoad in gameState.Chests.ToArray())
                {
                    var cachedChest = ServiceCollections.GetCached<TreasureChest>(chestToLoad.ActualDepth);

                    cachedChest.LoadState(chestToLoad);

                    level.GetGameplayLayer().Entities.Insert(0, cachedChest);
                }
            }

            //Pickups load
            level.DeleteAll<TowerFall.Pickup>();

            foreach (var pickupToLoad in gameState.Pickups.ToArray())
            {
                var cachedPickup = ServiceCollections.GetCached<TowerFall.Pickup>(pickupToLoad.ActualDepth);

                if (cachedPickup == null)
                {
                    cachedPickup = TowerFall.Pickup.CreatePickup(pickupToLoad.Position.ToTFVector(), pickupToLoad.TargetPosition.ToTFVector(), pickupToLoad.Type.ToTFModel(), pickupToLoad.PlayerIndex);
                }

                cachedPickup.LoadState(pickupToLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedPickup);
            }

            //Orbs load
            var orb = gameState.Orb;
            //orbService.Save(orb);

            //TFGame.TimeRate = orb.Time.EngineTimeRate;
            var lavaControl = level.Get<LavaControl>();
            if (orb.Lava.IsDefault())
            {
                if (lavaControl != null)
                {
                    var lavas = level.GetAll<Lava>();

                    foreach (var lava in lavas)
                    {
                        level.GetGameplayLayer().Entities.Remove(lava);
                        lava.Removed();
                    }

                    level.GetGameplayLayer().Entities.Remove(lavaControl);
                    lavaControl.Removed();
                }
            }
            level.OrbLogic.LoadState(orb);

            //Lantern load
            foreach (TF.EX.Domain.Models.State.LevelEntity.Lantern toLoad in gameState.Lanterns.ToArray())
            {
                var gameLantern = level.GetEntityByDepth(toLoad.ActualDepth) as Lantern;

                if (gameLantern != null)
                {
                    gameLantern.LoadState(toLoad);
                }
            }

            //Chain load
            foreach (TF.EX.Domain.Models.State.LevelEntity.Chain toLoad in gameState.Chains.ToArray())
            {
                var gameChain = level.GetEntityByDepth(toLoad.ActualDepth) as Chain;

                if (gameChain != null)
                {
                    gameChain.LoadState(toLoad);
                }
            }

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

            //Rng
            var rng = gameState.Rng;
            rng.ResetRandom();
            rngService.UpdateState(rng.Gen_type);



            level.PostLoad(gameState);

            level.SortGamePlayLayer(CompareDepth);
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
            foreach (Domain.Models.State.Player.Player toLoad in gs.Players.ToArray())
            {
                var gamePlayer = self.GetPlayer(toLoad.Index);

                gamePlayer.LoadDeathArrow(toLoad.DeathArrowDepth);
            }

            foreach (TF.EX.Domain.Models.State.Arrows.Arrow toLoad in gs.Arrows.ToArray())
            {
                var arrow = self.GetEntityByDepth(toLoad.ActualDepth) as TowerFall.Arrow;
                if (arrow != null)
                {
                    arrow.LoadCannotHit(toLoad.HasUnhittableEntity, toLoad.PlayerIndex);
                }
            }

            foreach (var playerCorpse in gs.PlayerCorpses.ToArray())
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
    }
}
