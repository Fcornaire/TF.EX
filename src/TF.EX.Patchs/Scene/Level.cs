using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Arrows;
using TF.EX.Domain.Models.State.LevelEntity.Chest;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Entity.LevelEntity;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    public class LevelPatch : IHookable, IStateful<TowerFall.Level, GameState>
    {
        private readonly ISessionService _sessionService;
        private readonly INetplayManager _netplayManager;
        private readonly IOrbService _orbService;
        private readonly IRngService _rngService;
        private readonly IHUDService _hudService;
        private readonly IInputService _inputService;

        private TF.EX.Domain.Models.Modes _currentMode;

        private PlayerPatch _playerPatch;
        private IStateful<TowerFall.PlayerCorpse, Domain.Models.State.PlayerCorpse> _playerCorpsePatch;
        private ArrowPatch _arrowPatch;
        private IStateful<TowerFall.TreasureChest, Chest> _chestPatch;
        private PickupPatch _pickupPatch;
        private IStateful<TowerFall.Lantern, Domain.Models.State.LevelEntity.Lantern> _lanternPatch;
        private IStateful<TowerFall.Chain, Domain.Models.State.LevelEntity.Chain> _chainPatch;
        private OrbLogicPatch _orbLogicPatch;
        public static bool _isLoaded = false;

        private Random random = new Random();

        public LevelPatch(
            ISessionService sessionService,
            INetplayManager netplayManager,
            IOrbService orbService,
            IRngService rngService,
            IHUDService hudService,
            IInputService inputService
            )
        {
            _sessionService = sessionService;
            _netplayManager = netplayManager;
            _orbService = orbService;
            _rngService = rngService;
            _hudService = hudService;
            _inputService = inputService;
        }

        public void Load()
        {
            On.TowerFall.Level.HandlePausing += Level_HandlePausing;
            On.TowerFall.Level.Update += Level_Update;
        }

        public void Unload()
        {
            On.TowerFall.Level.HandlePausing -= Level_HandlePausing;
            On.TowerFall.Level.Update -= Level_Update;
        }

        private void Level_Update(On.TowerFall.Level.orig_Update orig, TowerFall.Level self)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                EnsureService();
            }

            self.AdjustSFX();

            var dynTFGame = DynamicData.For(TFGame.Instance);
            dynTFGame.Set("TimeMult", _orbService.GetOrb().Time.EngineTimeMult); //TODO load only

            //if (_netplayManager.IsTestMode())
            //{
            //    Console.WriteLine(self.FrameCounter);
            //}


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
        }

        private void Level_HandlePausing(On.TowerFall.Level.orig_HandlePausing orig, TowerFall.Level self)
        {
            //if (_netplayManager.IsReplayMode())
            //{
            //    orig(self);
            //}
        }

        private void UpdateLayersEntityList(TowerFall.Level level)
        {
            var gameplayLayer = level.GetGameplayLayer();
            var dynGameplayLayer = DynamicData.For(gameplayLayer);
            dynGameplayLayer.Invoke("UpdateEntityList");

            var versusStartLayer = level.GetVersusStartLayer();
            var dynVersusStartLayer = DynamicData.For(versusStartLayer);
            dynVersusStartLayer.Invoke("UpdateEntityList");
        }

        private void AddPlayersIndicators(TowerFall.Level self)
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

                        //dynPlayer.Set("Indicator", indicator);


                        player.Add(indicator);
                    }
                }
            }
        }

        public GameState GetState(Level entity)
        {
            var gameState = new GameState();

            //Versus Start
            gameState.Hud = _hudService.Get();

            //Players save state
            var playersState = new List<TF.EX.Domain.Models.State.Player>();
            if (entity[GameTags.Player] != null)
            {
                var players = entity[GameTags.Player];
                foreach (TowerFall.Player player in players.ToArray())
                {
                    playersState.Add(_playerPatch.GetState(player));
                }
            }
            gameState.Players = playersState;

            //PlayerCorpse save
            var playerCorpsesState = new List<TF.EX.Domain.Models.State.PlayerCorpse>();
            if (entity[GameTags.Corpse] != null && entity[GameTags.Corpse].Count > 0)
            {
                var gamePlayerCorpses = entity[GameTags.Corpse].ToArray();
                foreach (TowerFall.PlayerCorpse playerCorpse in gamePlayerCorpses)
                {
                    var pc = _playerCorpsePatch.GetState(playerCorpse);
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
                    arrowsState.Add(_arrowPatch.GetState(arrow));
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
                    var che = _chestPatch.GetState(chest);
                    chestsState.Add(che);
                    ServiceCollections.AddToCache(che.ActualDepth, chest);
                }
            }
            gameState.Chests = chestsState;

            //Pickup save state
            var pickupsState = new List<TF.EX.Domain.Models.State.LevelEntity.Chest.Pickup>();
            var gamePickups = entity.GetPickups().ToArray();
            if (gamePickups != null && gamePickups.Length > 0)
            {
                foreach (TowerFall.Pickup pickup in gamePickups)
                {
                    var pick = _pickupPatch.GetState(pickup);
                    pickupsState.Add(pick);
                    ServiceCollections.AddToCache(pick.ActualDepth, pickup);
                }
            }
            gameState.Pickups = pickupsState;

            //Session save
            if (_currentMode.IsNetplay() || _netplayManager.IsTestMode())
            {
                var dynLMRoundLogic = DynamicData.For(entity.Session.RoundLogic);
                var roundEndCounter = dynLMRoundLogic.Get<RoundEndCounter>("roundEndCounter");
                var endCounter = DynamicData.For(roundEndCounter).Get<float>("endCounter");
                var roundStarted = entity.Session.RoundLogic.RoundStarted;

                var isEnding = entity.Session.CurrentLevel.Ending;

                var dynRoundLogic = DynamicData.For(entity.Session.RoundLogic);
                var miasmaCounter = dynRoundLogic.Get<Counter>("miasmaCounter");
                var counter = DynamicData.For(miasmaCounter).Get<float>("counter");

                var miasma = entity.GetMiasma();
                var miasmaTentacleSineWave = 0.0f;
                var miasmaSineWave = 0.0f;
                if (miasma != null)
                {
                    var dynMiasma = DynamicData.For(miasma);
                    var miasmaTentacle = dynMiasma.Get<SineWave>("tentaclesSine");
                    miasmaTentacleSineWave = miasmaTentacle.Counter;
                    var miasmaSine = dynMiasma.Get<SineWave>("sine");
                    miasmaSineWave = miasmaSine.Counter;
                }

                var stateSession = _sessionService.GetSession();
                stateSession.RoundStarted = roundStarted;
                stateSession.RoundEndCounter = endCounter;
                stateSession.IsEnding = isEnding;
                stateSession.Miasma.Counter = counter;
                stateSession.Miasma.Percent = miasma != null ? miasma.Percent : Constants.DEFAULT_MIASMA_PERCENT;
                stateSession.Miasma.IsCollidable = miasma != null ? miasma.Collidable : false;
                stateSession.Miasma.SineCounter = miasmaSineWave;
                stateSession.Miasma.SineTentaclesWaveCounter = miasmaTentacleSineWave;
                var session = new Domain.Models.State.Session
                {
                    IsEnding = stateSession.IsEnding,
                    RoundEndCounter = stateSession.RoundEndCounter,
                    RoundStarted = stateSession.RoundStarted,
                    Miasma = new Domain.Models.State.Miasma
                    {
                        Counter = stateSession.Miasma.Counter,
                        Percent = stateSession.Miasma.Percent,
                        IsCollidable = stateSession.Miasma.IsCollidable,
                        SineCounter = stateSession.Miasma.SineCounter,
                        SineTentaclesWaveCounter = stateSession.Miasma.SineTentaclesWaveCounter
                    }
                };

                _sessionService.SaveSession(stateSession);
                gameState.Session = session;
            }

            //Orbs save
            var orb = _orbService.GetOrb();

            orb.Time.EngineTimeRate = TFGame.TimeRate;
            orb.Time.EngineTimeMult = TFGame.TimeRate; //In Fixed time step, TimeRate = TimeMult

            _orbService.Save(orb);
            gameState.Orb = orb;

            //Lantern save
            var lanternsState = new List<TF.EX.Domain.Models.State.LevelEntity.Lantern>();
            var gameLanterns = entity.GetLanterns().ToArray();
            if (gameLanterns != null && gameLanterns.Length > 0)
            {
                foreach (TowerFall.Lantern lantern in gameLanterns)
                {
                    var lan = _lanternPatch.GetState(lantern);
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
                    var ch = _chainPatch.GetState(chain);
                    chainsState.Add(ch);
                }
            }
            gameState.Chains = chainsState;

            //Background save
            var bgElements = GetBGElements(entity).ToArray();
            List<BackgroundElement> bgs = new List<BackgroundElement>();
            for (int i = 0; i < bgElements.Length; i++)
            {
                TowerFall.Background.BGElement bg = bgElements[i];
                if (bg is TowerFall.Background.ScrollLayer)
                {
                    var bgModel = (bg as TowerFall.Background.ScrollLayer).ToModel(i);
                    bgs.Add(bgModel);
                }
            }
            gameState.BackgroundElements = bgs;

            entity.ClearHats(); //TODO: Remove and save hats

            var dynGameplayLayer = DynamicData.For(entity.GetGameplayLayer());
            var actualDepthLookup = dynGameplayLayer.Get<Dictionary<int, double>>("actualDepthLookup");
            actualDepthLookup.Remove(-52);

            actualDepthLookup.Remove(-600); //TODO: Miasma

            _sessionService.SaveGamePlayLayerActualDepthLookup(actualDepthLookup);
            gameState.GamePlayerLayerActualDepthLookup = actualDepthLookup;

            gameState.Rng = _rngService.Get();
            gameState.Frame = (int)entity.FrameCounter;

            return gameState;
        }

        public void LoadState(GameState gameState, Level level)
        {
            var dynGameplayLayer = DynamicData.For(level.GetGameplayLayer());
            dynGameplayLayer.Set("actualDepthLookup", gameState.GamePlayerLayerActualDepthLookup);

            var dynScene = DynamicData.For(level as Monocle.Scene);
            dynScene.Set("FrameCounter", gameState.Frame);

            //Players
            foreach (TF.EX.Domain.Models.State.Player toLoad in gameState.Players.ToArray())
            {
                var gamePlayer = level.GetPlayer(toLoad.Index);
                if (gamePlayer != null)
                {
                    _playerPatch.LoadState(toLoad, gamePlayer);
                }
                else
                {
                    TowerFall.Level lvl = level as TowerFall.Level;

                    TowerFall.Player player =
                    new TowerFall.Player(
                        toLoad.Index,
                        toLoad.Position.ToTFVector(),
                        TowerFall.Allegiance.Neutral,
                        TowerFall.Allegiance.Neutral,
                        lvl.Session.GetPlayerInventory(toLoad.Index),
                        lvl.Session.GetSpawnHatState(toLoad.Index),
                        frozen: false,
                        flash: false,
                    indicator: false);

                    _playerPatch.LoadState(toLoad, player);
                    level.GetGameplayLayer().Entities.Insert(0, player);
                }
            }

            //Arrows
            if (level[GameTags.Arrow] != null)
            {
                level.ClearArrows();

                foreach (TF.EX.Domain.Models.State.Arrows.Arrow toLoad in gameState.Arrows.ToArray())
                {
                    TowerFall.LevelEntity entityHavingArrow = level.GetPlayerOrCorpse(toLoad.PlayerIndex);
                    if (entityHavingArrow == null)
                    {
                        throw new InvalidOperationException("Can't find the original player that shoot the current arrow being loaded");
                    }

                    var arrow = TowerFall.Arrow.Create(toLoad.ArrowType.ToTFModel(), entityHavingArrow, toLoad.Position.ToTFVector(), toLoad.Direction);
                    _arrowPatch.LoadState(toLoad, arrow);

                    level.GetGameplayLayer().Entities.Insert(0, arrow);
                }
            }

            //PlayerCorpses
            level.ClearPlayerCorpses();
            var corpsesToLoad = gameState.PlayerCorpses.ToArray();

            foreach (TF.EX.Domain.Models.State.PlayerCorpse toLoad in corpsesToLoad)
            {
                var cachedPlayerCorpse = ServiceCollections.GetCached<TowerFall.PlayerCorpse>(toLoad.ActualDepth);

                _playerCorpsePatch.LoadState(toLoad, cachedPlayerCorpse);

                level.GetGameplayLayer().Entities.Insert(0, cachedPlayerCorpse);
            }

            //Session load
            var session = gameState.Session;
            _sessionService.SaveSession(session);

            var dynRoundLogic = DynamicData.For(level.Session.RoundLogic);
            dynRoundLogic.Set("RoundStarted", session.RoundStarted);

            (level as TowerFall.Level).Session.CurrentLevel.Ending = session.IsEnding;

            if (session.Miasma.Counter > 0)
            {
                var miasma = level.GetMiasma();
                if (miasma != null)
                {
                    level.GetGameplayLayer().Entities.Remove(miasma);
                    miasma.Removed();
                }
            }

            if (session.Miasma.State != MiasmaState.Uninitialized && session.Miasma.Counter <= 0)
            {
                var miasma = (level as TowerFall.Level).GetMiasma();

                if (miasma != null)
                {
                    MiasmaPatch.LoadState(miasma, session.Miasma);
                }
            }

            //Chests load
            if (level != null && level[GameTags.TreasureChest] != null && level[GameTags.TreasureChest].Count > 0)
            {
                level.ClearChests();

                foreach (var chestToLoad in gameState.Chests.ToArray())
                {
                    var cachedChest = ServiceCollections.GetCached<TreasureChest>(chestToLoad.ActualDepth);

                    _chestPatch.LoadState(chestToLoad, cachedChest);

                    level.GetGameplayLayer().Entities.Insert(0, cachedChest);
                }
            }

            //Pickups load
            level.ClearPickups();

            foreach (var pickupToLoad in gameState.Pickups.ToArray())
            {
                var cachedPickup = ServiceCollections.GetCached<TowerFall.Pickup>(pickupToLoad.ActualDepth);

                _pickupPatch.LoadState(pickupToLoad, cachedPickup);

                level.GetGameplayLayer().Entities.Insert(0, cachedPickup);
            }

            //Orbs load
            var orb = gameState.Orb;
            _orbService.Save(orb);

            TFGame.TimeRate = orb.Time.EngineTimeRate;
            var lavaControl = (level as TowerFall.Level).GetLavaControl();
            if (orb.Lava.IsDefault())
            {
                if (lavaControl != null)
                {
                    var lavas = level.GetLavas();

                    foreach (var lava in lavas)
                    {
                        level.GetGameplayLayer().Entities.Remove(lava);
                        lava.Removed();
                    }

                    level.GetGameplayLayer().Entities.Remove(lavaControl);
                    lavaControl.Removed();
                }
            }
            _orbLogicPatch.LoadState(level.OrbLogic, orb);

            //Lantern load
            foreach (TF.EX.Domain.Models.State.LevelEntity.Lantern toLoad in gameState.Lanterns.ToArray())
            {
                var gameLantern = GetEntityByDepth(level, toLoad.ActualDepth) as Lantern;

                if (gameLantern != null)
                {
                    _lanternPatch.LoadState(toLoad, gameLantern);
                }
            }

            //Chain load
            foreach (TF.EX.Domain.Models.State.LevelEntity.Chain toLoad in gameState.Chains.ToArray())
            {
                var gameChain = GetEntityByDepth(level, toLoad.ActualDepth) as Chain;

                if (gameChain != null)
                {
                    _chainPatch.LoadState(toLoad, gameChain);
                }
            }

            //Background load
            foreach (TF.EX.Domain.Models.State.BackgroundElement toLoad in gameState.BackgroundElements.ToArray())
            {
                var gameBackground = GetBGElementByIndex(level, toLoad.index);

                if (gameBackground != null && gameBackground is Background.ScrollLayer)
                {
                    (gameBackground as Background.ScrollLayer).Image.Position = toLoad.Position.ToTFVector();
                }
            }

            //Rng
            var rng = gameState.Rng;
            rng.ResetRandom();
            _rngService.UpdateState(rng.Gen_type);

            //VersusStart

            level.ClearVersusStart();
            _hudService.Update(new Domain.Models.State.HUD.HUD());

            if (gameState.Hud.VersusStart.CoroutineState > 0)
            {
                var versusStart = new TowerFall.VersusStart(level.Session);
                var dynVersusStart = DynamicData.For(versusStart);
                dynVersusStart.Set("Scene", level);
                dynVersusStart.Set("Level", level);

                level.Layers.FirstOrDefault(l => l.Value.Index == versusStart.LayerIndex).Value.Entities.Add(versusStart);
                versusStart.Added();

                var coroutine = versusStart.Components.FirstOrDefault(c => c is Monocle.Coroutine);

                for (int i = 0; i < gameState.Hud.VersusStart.CoroutineState; i++)
                {
                    coroutine.Update();
                }
            }

            PostLoad(level, gameState);

            level.Sort(CompareDepth);
        }

        private int CompareDepth(Monocle.Entity a, Monocle.Entity b)
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
        /// <para>the hack is to let them load first normally and them finish the remaining piece (Death arrow + CannotHit)</para>
        /// </summary>
        private void PostLoad(TowerFall.Level self, GameState gs)
        {
            foreach (TF.EX.Domain.Models.State.Player toLoad in gs.Players.ToArray())
            {
                var gamePlayer = self.GetPlayer(toLoad.Index);

                _playerPatch.LoadDeathArrow(gamePlayer, toLoad.DeathArrowDepth);
            }

            foreach (TF.EX.Domain.Models.State.Arrows.Arrow toLoad in gs.Arrows.ToArray())
            {
                var arrow = this.GetEntityByDepth(self, toLoad.ActualDepth) as TowerFall.Arrow;
                _arrowPatch.LoadCannotHit(arrow, toLoad.HasUnhittableEntity, toLoad.PlayerIndex);
            }
        }
        private List<Background.BGElement> GetBGElements(TowerFall.Level level)
        {
            var dynBacground = DynamicData.For(level.Background);
            return dynBacground.Get<List<Background.BGElement>>("elements");
        }

        private Background.BGElement GetBGElementByIndex(TowerFall.Level level, int index) { return GetBGElements(level)[index]; }

        public Monocle.Entity GetEntityByDepth(TowerFall.Level self, double actualDepth)
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

        private void EnsureService()
        {
            _playerPatch = ServiceCollections.ServiceProvider.GetPlayerPatch();
            _playerCorpsePatch = ServiceCollections.ServiceProvider.GetPlayerCorpsePatch();
            _arrowPatch = ServiceCollections.ServiceProvider.GetArrowPatch();
            _chestPatch = ServiceCollections.ServiceProvider.GetTreasureChestPatch();
            _pickupPatch = ServiceCollections.ServiceProvider.GetPickupPatch();
            _lanternPatch = ServiceCollections.ServiceProvider.GetLanternPatch();
            _chainPatch = ServiceCollections.ServiceProvider.GetChainPatch();
            _orbLogicPatch = ServiceCollections.ServiceProvider.GetOrbLogicPatch();
            (_, _currentMode) = ServiceCollections.ResolveStateMachineService();
        }
    }
}
