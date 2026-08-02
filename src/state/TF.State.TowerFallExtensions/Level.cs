using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.Domain.Extensions;
using TF.State.Domain.Logging;
using TF.State.Domain.Models;
using TF.State.Domain.Models.Entity.LevelEntity.Arrows;
using TF.State.Domain.Models.Entity.LevelEntity.Chest;
using TF.State.Domain.Models.Entity.LevelEntity.Player;
using TF.State.Domain.Models.Layer;
using TF.State.TowerFallExtensions.Entity.HUD;
using TF.State.TowerFallExtensions.Entity.LevelEntity;
using TF.State.TowerFallExtensions.Layer;
using TF.State.TowerFallExtensions.Scene;
using TowerFall;
namespace TF.State.TowerFallExtensions
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

        public static void EnsurePlayerIndicators(this Level level)
        {
            foreach (var player in level.GetAll<TowerFall.Player>())
            {
                if (player.Indicator != null)
                {
                    continue;
                }

                var indicator = new PlayerIndicator(Vector2.Zero, player.PlayerIndex, player.HatState == TowerFall.Player.HatStates.Crown);

                DynamicData.For(player).Set("Indicator", indicator);
                player.Add(indicator);
                indicator.OnIntroStart(true);
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
            }
        }

        //TODO: remove and use Scene extension instead
        public static void DeleteAll<T>(this Level level) where T : Monocle.Entity
        {
            foreach (var layer in level.Layers.Values)
            {
                DynamicData.For(layer).Get<List<Monocle.Entity>>("toAdd")?.RemoveAll(entity => entity is T);

                var entities = layer.Entities.Where(entity => entity is T).ToList();

                foreach (var entity in entities)
                {
                    layer.Entities.Remove(entity);
                    entity.Removed();
                }
            }
        }

        public static void RemoveEntity(this Level level, Monocle.Entity entity)
        {
            foreach (var layer in level.Layers.Values)
            {
                layer.Entities.Remove(entity);
                DynamicData.For(layer).Get<List<Monocle.Entity>>("toAdd")?.Remove(entity);
            }

            foreach (var tag in entity.Tags)
            {
                level[tag].Remove(entity);
            }

            entity.Removed();
        }

        public static void DropLiveFromEntityPool<T>(this Level level) where T : Monocle.Entity, new()
        {
            if (Monocle.Cache.cache == null || !Monocle.Cache.cache.TryGetValue(typeof(T), out var pool))
            {
                return;
            }

            var live = level.GetAll<T>().ToList();
            var kept = pool.Where(entity => !live.Contains(entity)).Reverse().ToList();

            pool.Clear();
            kept.ForEach(pool.Push);
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
            var sessionService = ServiceCollections.ResolveSessionService();

            gameState.Frame = StateFlags.CurrentFrame;

            gameState.AddJumpPadsState(self);

            if (!StateFlags.IsTestMode)
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
            gameState.AddMovingPlatformState(self);
            gameState.AddBramblesState(self);
            gameState.AddIciclesState(self);
            gameState.AddSnowClumpsState(self);
            gameState.AddSwitchBlocksState(self);
            gameState.AddSwitchBlockControlState(self);
            gameState.AddShiftBlocksState(self);
            gameState.AddProximityBlocksState(self);
            gameState.AddMoonGlassBlocksState(self);
            gameState.AddGhostPlatformsState(self);
            gameState.AddLoopPlatformsState(self);
            gameState.AddRotatePlatformsState(self);
            gameState.AddSensorBlocksState(self);
            gameState.AddCrumbleBlocksState(self);
            gameState.AddCrumbleWallsState(self);
            gameState.AddPrismsState(self);
            gameState.AddTeamReviversState(self);
            gameState.AddPlayerGhostsState(self);
            gameState.AddQuestSpawnPortalsState(self);
            gameState.AddSlimesState(self);
            gameState.AddBatsState(self);
            gameState.AddEnemyAttacksState(self);
            gameState.AddDarkPortalsSequenceState(self);
            gameState.AddDummiesState(self);
            gameState.AddDummyHeadsState(self);
            gameState.AddTrialsControlState(self);

            gameState.Session.BramblesStartingState = sessionService.GetBramblesStartingState();
            gameState.Rng = rngService.Get();

            gameState.MatchStats = self.Session.MatchStats.Select(stat => stat.ToDomain()).ToArray();

            gameState.AddCrackedPlatform(self);
            gameState.AddCrackedWalls(self);
            gameState.AddSpikeball(self);
            gameState.AddExplosions(self);
            gameState.AddBGMushrooms(self);

            gameState.AdditionnalData = TF.State.Domain.ServiceCollections.ResolveAPIManager().GetStates();

            return gameState;
        }

        public static void LoadState(this Level level, GameState gameState)
        {
            var hudService = ServiceCollections.ResolveHUDService();
            var sessionService = ServiceCollections.ResolveSessionService();
            var rngService = ServiceCollections.ResolveRngService();
            var sfxService = ServiceCollections.ResolveSFXService();
            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            //To deal with coroutine brambles
            sessionService.LoadBramblesStartingState(gameState.Session.BramblesStartingState);

            sessionService.LoadRoundData(gameState.Entities.RoundData);

            sfxService.UpdateLastRollbackFrame(gameState.Frame);
            sfxService.Load(gameState.SFXs.Select(sfx => new TF.State.Domain.Models.SFX
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

            var rotatesLevels = dynLevelSystem.Get<List<string>>("levels") != null;

            if (rotatesLevels)
            {
                dynLevelSystem.Set("levels", roundLevels);
                dynLevelSystem.Set("lastLevel", gameState.RoundLogic.RoundLevels.Last);
            }

            //Session load
            var session = new TF.State.Domain.Models.Session
            {
                IsEnding = gameState.Session.IsEnding,
                RoundStarted = gameState.Session.RoundStarted,
                RoundEndCounter = gameState.Session.RoundEndCounter,
                RoundIndex = gameState.Session.RoundIndex,
                IsDone = gameState.Session.IsDone,
                Miasma = new TF.State.Domain.Models.Miasma
                {
                    Counter = gameState.Session.Miasma.Counter,
                    Mode = gameState.Session.Miasma.Mode,
                    Ticks = gameState.Session.Miasma.Ticks,
                    Dir = gameState.Session.Miasma.Dir,
                    IsDissipating = gameState.Session.Miasma.IsDissipating,
                    DissipateTicks = gameState.Session.Miasma.DissipateTicks,
                    DissipateStartPercent = gameState.Session.Miasma.DissipateStartPercent,
                    DissipateStartSideWeight = gameState.Session.Miasma.DissipateStartSideWeight,
                },
                Scores = gameState.Session.Scores.ToArray(),
                OldScores = gameState.Session.OldScores.ToArray(),
            };

            sessionService.SaveSession(session);

            level.Session.Scores = session.Scores.ToArray();
            level.Session.OldScores = session.OldScores.ToArray();

            if (session.RoundIndex != level.Session.RoundIndex)
            {
                if (rotatesLevels)
                {
                    roundLevels.Insert(0, gameState.RoundLogic.RoundLevels.Last);
                    dynLevelSystem.Set("levels", roundLevels);
                }

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

                level.Begin();
            }

            var dynGameplayLayer = DynamicData.For(level.GetGameplayLayer());
            Dictionary<int, double> actualDepthLookup = dynGameplayLayer.Get<Dictionary<int, double>>("actualDepthLookup");
            actualDepthLookup.Clear();

            foreach (var item in gameState.Layer.GameplayLayerActualDepthLookup)
            {
                actualDepthLookup.Add(item.Key, item.Value);
            }

            var dynScene = DynamicData.For(level as Monocle.Scene);
            dynScene.Set("FrameCounter", (float)gameState.Frame);
            level.EndScreenShake();

            level.Frozen = gameState.IsLevelFrozen;

            var dynRoundLogic = DynamicData.For(level.Session.RoundLogic);
            dynRoundLogic.Set("RoundStarted", session.RoundStarted);
            dynRoundLogic.Set("done", session.IsDone);
            dynRoundLogic.Set("Time", gameState.RoundLogic.Time);

            var liveKills = level.Session.RoundLogic.Kills;
            var savedKills = gameState.RoundLogic.Kills;

            if (liveKills != null && savedKills != null)
            {
                for (int seat = 0; seat < liveKills.Length; seat++)
                {
                    liveKills[seat] = seat < savedKills.Length ? savedKills[seat] : 0;
                }
            }

            if (currentMode.IsNetplay() || StateFlags.IsTestMode || StateFlags.IsCaptureActive)
            {
                var endCounter = dynRoundLogic.Get<RoundEndCounter>("roundEndCounter");

                if (endCounter != null)
                {
                    DynamicData.For(endCounter).Set("endCounter", session.RoundEndCounter);
                    DynamicData.For(endCounter).Set("ghostWaitCounter", session.GhostWaitCounter);
                }
            }

            level.Session.CurrentLevel.Ending = session.IsEnding;

            var dynCounter = dynRoundLogic.Get<Counter>("miasmaCounter");
            var dynamicMiasmaCounter = DynamicData.For(dynCounter);
            dynamicMiasmaCounter.Set("counter", gameState.Session.Miasma.Counter);

            var existingMiasma = level.Get<TowerFall.Miasma>();
            level.Delete<TowerFall.Miasma>();

            var sess = sessionService.GetSession();

            if (gameState.Session.Miasma.IsDissipating)
            {
                var miasma = level.AddMiasmaToGameplayLayer(gameState.Session.Miasma.ActualDepth);

                sess.Miasma.Mode = gameState.Session.Miasma.Mode;
                sess.Miasma.Ticks = gameState.Session.Miasma.Ticks;
                sess.Miasma.Dir = gameState.Session.Miasma.Dir;
                sess.Miasma.IsDissipating = true;
                sess.Miasma.DissipateTicks = gameState.Session.Miasma.DissipateTicks;
                sess.Miasma.DissipateStartPercent = gameState.Session.Miasma.DissipateStartPercent;
                sess.Miasma.DissipateStartSideWeight = gameState.Session.Miasma.DissipateStartSideWeight;

                MiasmaSequenceController.EvaluateDissipate(
                    sess.Miasma.DissipateTicks, sess.Miasma.DissipateStartPercent, sess.Miasma.DissipateStartSideWeight,
                    out float mp, out float msw, out _);
                miasma.Percent = mp;
                miasma.SideWeight = msw;
                miasma.Collidable = false;
                miasma.NervesOfSteelCheck = MiasmaSequenceController.Evaluate(sess.Miasma.Mode, sess.Miasma.Ticks, sess.Miasma.Dir).NervesOfSteelCheck;
                miasma.Update();
                miasma.RestoreMiasmaVisualPhase(sess.Miasma.Ticks + sess.Miasma.DissipateTicks);
                TransplantMiasmaSpriteState(existingMiasma, miasma);
            }
            else if (gameState.Session.Miasma.Ticks > 0f)
            {
                var miasma = level.AddMiasmaToGameplayLayer(gameState.Session.Miasma.ActualDepth);

                sess.Miasma.Mode = gameState.Session.Miasma.Mode;
                sess.Miasma.Ticks = gameState.Session.Miasma.Ticks;
                sess.Miasma.Dir = gameState.Session.Miasma.Dir;
                sess.Miasma.IsDissipating = false;

                var result = MiasmaSequenceController.Evaluate(sess.Miasma.Mode, sess.Miasma.Ticks, sess.Miasma.Dir);
                miasma.Percent = result.Percent;
                miasma.SideWeight = result.SideWeight;
                miasma.Collidable = result.Collidable;
                miasma.NervesOfSteelCheck = result.NervesOfSteelCheck;
                miasma.Update();
                miasma.RestoreMiasmaVisualPhase(sess.Miasma.Ticks);
                TransplantMiasmaSpriteState(existingMiasma, miasma);
            }

            //Event logs
            dynRoundLogic.Set("Events", gameState.RoundLogic.EventLogs.ToTFModel());

            //HUD (VersusRoundResults && VersusStart)
            hudService.Update(new TF.State.Domain.Models.Entity.HUD.HUD());

            var isTrialsState = gameState.Entities.Hud.TrialsControl != null;

            //VersusRoundResults
            level.Delete<VersusRoundResults>();
            level.Delete<HUDFade>();

            level.Delete<VersusMatchResults>();
            level.DeleteAll<VersusPlayerMatchResults>();
            level.DeleteAll<VersusSeedDisplay>();

            if (!isTrialsState && gameState.Entities.Hud.VersusRoundResults.CoroutineState > 0)
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

            var isIntroRunning = !isTrialsState && gameState.Entities.Hud.VersusStart.CoroutineState > 0;

            if (isIntroRunning)
            {
                var versusStart = new TowerFall.VersusStart(level.Session);
                var dynVersusStart = DynamicData.For(versusStart);
                dynVersusStart.Set("Scene", level);
                dynVersusStart.Set("Level", level);

                level.Layers.FirstOrDefault(l => l.Value.Index == versusStart.LayerIndex).Value.Entities.Add(versusStart);
                versusStart.Added();

                level.EnsurePlayerIndicators();

                try
                {
                    for (int i = 0; i < gameState.Entities.Hud.VersusStart.CoroutineState; i++)
                    {
                        versusStart.Update();
                    }
                }
                catch (Exception e)
                {
                    ServiceCollections.ResolveLogger()?.LogError<VersusStart>(
                        $"Could not replay the intro to state {gameState.Entities.Hud.VersusStart.CoroutineState}, dropping it: {e}");

                    level.Delete<VersusStart>();
                }
            }

            hudService.Update(gameState.Entities.Hud);

            //CrumbleBlocks (Darkfang) load before Player
            gameState.LoadCrumbleBlocks(level);
            gameState.LoadCrumbleWalls(level);

            //Players
            foreach (TF.State.Domain.Models.Entity.LevelEntity.Player.Player toLoad in gameState.Entities.Players.ToArray())
            {
                var gamePlayer = level.GetPlayer(toLoad.Index);
                if (gamePlayer != null)
                {
                    gamePlayer.LoadState(toLoad);
                }
                else
                {
                    var allegiance = level.Session.MatchSettings.GetPlayerAllegiance(toLoad.Index);

                    TowerFall.Player player =
                    new TowerFall.Player(
                        toLoad.Index,
                        toLoad.Position.ToTFVector(),
                        allegiance,
                        allegiance,
                        level.Session.GetPlayerInventory(toLoad.Index),
                        level.Session.GetSpawnHatState(toLoad.Index),
                        frozen: false,
                        flash: false,
                    indicator: false);

                    player.LoadState(toLoad);
                    level.GetGameplayLayer().Entities.Insert(0, player);

                    foreach (var tag in player.Tags)
                    {
                        var tagList = level[tag];
                        if (!tagList.Contains(player))
                        {
                            tagList.Add(player);
                        }
                    }
                }
            }

            var loadedIndexes = gameState.Entities.Players.Select(toLoad => toLoad.Index).ToList();
            foreach (var livePlayer in level.GetAll<TowerFall.Player>().ToArray())
            {
                if (!loadedIndexes.Contains(livePlayer.PlayerIndex))
                {
                    level.RemoveEntity(livePlayer);
                }
            }

            if (isIntroRunning)
            {
                level.EnsurePlayerIndicators();
            }

            //PlayerCorpses
            level.DeleteAll<TowerFall.PlayerCorpse>();
            var corpsesToLoad = gameState.Entities.PlayerCorpses.ToArray();

            foreach (TF.State.Domain.Models.Entity.LevelEntity.Player.PlayerCorpse toLoad in corpsesToLoad)
            {
                var cachedPlayerCorpse = ServiceCollections.GetCachedEntity<TowerFall.PlayerCorpse>(toLoad.ActualDepth);

                if (cachedPlayerCorpse == null)
                {
                    cachedPlayerCorpse = toLoad.PlayerIndex < 0
                        ? new TowerFall.PlayerCorpse(
                            TowerFall.PlayerCorpse.EnemyCorpses.Dummy,
                            toLoad.Position.ToTFVector(),
                            (TowerFall.Facing)toLoad.Facing,
                            toLoad.KillerIndex)
                        : new TowerFall.PlayerCorpse(
                            toLoad.Position.ToTFVector(),
                            TowerFall.ArcherData.Get(TFGame.Characters[toLoad.PlayerIndex], TFGame.AltSelect[toLoad.PlayerIndex]),
                            level.Session.MatchSettings.GetPlayerAllegiance(toLoad.PlayerIndex),
                            (TowerFall.Facing)toLoad.Facing,
                            toLoad.PlayerIndex,
                            toLoad.KillerIndex);

                    DynamicData.For(cachedPlayerCorpse).Set("Arrows", new TowerFall.ArrowList());
                }

                cachedPlayerCorpse.LoadState(toLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedPlayerCorpse);
            }

            gameState.LoadQuestSpawnPortals(level);
            gameState.LoadSlimes(level);
            gameState.LoadBats(level);
            gameState.LoadEnemyAttacks(level);
            gameState.LoadDarkPortalsSequence(level);

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

                foreach (TF.State.Domain.Models.Entity.LevelEntity.Arrows.Arrow toLoad in gameState.Entities.Arrows.ToArray())
                {
                    TowerFall.LevelEntity entityHavingArrow = level.GetPlayerOrCorpse(toLoad.PlayerIndex);
                    if (entityHavingArrow == null)
                    {
                        var players = level[GameTags.Player];
                        var corpses = level[GameTags.Corpse];
                        if (players != null && players.Count > 0)
                        {
                            entityHavingArrow = players[0] as TowerFall.LevelEntity;
                        }
                        else if (corpses != null && corpses.Count > 0)
                        {
                            entityHavingArrow = corpses[0] as TowerFall.LevelEntity;
                        }
                    }
                    if (entityHavingArrow == null)
                    {
                        throw new InvalidOperationException("Can't find the original player that shoot the current arrow being loaded");
                    }

                    var arrow = TowerFall.Arrow.Create(toLoad.ArrowType.ToTFModel(), entityHavingArrow, toLoad.Position.ToTFVector(), toLoad.Direction);
                    arrow.LoadState(toLoad, gameState.Session.BramblesStartingState, gameState.Frame);

                    level.GetGameplayLayer().Entities.Insert(0, arrow);
                }
            }

            //TriggerArrow detonation relations
            gameState.LoadTriggerArrows(level);

            //Prism from Prism Arrow
            gameState.LoadPrisms(level);

            //TeamRevivers
            gameState.LoadTeamRevivers(level);
            gameState.LoadPlayerGhosts(level);

            //Chests load
            level.DeleteAll<TowerFall.TreasureChest>();

            foreach (GameTags tag in Enum.GetValues(typeof(GameTags)))
            {
                level[tag]?.RemoveAll(entity => entity is TowerFall.TreasureChest);
            }

            if (gameState.Entities.RoundData.TryGetValue(gameState.Session.RoundIndex, out var chestRoundData))
            {
                foreach (var chestToLoad in chestRoundData.Chests)
                {
                    if (!ServiceCollections.GetCached<TreasureChest>($"chest_{gameState.Session.RoundIndex}_{chestToLoad.ActualDepth}", out var cachedChest))
                    {
                        var chestPickups = chestToLoad.PickupList != null && chestToLoad.PickupList.Any()
                            ? chestToLoad.PickupList.Select(pickup => pickup.ToTFModel()).ToArray()
                            : [chestToLoad.Pickups.ToTFModel()];

                        cachedChest = new TreasureChest(Vector2.Zero, (TreasureChest.Types)chestToLoad.Type, TreasureChest.AppearModes.Time, chestPickups);
                    }

                    cachedChest.LoadState(chestToLoad);

                    SyncTags(level, cachedChest);

                    level.GetGameplayLayer().Entities.Insert(0, cachedChest);
                }
            }

            //Pickups load
            level.DeleteAll<TowerFall.Pickup>();

            foreach (GameTags tag in Enum.GetValues(typeof(GameTags)))
            {
                level[tag]?.RemoveAll(entity => entity is TowerFall.Pickup);
            }

            foreach (var pickupToLoad in gameState.Entities.Pickups.ToArray())
            {
                var cachedPickup = ServiceCollections.GetCachedEntity<TowerFall.Pickup>(pickupToLoad.ActualDepth);

                if (cachedPickup == null)
                {
                    cachedPickup = pickupToLoad.Type == PickupState.Gem
                        ? new TowerFall.GemPickup(pickupToLoad.Position.ToTFVector(), pickupToLoad.TargetPosition.ToTFVector())
                        : TowerFall.Pickup.CreatePickup(pickupToLoad.Position.ToTFVector(), pickupToLoad.TargetPosition.ToTFVector(), pickupToLoad.Type.ToTFModel(), pickupToLoad.PlayerIndex);
                }

                cachedPickup.LoadState(pickupToLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedPickup);

                SyncTags(level, cachedPickup);
            }

            TFGame.Instance.Screen.Offset = gameState.ScreenOffset.ToTFVector();

            //Orbs load
            var orb = gameState.Entities.OrbLogic;
            level.OrbLogic.LoadState(orb);

            //Lantern load
            foreach (TF.State.Domain.Models.Entity.LevelEntity.Lantern toLoad in gameState.Entities.Lanterns.ToArray())
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
            foreach (TF.State.Domain.Models.Entity.LevelEntity.Chain toLoad in gameState.Entities.Chains.ToArray())
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

            //CrackedWall load
            gameState.LoadCrackedWalls(level);

            //BGMushroom load
            gameState.LoadBGMushrooms(level);

            //Background and Foreground load
            //gameState.LoadBackgroundAndForegroundElements(level);

            //MovingPlatform load
            gameState.LoadMovingPlatforms(level);

            //Brambles load
            gameState.LoadBrambles(level);

            //Icicles load
            gameState.LoadIcicles(level);

            //SnowClumps load
            gameState.LoadSnowClumps(level);

            //SwitchBlocks (King's Court) load
            gameState.LoadSwitchBlocks(level);
            gameState.LoadSwitchBlockControl(level);

            //ShiftBlocks (Sunken City) load
            gameState.LoadShiftBlocks(level);

            //ProximityBlocks (Moonstone) load
            gameState.LoadProximityBlocks(level);

            //MoonGlassBlocks (Moonstone) load
            gameState.LoadMoonGlassBlocks(level);

            //GhostPlatforms, LoopPlatforms and RotatePlatforms (The Amaranth) load
            gameState.LoadGhostPlatforms(level);
            gameState.LoadLoopPlatforms(level);
            gameState.LoadRotatePlatforms(level);

            //SensorBlocks (Dreadwood) load
            gameState.LoadSensorBlocks(level);

            //Trials targets and their timer. After the HUD block above, which rebuilds HUD entities.
            gameState.LoadDummies(level);
            gameState.LoadDummyHeads(level);
            gameState.LoadTrialsControl(level);

            var sine = dynLightingLayer.Get<SineWave>("sine");
            sine.UpdateAttributes(gameState.Layer.LightingLayerSine);

            //Rng
            rngService.LoadState(gameState.Rng);

            var matchStats = gameState.MatchStats.ToArray();

            for (int seat = 0; seat < matchStats.Length && seat < level.Session.MatchStats.Length; seat++)
            {
                level.Session.MatchStats[seat] = matchStats[seat].ToTF();
            }

            level.PostLoad(gameState);

            TF.State.Domain.ServiceCollections.ResolveAPIManager().LoadStates(gameState.AdditionnalData);

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
            foreach (TF.State.Domain.Models.Entity.LevelEntity.Player.Player toLoad in gs.Entities.Players.ToArray())
            {
                var gamePlayer = self.GetPlayer(toLoad.Index);

                gamePlayer.LoadDeathArrow(toLoad.DeathArrowDepth);
            }

            foreach (TF.State.Domain.Models.Entity.LevelEntity.Arrows.Arrow toLoad in gs.Entities.Arrows.ToArray())
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
                gamePlayerCorpse.LoadArrowCushionDatas(playerCorpse);
            }

            foreach (var bat in gs.Entities.Enemies.Bats.ToArray())
            {
                var gameBat = self.GetEntityByDepth(bat.ActualDepth) as TowerFall.Bat;
                gameBat?.LoadArrowCushionDatas(bat);
            }
        }

        private static Background.BGElement GetBGElementByIndex(this Level level, int index) { return level.Background.GetBGElements()[index]; }

        private static Background.BGElement GetFGElementByIndex(this Level level, int index) { return level.Foreground.GetBGElements()[index]; }

        //TODO: use generic method instead
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

            gameState.RoundLogic.RoundLevels.Nexts = levels?.ToList() ?? new List<string>();
            gameState.RoundLogic.RoundLevels.Last = lastLevel;

            gameState.IsLevelFrozen = level.Frozen;

            var dynRoundLogic = DynamicData.For(level.Session.RoundLogic);

            gameState.RoundLogic.WasFinalKill = dynRoundLogic.Get<bool?>("wasFinalKill") ?? false;
            gameState.RoundLogic.Time = level.Session.RoundLogic.Time;

            gameState.RoundLogic.Kills = level.Session.RoundLogic.Kills.ToArray();

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
            if (level[GameTags.Arrow] != null && level[GameTags.Arrow].Count > 0)
            {
                var arrows = level[GameTags.Arrow].ToArray();

                foreach (TowerFall.Arrow arrow in arrows)
                {
                    var dynArrow = DynamicData.For(arrow);
                    dynArrow.Set("counter", Vector2.Zero);//For some weird reason, the counter is not reseted an ctor, so we do it here
                                                          //Oh i think i found why, it's might be du to the arrow cache 

                    gameState.Entities.Arrows.Add(arrow.GetState());
                }
            }
        }

        private static void AddChestsState(this GameState gameState, Level level)
        {
            var sessionService = ServiceCollections.ResolveSessionService();
            var roundIndex = level.Session.RoundIndex;
            var chests = new List<Chest>();

            if (level[GameTags.TreasureChest] != null)
            {
                foreach (TowerFall.TreasureChest chest in level[GameTags.TreasureChest].ToArray())
                {
                    var che = chest.GetState();
                    chests.Add(che);
                    ServiceCollections.AddToCache($"chest_{roundIndex}_{che.ActualDepth}", chest, TimeSpan.FromMinutes(5));
                }
            }

            sessionService.UpdateRoundChests(roundIndex, chests);
            gameState.Entities.RoundData = sessionService.GetRoundData();
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
            var sessionService = ServiceCollections.ResolveSessionService();

            var currentMode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            var dynRoundLogic = DynamicData.For(level.Session.RoundLogic);

            if (currentMode.IsNetplay() || StateFlags.IsTestMode || StateFlags.IsCaptureActive)
            {
                var roundEndCounter = dynRoundLogic.Get<RoundEndCounter>("roundEndCounter");
                var dynRoundEndCounter = roundEndCounter != null ? DynamicData.For(roundEndCounter) : null;

                var endCounter = dynRoundEndCounter?.Get<float>("endCounter") ?? 0f;
                var ghostWaitCounter = dynRoundEndCounter?.Get<float>("ghostWaitCounter") ?? 0f;
                var roundStarted = level.Session.RoundLogic.RoundStarted;
                var done = dynRoundLogic.Get<bool?>("done") ?? false;
                var roundIndex = level.Session.RoundIndex;

                var isEnding = level.Session.CurrentLevel.Ending;

                var miasmaCounter = dynRoundLogic.Get<Counter>("miasmaCounter");
                var counter = DynamicData.For(miasmaCounter).Get<float>("counter");

                var stateSession = sessionService.GetSession();

                stateSession.RoundStarted = roundStarted;
                stateSession.RoundEndCounter = endCounter;
                stateSession.GhostWaitCounter = ghostWaitCounter;
                stateSession.IsEnding = isEnding;
                stateSession.Miasma.Counter = counter;
                stateSession.IsDone = done;
                stateSession.RoundIndex = roundIndex;
                var session = new TF.State.Domain.Models.Session
                {
                    IsEnding = stateSession.IsEnding,
                    RoundEndCounter = stateSession.RoundEndCounter,
                    IsDone = stateSession.IsDone,
                    RoundIndex = stateSession.RoundIndex,
                    RoundStarted = stateSession.RoundStarted,
                    Miasma = new TF.State.Domain.Models.Miasma
                    {
                        Counter = stateSession.Miasma.Counter,
                        Mode = stateSession.Miasma.Mode,
                        Ticks = stateSession.Miasma.Ticks,
                        Dir = stateSession.Miasma.Dir,
                        IsDissipating = stateSession.Miasma.IsDissipating,
                        DissipateTicks = stateSession.Miasma.DissipateTicks,
                        DissipateStartPercent = stateSession.Miasma.DissipateStartPercent,
                        DissipateStartSideWeight = stateSession.Miasma.DissipateStartSideWeight,
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
            var sessionService = ServiceCollections.ResolveSessionService();
            var roundIndex = level.Session.RoundIndex;
            var orbs = new List<TF.State.Domain.Models.Entity.LevelEntity.Orb>();

            foreach (TowerFall.Orb orb in level.GetAll<TowerFall.Orb>())
            {
                orbs.Add(orb.GetState());
            }

            sessionService.UpdateRoundOrbs(roundIndex, orbs);
            gameState.Entities.RoundData = sessionService.GetRoundData();
        }

        private static void AddLavaControlState(this GameState gameState, Level level)
        {
            var sessionService = ServiceCollections.ResolveSessionService();
            var roundIndex = level.Session.RoundIndex;

            var gameLavaControl = level.Get<LavaControl>();
            sessionService.UpdateRoundLavaControl(roundIndex, gameLavaControl?.GetState());
            gameState.Entities.RoundData = sessionService.GetRoundData();
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
            //var bgElements = level.Background.GetBGElements().ToArray();
            //List<BGElement> bgs = new List<BGElement>();
            //for (int i = 0; i < bgElements.Length; i++)
            //{
            //    var bgModel = bgElements[i].GetState(i);
            //    bgs.Add(bgModel);
            //}
            //gameState.Layer.BackgroundElements = bgs;

            //Foreground save
            //if (level.Foreground != null)
            //{
            //    var fgElements = level.Foreground.GetBGElements().ToArray();
            //    List<BGElement> fgs = new List<BGElement>();
            //    for (int i = 0; i < fgElements.Length; i++)
            //    {
            //        var fgModel = fgElements[i].GetState(i);
            //        fgs.Add(fgModel);
            //    }
            //    gameState.Layer.ForegroundElements = fgs;
            //}

            var dynLightingLayer = DynamicData.For(level.LightingLayer);

            var sineWave = dynLightingLayer.Get<SineWave>("sine");
            gameState.Layer.LightingLayerSine = sineWave.Counter;

            level.DeleteAll<TowerFall.Hat>(); //TODO: Remove and save hats

            var dynGameplayLayer = DynamicData.For(level.GetGameplayLayer());
            var actualDepthLookup = dynGameplayLayer.Get<Dictionary<int, double>>("actualDepthLookup");
            actualDepthLookup.Remove(-52);

            actualDepthLookup.Remove(-600); //TODO: Miasma

            actualDepthLookup.Remove(-490); //DEPTH_PARTICES_FG:

            var canonicalDepthLookup = actualDepthLookup
                .OrderBy(entry => entry.Key)
                .ToDictionary(entry => entry.Key, entry => entry.Value);

            sessionService.SaveGamePlayLayerActualDepthLookup(canonicalDepthLookup);
            gameState.Layer.GameplayLayerActualDepthLookup = canonicalDepthLookup;
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
                var state = spikeball.GetState();
                gameState.Entities.Spikeball = state;
                ServiceCollections.AddEntityToCache(state.ActualDepth, spikeball);
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

        public static void AddCrackedWalls(this GameState gameState, TowerFall.Level level)
        {
            var crackedWalls = level.GetAll<TowerFall.CrackedWall>().ToArray();
            if (crackedWalls != null && crackedWalls.Length > 0)
            {
                foreach (TowerFall.CrackedWall crackedWall in crackedWalls)
                {
                    var crackedWallState = crackedWall.GetState();
                    gameState.Entities.CrackedWalls.Add(crackedWallState);
                    ServiceCollections.AddEntityToCache(crackedWallState.ActualDepth, crackedWall);
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

        private static void RestoreMiasmaVisualPhase(this TowerFall.Miasma miasma, float elapsedTicks)
        {
            var dynMiasma = DynamicData.For(miasma);
            // sine = new SineWave(100); tentaclesSine = new SineWave(240) started down (Counter = 3*PI/2).
            SetSinePhase(dynMiasma.Get<SineWave>("sine"), MathF.PI * 2f / 100f, 0f, elapsedTicks);
            SetSinePhase(dynMiasma.Get<SineWave>("tentaclesSine"), MathF.PI * 2f / 240f, 4.712389f, elapsedTicks);
        }

        private static void SetSinePhase(SineWave sine, float rate, float initialCounter, float elapsedTicks)
        {
            if (sine == null)
            {
                return;
            }

            float counter = (initialCounter + rate * elapsedTicks) % (MathF.PI * 8f);
            var dynSine = DynamicData.For(sine);
            dynSine.Set("Counter", counter);
            dynSine.Set("Value", (float)Math.Sin(counter));
            dynSine.Set("ValueOverTwo", (float)Math.Sin(counter / 2f));
            dynSine.Set("TwoValue", (float)Math.Sin(counter * 2f));
        }

        private static void TransplantMiasmaSpriteState(TowerFall.Miasma from, TowerFall.Miasma to)
        {
            if (from == null || to == null)
            {
                return;
            }

            var dynFrom = DynamicData.For(from);
            var dynTo = DynamicData.For(to);
            CopySpriteArrayState(dynFrom.Get<Monocle.Sprite<int>[]>("leftSprites"), dynTo.Get<Monocle.Sprite<int>[]>("leftSprites"));
            CopySpriteArrayState(dynFrom.Get<Monocle.Sprite<int>[]>("rightSprites"), dynTo.Get<Monocle.Sprite<int>[]>("rightSprites"));
        }

        private static void CopySpriteArrayState(Monocle.Sprite<int>[] from, Monocle.Sprite<int>[] to)
        {
            if (from == null || to == null)
            {
                return;
            }

            int count = Math.Min(from.Length, to.Length);
            for (int i = 0; i < count; i++)
            {
                if (from[i] == null || to[i] == null)
                {
                    continue;
                }

                var dynFrom = DynamicData.For(from[i]);
                var dynTo = DynamicData.For(to[i]);
                dynTo.Set("currentFrame", dynFrom.Get<int>("currentFrame"));
                dynTo.Set("timer", dynFrom.Get<float>("timer"));
                dynTo.Set("AnimationFrame", dynFrom.Get<int>("AnimationFrame"));
            }
        }

        private static void AddBGMushrooms(this GameState game, Level level)
        {
            var bgMushrooms = level.GetAll<TowerFall.BGMushroom>().ToArray();
            if (bgMushrooms != null && bgMushrooms.Length > 0)
            {
                foreach (TowerFall.BGMushroom bgMushroom in bgMushrooms)
                {
                    var mush = bgMushroom.GetState();
                    game.Entities.BGMushrooms.Add(mush);
                }
            }
        }

        private static void AddMovingPlatformState(this GameState gameState, Level level)
        {

            var movingPlatforms = level.GetAll<TowerFall.MovingPlatform>().ToArray();

            foreach (TowerFall.MovingPlatform movingPlatform in movingPlatforms)
            {
                var state = movingPlatform.GetState();
                gameState.Entities.MovingPlatforms.Add(state);
            }
        }

        private static void AddBramblesState(this GameState gameState, Level level)
        {
            var brambles = level.GetAll<TowerFall.Brambles>().ToArray();
            if (brambles != null && brambles.Length > 0)
            {
                foreach (TowerFall.Brambles bramble in brambles)
                {
                    var state = bramble.GetState();
                    gameState.Entities.Brambles.Add(state);
                    ServiceCollections.AddEntityToCache(state.ActualDepth, bramble);
                }
            }
        }

        private static void AddIciclesState(this GameState gameState, Level level)
        {
            var icicles = level.GetAll<TowerFall.Icicle>().ToArray();
            if (icicles != null && icicles.Length > 0)
            {
                foreach (TowerFall.Icicle icicle in icicles)
                {
                    var state = icicle.GetState();
                    gameState.Entities.Icicles.Add(state);
                    ServiceCollections.AddEntityToCache(state.ActualDepth, icicle);
                }
            }
        }

        private static void AddSnowClumpsState(this GameState gameState, Level level)
        {
            var snowClumps = level.GetAll<TowerFall.SnowClump>().ToArray();
            if (snowClumps != null && snowClumps.Length > 0)
            {
                foreach (TowerFall.SnowClump snowClump in snowClumps)
                {
                    var state = snowClump.GetState();
                    gameState.Entities.SnowClumps.Add(state);
                    ServiceCollections.AddEntityToCache(state.ActualDepth, snowClump);
                }
            }
        }

        private static void AddCrumbleWallsState(this GameState gameState, Level level)
        {
            foreach (TowerFall.CrumbleWall crumbleWall in level.GetAll<TowerFall.CrumbleWall>())
            {
                var state = crumbleWall.GetState();
                gameState.Entities.CrumbleWalls.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, crumbleWall);
            }
        }

        private static void AddPrismsState(this GameState gameState, Level level)
        {
            foreach (TowerFall.Prism prism in level.GetAll<TowerFall.Prism>())
            {
                var state = prism.GetState();
                gameState.Entities.Prisms.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, prism);
            }
        }

        private static void AddTeamReviversState(this GameState gameState, Level level)
        {
            foreach (TowerFall.TeamReviver teamReviver in level.GetAll<TowerFall.TeamReviver>())
            {
                var state = teamReviver.GetState();
                gameState.Entities.TeamRevivers.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, teamReviver);
            }
        }

        private static void AddPlayerGhostsState(this GameState gameState, Level level)
        {
            foreach (TowerFall.PlayerGhost playerGhost in level.GetAll<TowerFall.PlayerGhost>())
            {
                var state = playerGhost.GetState();
                gameState.Entities.PlayerGhosts.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, playerGhost);
            }
        }

        private static void AddQuestSpawnPortalsState(this GameState gameState, Level level)
        {
            foreach (TowerFall.QuestSpawnPortal portal in level.GetAll<TowerFall.QuestSpawnPortal>())
            {
                var state = portal.GetState();
                gameState.Entities.Enemies.QuestSpawnPortals.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, portal);
            }
        }

        private static void AddSlimesState(this GameState gameState, Level level)
        {
            foreach (TowerFall.Slime slime in level.GetAll<TowerFall.Slime>())
            {
                var state = slime.GetState();
                gameState.Entities.Enemies.Slimes.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, slime);
            }
        }

        private static void AddBatsState(this GameState gameState, Level level)
        {
            foreach (TowerFall.Bat bat in level.GetAll<TowerFall.Bat>())
            {
                var state = bat.GetState();
                gameState.Entities.Enemies.Bats.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, bat);
            }
        }

        private static void AddEnemyAttacksState(this GameState gameState, Level level)
        {
            foreach (TowerFall.EnemyAttack enemyAttack in level.GetAll<TowerFall.EnemyAttack>())
            {
                var state = enemyAttack.GetState();
                gameState.Entities.Enemies.EnemyAttacks.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, enemyAttack);
            }
        }

        private static void AddDarkPortalsSequenceState(this GameState gameState, Level level)
        {
            var sequence = level.GetAll<TowerFall.DarkPortalsVariantSequence>().FirstOrDefault();
            if (sequence == null)
            {
                return;
            }

            var dynSequence = DynamicData.For(sequence);
            gameState.Entities.Enemies.DarkPortalsSequence = new TF.State.Domain.Models.Entity.LevelEntity.DarkPortalsSequence
            {
                Phase = dynSequence.Get<int>("darkPortalsPhase"),
                Counter = dynSequence.Get<float>("darkPortalsCounter"),
            };
        }

        private static void AddCrumbleBlocksState(this GameState gameState, Level level)
        {
            var crumbleBlocks = level.GetAll<TowerFall.CrumbleBlock>().ToArray();
            if (crumbleBlocks != null && crumbleBlocks.Length > 0)
            {
                foreach (TowerFall.CrumbleBlock crumbleBlock in crumbleBlocks)
                {
                    var state = crumbleBlock.GetState();
                    gameState.Entities.CrumbleBlocks.Add(state);
                    ServiceCollections.AddEntityToCache(state.ActualDepth, crumbleBlock);
                }
            }
        }

        private static void AddSwitchBlocksState(this GameState state, Level level)
        {
            var switchBlocks = level.GetAll<TowerFall.SwitchBlock>();
            foreach (var switchBlock in switchBlocks)
            {
                state.Entities.SwitchBlocks.Add(switchBlock.GetState());
            }
        }

        private static void AddDummiesState(this GameState gameState, Level level)
        {
            foreach (var dummy in level.GetAll<TowerFall.Dummy>())
            {
                var state = dummy.GetState();
                gameState.Entities.Dummies.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, dummy);
            }
        }

        private static void LoadDummies(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.Dummy>();

            foreach (GameTags tag in Enum.GetValues(typeof(GameTags)))
            {
                level[tag]?.RemoveAll(entity => entity is TowerFall.Dummy);
            }

            foreach (var toLoad in gameState.Entities.Dummies)
            {
                var cachedDummy = ServiceCollections.GetCachedEntity<TowerFall.Dummy>(toLoad.ActualDepth)
                    ?? new TowerFall.Dummy(toLoad.Position.ToTFVector(), (TowerFall.Facing)toLoad.Facing);

                var dynDummy = DynamicData.For(cachedDummy);

                dynDummy.Set("Scene", level);
                dynDummy.Set("Level", level);

                cachedDummy.SceneBegin();

                cachedDummy.LoadState(toLoad);
                level.GetGameplayLayer().Entities.Insert(0, cachedDummy);

                SyncTags(level, cachedDummy);
            }
        }

        private static void AddDummyHeadsState(this GameState gameState, Level level)
        {
            foreach (var head in level.GetAll<TowerFall.DummyHead>())
            {
                var state = head.GetState();
                gameState.Entities.DummyHeads.Add(state);
                ServiceCollections.AddEntityToCache(state.ActualDepth, head);
            }
        }

        private static void LoadDummyHeads(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.DummyHead>();

            foreach (GameTags tag in Enum.GetValues(typeof(GameTags)))
            {
                level[tag]?.RemoveAll(entity => entity is TowerFall.DummyHead);
            }

            foreach (var toLoad in gameState.Entities.DummyHeads)
            {
                var cachedHead = ServiceCollections.GetCachedEntity<TowerFall.DummyHead>(toLoad.ActualDepth)
                    ?? new TowerFall.DummyHead(toLoad.Position.ToTFVector(), null, (int)toLoad.ImageScaleX);

                var dynHead = DynamicData.For(cachedHead);

                dynHead.Set("Scene", level);
                dynHead.Set("Level", level);

                cachedHead.LoadState(toLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedHead);
                SyncTags(level, cachedHead);
            }
        }

        private static void AddTrialsControlState(this GameState gameState, Level level)
        {
            var control = level.Get<TowerFall.TrialsControl>();

            if (control != null)
            {
                gameState.Entities.Hud.TrialsControl = control.GetState();
            }
        }

        private static void LoadTrialsControl(this GameState gameState, Level level)
        {
            if (gameState.Entities.Hud.TrialsControl == null)
            {
                return;
            }

            level.Get<TowerFall.TrialsControl>()?.LoadState(
                gameState.Entities.Hud.TrialsControl, level.GetAll<TowerFall.Dummy>().Count());
        }

        private static void AddSwitchBlockControlState(this GameState state, Level level)
        {
            var control = level.Get<TowerFall.SwitchBlockControl>();
            if (control != null)
            {
                state.Entities.SwitchBlockControl = control.GetState();
            }
        }

        private static void LoadSwitchBlocks(this GameState gameState, Level level)
        {
            var inGameSwitchBlocks = level.GetAll<TowerFall.SwitchBlock>();
            foreach (var switchBlock in gameState.Entities.SwitchBlocks)
            {
                var toLoad = inGameSwitchBlocks.FirstOrDefault(sb =>
                    DynamicData.For(sb).Get<double>("actualDepth") == switchBlock.ActualDepth);

                toLoad?.LoadState(switchBlock);
            }
        }

        private static void LoadSwitchBlockControl(this GameState gameState, Level level)
        {
            if (gameState.Entities.SwitchBlockControl == null)
            {
                return;
            }

            var control = level.Get<TowerFall.SwitchBlockControl>();
            control?.LoadState(gameState.Entities.SwitchBlockControl);
        }

        private static void AddShiftBlocksState(this GameState state, Level level)
        {
            var shiftBlocks = level.GetAll<TowerFall.ShiftBlock>();
            foreach (var shiftBlock in shiftBlocks)
            {
                state.Entities.ShiftBlocks.Add(shiftBlock.GetState());
            }
        }

        private static void LoadShiftBlocks(this GameState gameState, Level level)
        {
            var inGameShiftBlocks = level.GetAll<TowerFall.ShiftBlock>();
            foreach (var shiftBlock in gameState.Entities.ShiftBlocks)
            {
                var toLoad = inGameShiftBlocks.FirstOrDefault(sb =>
                    DynamicData.For(sb).Get<double>("actualDepth") == shiftBlock.ActualDepth);

                toLoad?.LoadState(shiftBlock);
            }
        }

        private static void AddProximityBlocksState(this GameState state, Level level)
        {
            var proximityBlocks = level.GetAll<TowerFall.ProximityBlock>();
            foreach (var proximityBlock in proximityBlocks)
            {
                state.Entities.ProximityBlocks.Add(proximityBlock.GetState());
            }
        }

        private static void LoadProximityBlocks(this GameState gameState, Level level)
        {
            var inGameProximityBlocks = level.GetAll<TowerFall.ProximityBlock>();
            foreach (var proximityBlock in gameState.Entities.ProximityBlocks)
            {
                var toLoad = inGameProximityBlocks.FirstOrDefault(pb =>
                    DynamicData.For(pb).Get<double>("actualDepth") == proximityBlock.ActualDepth);

                toLoad?.LoadState(proximityBlock);
            }
        }

        private static void AddGhostPlatformsState(this GameState state, Level level)
        {
            foreach (var ghostPlatform in level.GetAll<TowerFall.GhostPlatform>())
            {
                state.Entities.GhostPlatforms.Add(ghostPlatform.GetState());
            }
        }

        private static void LoadGhostPlatforms(this GameState gameState, Level level)
        {
            var inGameGhostPlatforms = level.GetAll<TowerFall.GhostPlatform>();
            foreach (var ghostPlatform in gameState.Entities.GhostPlatforms)
            {
                var toLoad = inGameGhostPlatforms.FirstOrDefault(gp =>
                    DynamicData.For(gp).Get<double>("actualDepth") == ghostPlatform.ActualDepth);

                toLoad?.LoadState(ghostPlatform);
            }
        }

        private static void AddLoopPlatformsState(this GameState state, Level level)
        {
            foreach (var loopPlatform in level.GetAll<TowerFall.LoopPlatform>())
            {
                state.Entities.LoopPlatforms.Add(loopPlatform.GetState());
            }
        }

        private static void LoadLoopPlatforms(this GameState gameState, Level level)
        {
            var inGameLoopPlatforms = level.GetAll<TowerFall.LoopPlatform>();
            foreach (var loopPlatform in gameState.Entities.LoopPlatforms)
            {
                var toLoad = inGameLoopPlatforms.FirstOrDefault(lp =>
                    DynamicData.For(lp).Get<double>("actualDepth") == loopPlatform.ActualDepth);

                toLoad?.LoadState(loopPlatform);
            }
        }

        private static void AddRotatePlatformsState(this GameState state, Level level)
        {
            foreach (var rotatePlatform in level.GetAll<TowerFall.RotatePlatform>())
            {
                state.Entities.RotatePlatforms.Add(rotatePlatform.GetState());
            }
        }

        private static void LoadRotatePlatforms(this GameState gameState, Level level)
        {
            var inGameRotatePlatforms = level.GetAll<TowerFall.RotatePlatform>();
            foreach (var rotatePlatform in gameState.Entities.RotatePlatforms)
            {
                var toLoad = inGameRotatePlatforms.FirstOrDefault(rp =>
                    DynamicData.For(rp).Get<double>("actualDepth") == rotatePlatform.ActualDepth);

                toLoad?.LoadState(rotatePlatform);
            }
        }

        private static void AddSensorBlocksState(this GameState state, Level level)
        {
            foreach (var sensorBlock in level.GetAll<TowerFall.SensorBlock>())
            {
                state.Entities.SensorBlocks.Add(sensorBlock.GetState());
            }
        }

        private static void LoadSensorBlocks(this GameState gameState, Level level)
        {
            var inGameSensorBlocks = level.GetAll<TowerFall.SensorBlock>();
            foreach (var sensorBlock in gameState.Entities.SensorBlocks)
            {
                var toLoad = inGameSensorBlocks.FirstOrDefault(sb =>
                    DynamicData.For(sb).Get<double>("actualDepth") == sensorBlock.ActualDepth);

                toLoad?.LoadState(sensorBlock);
            }
        }

        private static void AddMoonGlassBlocksState(this GameState state, Level level)
        {
            var moonGlassBlocks = level.GetAll<TowerFall.MoonGlassBlock>();
            foreach (var moonGlassBlock in moonGlassBlocks)
            {
                var moonGlassBlockState = moonGlassBlock.GetState();
                state.Entities.MoonGlassBlocks.Add(moonGlassBlockState);
                ServiceCollections.AddEntityToCache(moonGlassBlockState.ActualDepth, moonGlassBlock);
            }
        }

        private static void LoadMoonGlassBlocks(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.MoonGlassBlock>();

            foreach (var toLoad in gameState.Entities.MoonGlassBlocks.ToArray())
            {
                var cachedMoonGlassBlock = ServiceCollections.GetCachedEntity<TowerFall.MoonGlassBlock>(toLoad.ActualDepth);

                if (cachedMoonGlassBlock == null)
                {
                    cachedMoonGlassBlock = new TowerFall.MoonGlassBlock(
                        toLoad.Position.ToTFVector(), toLoad.Width, toLoad.Height);
                }

                var dynCachedMoonGlassBlock = DynamicData.For(cachedMoonGlassBlock);
                dynCachedMoonGlassBlock.Set("Level", level);
                dynCachedMoonGlassBlock.Set("Scene", level);

                cachedMoonGlassBlock.LoadState(toLoad);
                level.GetGameplayLayer().Entities.Insert(0, cachedMoonGlassBlock);

                foreach (var tag in cachedMoonGlassBlock.Tags)
                {
                    var tagList = level[tag];
                    if (!tagList.Contains(cachedMoonGlassBlock))
                    {
                        tagList.Add(cachedMoonGlassBlock);
                    }
                }
            }
        }

        private static void LoadLavaControl(this GameState gameState, Level level)
        {
            var gameLavaControl = level.Get<LavaControl>();
            gameState.Entities.RoundData.TryGetValue(gameState.Session.RoundIndex, out var lavaRoundData);
            var lavaControl = lavaRoundData?.LavaControl;
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

                    dynLavaC.Set("Scene", level);
                    dynLavaC.Set("Level", level);
                    level.GetGameplayLayer().Entities.Add(lavaC);

                    var lavas = new TowerFall.Lava[lavaControl.Lavas.Count()];

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
                    dynOrbLogic.Set("control", lavaC);
                }
            }
        }

        private static void LoadOrb(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.Orb>();

            if (gameState.Entities.RoundData.TryGetValue(gameState.Session.RoundIndex, out var orbRoundData))
            {
                foreach (var toLoad in orbRoundData.Orbs)
                {
                    var orb = new TowerFall.Orb(toLoad.Position.ToTFVector(), toLoad.Explodes);
                    level.Spawn(orb, toLoad.ActualDepth);
                    orb.LoadState(toLoad);
                }
            }
        }

        private static void LoadTriggerArrows(this GameState gameState, Level level)
        {
            foreach (var toLoad in gameState.Entities.Players)
            {
                var gamePlayer = level.GetPlayer(toLoad.Index);
                if (gamePlayer == null)
                {
                    continue;
                }

                var triggerArrows = DynamicData.For(gamePlayer).Get<List<TowerFall.TriggerArrow>>("triggerArrows");
                triggerArrows.Clear();

                if (toLoad.TriggerArrowDepths == null)
                {
                    continue;
                }

                foreach (var depth in toLoad.TriggerArrowDepths)
                {
                    if (level.GetEntityByDepth(depth) is TowerFall.TriggerArrow triggerArrow)
                    {
                        DynamicData.For(triggerArrow).Set("playerDetonator", gamePlayer);
                        triggerArrows.Add(triggerArrow);
                    }
                }
            }
        }

        private static void LoadCrumbleWalls(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.CrumbleWall>();

            foreach (var toLoad in gameState.Entities.CrumbleWalls)
            {
                var cachedCrumbleWall = ServiceCollections.GetCachedEntity<TowerFall.CrumbleWall>(toLoad.ActualDepth);

                if (cachedCrumbleWall == null)
                {
                    cachedCrumbleWall = new TowerFall.CrumbleWall(toLoad.Position.ToTFVector());
                }

                cachedCrumbleWall.LoadState(toLoad);
                level.GetGameplayLayer().Entities.Insert(0, cachedCrumbleWall);

                foreach (var tag in cachedCrumbleWall.Tags)
                {
                    var tagList = level[tag];
                    if (!tagList.Contains(cachedCrumbleWall))
                    {
                        tagList.Add(cachedCrumbleWall);
                    }
                }
            }
        }

        private static void LoadPrisms(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.Prism>();

            foreach (var player in level.GetAll<TowerFall.Player>())
            {
                var dynPlayer = DynamicData.For(player);
                dynPlayer.Set("Prism", null);
                dynPlayer.Set("depth", Constants.PLAYER_DEPTH);
            }

            foreach (var toLoad in gameState.Entities.Prisms)
            {
                var cachedPrism = ServiceCollections.GetCachedEntity<TowerFall.Prism>(toLoad.ActualDepth);

                if (cachedPrism == null)
                {
                    cachedPrism = new TowerFall.Prism();
                }

                cachedPrism.LoadState(toLoad);

                var encasedPlayer = toLoad.EncasedPlayerIndex >= 0 ? level.GetPlayer(toLoad.EncasedPlayerIndex) : null;

                DynamicData.For(cachedPrism).Set("EncasedPlayer", encasedPlayer);

                if (encasedPlayer != null)
                {
                    var dynEncased = DynamicData.For(encasedPlayer);
                    dynEncased.Set("Prism", cachedPrism);
                    dynEncased.Set("depth", Constants.PLAYER_PRISM_DEPTH);
                }

                level.GetGameplayLayer().Entities.Insert(0, cachedPrism);

                foreach (var tag in cachedPrism.Tags)
                {
                    var tagList = level[tag];
                    if (!tagList.Contains(cachedPrism))
                    {
                        tagList.Add(cachedPrism);
                    }
                }
            }

            level.DropLiveFromEntityPool<TowerFall.Prism>();
        }

        private static void LoadPlayerGhosts(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.PlayerGhost>();

            level[GameTags.PlayerGhost].RemoveAll(entity => entity is TowerFall.PlayerGhost);
            level[GameTags.PlayerGhostCollider].RemoveAll(entity => entity is TowerFall.PlayerGhost);
            level[GameTags.LightSource].RemoveAll(entity => entity is TowerFall.PlayerGhost);

            foreach (var toLoad in gameState.Entities.PlayerGhosts)
            {
                var cachedPlayerGhost = ServiceCollections.GetCachedEntity<TowerFall.PlayerGhost>(toLoad.ActualDepth);

                if (cachedPlayerGhost == null)
                {
                    var corpse = level.GetAll<TowerFall.PlayerCorpse>().FirstOrDefault(c => c.PlayerIndex == toLoad.PlayerIndex)
                        ?? level.GetAll<TowerFall.PlayerCorpse>().FirstOrDefault();

                    if (corpse == null)
                    {
                        continue;
                    }

                    cachedPlayerGhost = new TowerFall.PlayerGhost(corpse);
                }

                cachedPlayerGhost.LoadState(toLoad);
                cachedPlayerGhost.LoadDespawnCorpse(toLoad, level);

                level.GetGameplayLayer().Entities.Insert(0, cachedPlayerGhost);

                foreach (var tag in cachedPlayerGhost.Tags)
                {
                    var tagList = level[tag];
                    if (!tagList.Contains(cachedPlayerGhost))
                    {
                        tagList.Add(cachedPlayerGhost);
                    }
                }
            }
        }

        private static void LoadQuestSpawnPortals(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.QuestSpawnPortal>();
            level[GameTags.LightSource].RemoveAll(entity => entity is TowerFall.QuestSpawnPortal);

            foreach (var toLoad in gameState.Entities.Enemies.QuestSpawnPortals)
            {
                var cachedPortal = ServiceCollections.GetCachedEntity<TowerFall.QuestSpawnPortal>(toLoad.ActualDepth)
                    ?? new TowerFall.QuestSpawnPortal(toLoad.Position.ToTFVector(), null);

                cachedPortal.LoadState(toLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedPortal);
                SyncTags(level, cachedPortal);
            }
        }

        private static void LoadSlimes(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.Slime>();
            RemoveEnemyTags(level, entity => entity is TowerFall.Slime);

            foreach (var toLoad in gameState.Entities.Enemies.Slimes)
            {
                var cachedSlime = ServiceCollections.GetCachedEntity<TowerFall.Slime>(toLoad.ActualDepth)
                    ?? new TowerFall.Slime(toLoad.Position.ToTFVector(), (TowerFall.Facing)toLoad.Facing, (TowerFall.Slime.SlimeColors)toLoad.SlimeColor);

                cachedSlime.LoadState(toLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedSlime);
                SyncTags(level, cachedSlime);
            }
        }

        private static void LoadBats(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.Bat>();
            RemoveEnemyTags(level, entity => entity is TowerFall.Bat);

            foreach (var toLoad in gameState.Entities.Enemies.Bats)
            {
                var cachedBat = ServiceCollections.GetCachedEntity<TowerFall.Bat>(toLoad.ActualDepth)
                    ?? new TowerFall.Bat(toLoad.Position.ToTFVector(), (TowerFall.Facing)toLoad.Facing, (TowerFall.Bat.BatType)toLoad.BatType);

                cachedBat.LoadState(toLoad);
                cachedBat.LoadTarget(toLoad, level);

                level.GetGameplayLayer().Entities.Insert(0, cachedBat);
                SyncTags(level, cachedBat);
            }
        }

        private static void LoadEnemyAttacks(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.EnemyAttack>();
            level[GameTags.PlayerCollider].RemoveAll(entity => entity is TowerFall.EnemyAttack);
            level[GameTags.Target].RemoveAll(entity => entity is TowerFall.EnemyAttack);

            foreach (var toLoad in gameState.Entities.Enemies.EnemyAttacks)
            {
                var enemy = level.GetEntityByDepth(toLoad.EnemyActualDepth) as TowerFall.Enemy;
                if (enemy == null)
                {
                    continue;
                }

                var cachedAttack = ServiceCollections.GetCachedEntity<TowerFall.EnemyAttack>(toLoad.ActualDepth)
                    ?? Monocle.Cache.Create<TowerFall.EnemyAttack>();

                cachedAttack.LoadState(toLoad, enemy);

                level.GetGameplayLayer().Entities.Insert(0, cachedAttack);
                SyncTags(level, cachedAttack);
            }

            level.DropLiveFromEntityPool<TowerFall.EnemyAttack>();
        }

        private static void LoadDarkPortalsSequence(this GameState gameState, Level level)
        {
            if (gameState.Entities.Enemies.DarkPortalsSequence == null)
            {
                return;
            }

            var sequence = level.GetAll<TowerFall.DarkPortalsVariantSequence>().FirstOrDefault();
            if (sequence == null)
            {
                return;
            }

            var dynSequence = DynamicData.For(sequence);
            dynSequence.Set("darkPortalsPhase", gameState.Entities.Enemies.DarkPortalsSequence.Phase);
            dynSequence.Set("darkPortalsCounter", gameState.Entities.Enemies.DarkPortalsSequence.Counter);
        }

        private static void RemoveEnemyTags(Level level, Predicate<Monocle.Entity> match)
        {
            level[GameTags.Actor].RemoveAll(match);
            level[GameTags.Enemy].RemoveAll(match);
            level[GameTags.PlayerCollider].RemoveAll(match);
            level[GameTags.ExplosionCollider].RemoveAll(match);
            level[GameTags.Target].RemoveAll(match);
            level[GameTags.LightSource].RemoveAll(match);
        }

        private static void SyncTags(Level level, Monocle.Entity entity)
        {
            foreach (GameTags tag in Enum.GetValues(typeof(GameTags)))
            {
                var tagList = level[tag];
                if (tagList == null)
                {
                    continue;
                }

                var wanted = entity.Tags.Contains(tag);
                var present = tagList.Contains(entity);

                if (wanted && !present)
                {
                    tagList.Add(entity);
                }
                else if (!wanted && present)
                {
                    tagList.RemoveAll(tagged => tagged == entity);
                }
            }
        }

        private static readonly GameTags[] ENEMY_TAGS =
        [
            GameTags.Enemy, GameTags.PlayerCollider, GameTags.ExplosionCollider, GameTags.Target
        ];

        public static void RestoreEnemyTags(this Monocle.Entity entity, bool alive)
        {
            var scene = TowerFall.TFGame.Instance.Scene;
            if (scene != null)
            {
                var dynScene = DynamicData.For(scene);
                foreach (var pending in new[] { "tagsToAdd", "tagsToRemove" })
                {
                    var queues = dynScene.Get<HashSet<Monocle.Entity>[]>(pending);
                    if (queues == null)
                    {
                        continue;
                    }

                    foreach (var queue in queues)
                    {
                        queue?.Remove(entity);
                    }
                }
            }

            foreach (var tag in ENEMY_TAGS)
            {
                if (alive)
                {
                    if (!entity.Tags.Contains(tag))
                    {
                        entity.Tags.Add(tag);
                    }
                }
                else
                {
                    entity.Tags.Remove(tag);
                }
            }
        }

        private static void LoadTeamRevivers(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.TeamReviver>();

            level[GameTags.TeamReviver].RemoveAll(entity => entity is TowerFall.TeamReviver);
            level[GameTags.LightSource].RemoveAll(entity => entity is TowerFall.TeamReviver);

            foreach (var toLoad in gameState.Entities.TeamRevivers)
            {
                var corpse = level.GetEntityByDepth(toLoad.CorpseActualDepth) as TowerFall.PlayerCorpse;
                if (corpse == null)
                {
                    continue;
                }

                var cachedTeamReviver = ServiceCollections.GetCachedEntity<TowerFall.TeamReviver>(toLoad.ActualDepth);

                if (cachedTeamReviver == null)
                {
                    cachedTeamReviver = new TowerFall.TeamReviver(corpse, (TowerFall.TeamReviver.Modes)toLoad.Mode);
                    DynamicData.For(cachedTeamReviver).Set("reviveSequenceCounter", -1f);
                }

                cachedTeamReviver.LoadState(toLoad);
                DynamicData.For(cachedTeamReviver).Set("Corpse", corpse);

                level.GetGameplayLayer().Entities.Insert(0, cachedTeamReviver);

                foreach (var tag in cachedTeamReviver.Tags)
                {
                    var tagList = level[tag];
                    if (!tagList.Contains(cachedTeamReviver))
                    {
                        tagList.Add(cachedTeamReviver);
                    }
                }
            }
        }

        private static void LoadCrackedPlatform(this GameState gameState, TowerFall.Level level)
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

        private static void LoadSpikeBall(this GameState gameState, TowerFall.Level level)
        {
            var spikeballState = gameState.Entities.Spikeball;

            if (spikeballState == null)
            {
                level.DeleteAll<TowerFall.Spikeball>();
                return;
            }

            var pendingSpikeball = level.GetAllToBeSpawned<TowerFall.Spikeball>().FirstOrDefault();

            if (pendingSpikeball != null)
            {
                pendingSpikeball.LoadState(spikeballState);
                return;
            }

            var spikeball = ServiceCollections.GetCachedEntity<TowerFall.Spikeball>(spikeballState.ActualDepth)
                ?? level.GetAll<TowerFall.Spikeball>().FirstOrDefault();

            if (spikeball == null)
            {
                return;
            }

            level.DeleteAll<TowerFall.Spikeball>();

            spikeball.LoadState(spikeballState);
            level.GetGameplayLayer().Entities.Insert(0, spikeball);

            foreach (var tag in spikeball.Tags)
            {
                var tagList = level[tag];
                if (!tagList.Contains(spikeball))
                {
                    tagList.Add(spikeball);
                }
            }
        }

        private static void LoadExplosions(this GameState gameState, TowerFall.Level level)
        {
            level.DeleteAll<TowerFall.Explosion>();

            var explosionsToLoad = gameState.Entities.Explosions.ToArray();

            foreach (var toLoad in explosionsToLoad)
            {
                var cachedExplosion = ServiceCollections.GetCachedEntity<TowerFall.Explosion>(toLoad.ActualDepth)
                    ?? Monocle.Cache.Create<TowerFall.Explosion>();

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

        private static void LoadCrackedWalls(this GameState gameState, TowerFall.Level level)
        {
            level.DeleteAll<TowerFall.CrackedWall>();

            foreach (var crackedWallToLoad in gameState.Entities.CrackedWalls.ToArray())
            {
                var cachedCrackedWall = ServiceCollections.GetCachedEntity<TowerFall.CrackedWall>(crackedWallToLoad.ActualDepth);
                var created = cachedCrackedWall == null;

                if (created)
                {
                    cachedCrackedWall = new TowerFall.CrackedWall(crackedWallToLoad.Position.ToTFVector());
                }

                var dynCachedCrackedWall = DynamicData.For(cachedCrackedWall);
                dynCachedCrackedWall.Set("Level", level);
                dynCachedCrackedWall.Set("Scene", level);

                if (created)
                {
                    cachedCrackedWall.Added();
                }

                var dynCachedCrackedWallScene = DynamicData.For(cachedCrackedWall.Scene);

                dynCachedCrackedWallScene.Invoke("TagEntityInstant", cachedCrackedWall, GameTags.Solid);
                dynCachedCrackedWallScene.Invoke("TagEntityInstant", cachedCrackedWall, GameTags.ExplosionCollider);


                cachedCrackedWall.LoadState(crackedWallToLoad);

                level.GetGameplayLayer().Entities.Insert(0, cachedCrackedWall);
            }
        }

        private static void LoadBGMushrooms(this GameState gameState, TowerFall.Level level)
        {
            var gameBGMushrooms = level.GetAll<TowerFall.BGMushroom>().ToArray();
            if (gameBGMushrooms != null && gameBGMushrooms.Length > 0)
            {
                foreach (TowerFall.BGMushroom bgMushroom in gameBGMushrooms)
                {
                    var dynBGMushroom = DynamicData.For(bgMushroom);
                    var actualDepth = dynBGMushroom.Get<double>("actualDepth");

                    var currentBGMushroom = gameState.Entities.BGMushrooms.FirstOrDefault(cp => cp.ActualDepth == actualDepth);
                    if (currentBGMushroom != null)
                    {
                        bgMushroom.LoadState(currentBGMushroom);
                    }
                }
            }
        }
        private static void LoadBackgroundAndForegroundElements(this GameState gameState, Level level)
        {
            //Background load
            foreach (BGElement toLoad in gameState.Layer.BackgroundElements.ToArray())
            {
                var gameBackground = level.GetBGElementByIndex(toLoad.Index);
                if (gameBackground != null)
                {
                    gameBackground.LoadState(toLoad);
                }
            }

            //Foreground load
            foreach (BGElement toLoad in gameState.Layer.ForegroundElements.ToArray())
            {
                var gameForeground = level.GetFGElementByIndex(toLoad.Index);

                if (gameForeground != null)
                {
                    gameForeground.LoadState(toLoad);
                }
            }
        }

        private static void LoadMovingPlatforms(this GameState gameState, Level level)
        {
            var gameMovingPlatforms = level.GetAll<TowerFall.MovingPlatform>().ToArray();
            if (gameMovingPlatforms != null && gameMovingPlatforms.Length > 0)
            {
                foreach (TowerFall.MovingPlatform movingPlatform in gameMovingPlatforms)
                {
                    var dynMovingPlatform = DynamicData.For(movingPlatform);
                    var actualDepth = dynMovingPlatform.Get<double>("actualDepth");

                    var currentMovingPlatform = gameState.Entities.MovingPlatforms.FirstOrDefault(cp => cp.ActualDepth == actualDepth);
                    if (currentMovingPlatform != null)
                    {
                        movingPlatform.LoadState(currentMovingPlatform);
                    }
                }
            }
        }

        private static void LoadBrambles(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.Brambles>();

            var dynData = new DynamicData(typeof(TowerFall.Brambles));
            var nextId = Calc.Random.Next();
            dynData.Set("nextID", nextId);
            int nextBramblesId = nextId;
            foreach (var bramble in gameState.Entities.Brambles)
            {
                var cachedBramble = ServiceCollections.GetCachedEntity<TowerFall.Brambles>(bramble.ActualDepth);

                if (cachedBramble == null)
                {
                    cachedBramble = TowerFall.Brambles.Create(nextBramblesId, bramble.Position.ToTFVector(), bramble.OwnerIndex, 0, false);
                }

                cachedBramble.LoadState(bramble);
                level.GetGameplayLayer().Entities.Insert(0, cachedBramble);

                foreach (var tag in cachedBramble.Tags)
                {
                    var tagList = level[tag];
                    if (!tagList.Contains(cachedBramble))
                    {
                        tagList.Add(cachedBramble);
                    }
                }
            }
        }

        private static void LoadIcicles(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.Icicle>();

            foreach (var toLoad in gameState.Entities.Icicles)
            {
                var cachedIcicle = ServiceCollections.GetCachedEntity<TowerFall.Icicle>(toLoad.ActualDepth);

                if (cachedIcicle == null)
                {
                    cachedIcicle = new TowerFall.Icicle(toLoad.Position.ToTFVector());
                }

                cachedIcicle.LoadState(toLoad);
                level.GetGameplayLayer().Entities.Insert(0, cachedIcicle);

                foreach (var tag in cachedIcicle.Tags)
                {
                    var tagList = level[tag];
                    if (!tagList.Contains(cachedIcicle))
                    {
                        tagList.Add(cachedIcicle);
                    }
                }
            }
        }

        private static void LoadSnowClumps(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.SnowClump>();

            foreach (var toLoad in gameState.Entities.SnowClumps)
            {
                var cachedSnowClump = ServiceCollections.GetCachedEntity<TowerFall.SnowClump>(toLoad.ActualDepth);

                if (cachedSnowClump == null)
                {
                    cachedSnowClump = new TowerFall.SnowClump(toLoad.Position.ToTFVector());
                }

                cachedSnowClump.LoadState(toLoad);
                level.GetGameplayLayer().Entities.Insert(0, cachedSnowClump);

                foreach (var tag in cachedSnowClump.Tags)
                {
                    var tagList = level[tag];
                    if (!tagList.Contains(cachedSnowClump))
                    {
                        tagList.Add(cachedSnowClump);
                    }
                }
            }
        }

        private static void LoadCrumbleBlocks(this GameState gameState, Level level)
        {
            level.DeleteAll<TowerFall.CrumbleBlock>();

            foreach (var toLoad in gameState.Entities.CrumbleBlocks)
            {
                var cachedCrumbleBlock = ServiceCollections.GetCachedEntity<TowerFall.CrumbleBlock>(toLoad.ActualDepth);

                if (cachedCrumbleBlock == null)
                {
                    cachedCrumbleBlock = new TowerFall.CrumbleBlock(toLoad.Position.ToTFVector(), toLoad.Width, toLoad.Height);
                }

                cachedCrumbleBlock.LoadState(toLoad);
                level.GetGameplayLayer().Entities.Insert(0, cachedCrumbleBlock);

                foreach (var tag in cachedCrumbleBlock.Tags)
                {
                    var tagList = level[tag];
                    if (!tagList.Contains(cachedCrumbleBlock))
                    {
                        tagList.Add(cachedCrumbleBlock);
                    }
                }
            }
        }
    }
}
