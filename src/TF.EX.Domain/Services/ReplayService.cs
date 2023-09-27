using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TF.EX.Common.Extensions;
using TF.EX.Domain.Context;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Domain.Utils;
using TowerFall;
using static TowerFall.MatchSettings;

namespace TF.EX.Domain.Services
{
    public class ReplayService : IReplayService
    {
        private readonly IGameContext _gameContext;
        private readonly IInputService _inputService;
        private readonly IRngService _rngService;
        private readonly INetplayManager _netplayManager;
        private readonly ILogger _logger;
        private int currentReplayFrame = 0;

        private string REPLAYS_FOLDER => $"{Directory.GetCurrentDirectory()}\\Replays";

        public ReplayService(IGameContext gameContext,
            IInputService inputService,
            IRngService rngService,
            INetplayManager netplayManager,
            ILogger logger)
        {
            _gameContext = gameContext;
            _inputService = inputService;
            _rngService = rngService;
            _netplayManager = netplayManager;
            _logger = logger;
        }

        public void AddRecord(GameState gameState, bool shouldSwapPlayer)
        {
            _gameContext.AddRecord(gameState, shouldSwapPlayer);
        }

        private static TimeSpan FrameToTimestamp(int frame)
        {
            return TimeSpan.FromSeconds(frame / 60);
        }

        public void Export()
        {
            var replay = _gameContext.GetReplay();

            if (replay == null)
            {
                return;
            }

            //Clear every sfx in record to reduce replay size
            foreach (var record in replay.Record)
            {
                record.GameState.SFXs = Enumerable.Empty<SFXState>();
            }

            replay.Informations.PlayerDraw = _netplayManager.GetPlayerDraw();
            replay.Informations.Mode = Models.Modes.LastManStanding;
            replay.Informations.MatchLenght = FrameToTimestamp(replay.Record.Count);
            replay.Informations.Archers = _netplayManager.GetArchersInfo();
            replay.Informations.Version = ServiceCollections.CurrentReplayVersion;

            var filename = $"{DateTime.UtcNow.ToString("dd'-'MM'-'yyy'T'HH'-'mm'-'ss")}.tow";

            replay.Informations.Name = filename;

            Directory.CreateDirectory(REPLAYS_FOLDER);

            var filePath = $"{REPLAYS_FOLDER}\\{filename}";

            using var fileStream = new FileStream(filePath, FileMode.Create);
            WriteToFile(replay, fileStream);

            _gameContext.ResetReplay();
        }

        public void Initialize()
        {
            _gameContext.InitializeReplay(MainMenu.VersusMatchSettings.LevelSystem.ID.X);
        }

        public void RemovePredictedRecords(int frame)
        {
            _gameContext.RemovePredictedRecords(frame);
        }

        public async Task LoadAndStart(string replayFilename)
        {
            try
            {
                await Task.Run(async () =>
               {
                   var replaysFolder = $"{Directory.GetCurrentDirectory()}\\Replays";

                   var filePath = $"{replaysFolder}\\{replayFilename}";

                   var isCached = ServiceCollections.GetCached(filePath, out Replay replay);
                   if (!isCached || replay == null || replay.Record.Count == 0)
                   {
                       replay = await ToReplay(filePath);
                       ServiceCollections.AddToCache(filePath, replay, TimeSpan.FromMinutes(5));
                   }

                   if (replay.Record.Any())
                   {
                       _gameContext.LoadReplay(replay);

                       _netplayManager.SetPlayersIndex((int)replay.Informations.PlayerDraw);
                       _netplayManager.UpdatePlayer2Name(replay.Informations.Archers.ToArray()[1].NetplayName);

                       var player1Index = replay.Informations.PlayerDraw == PlayerDraw.Player1 ? 0 : 1;
                       var player2Index = replay.Informations.PlayerDraw == PlayerDraw.Player1 ? 1 : 0;

                       var archers = replay.Informations.Archers.ToArray();

                       TFGame.Characters[0] = archers[player1Index].Index; //TODO: number of players dependent
                       TFGame.Characters[1] = archers[player2Index].Index;
                       TFGame.AltSelect[0] = (ArcherData.ArcherTypes)archers[player1Index].Type;
                       TFGame.AltSelect[1] = (ArcherData.ArcherTypes)archers[player2Index].Type;

                       currentReplayFrame = 0;
                       var firstRecord = replay.Record.First(rec => rec.GameState.Entities.Players.Count > 0);
                       _rngService.SetSeed(firstRecord.GameState.Rng.Seed);

                       for (int i = 0; i < firstRecord.GameState.Entities.Players.Count(); i++)
                       {
                           TFGame.Players[i] = true;
                       }

                       MatchSettings matchSettings = new MatchSettings(GameData.VersusTowers[replay.Informations.Id].GetLevelSystem(), TowerFall.Modes.LastManStanding, MatchLengths.Standard);
                       MainMenu.VersusMatchSettings = matchSettings;

                       new TowerFall.Session(matchSettings).StartGame();
                   }
               });
            }
            catch (Exception e)
            {
                _logger.LogError<ReplayService>($"Error while loading replay {replayFilename}", e);
            }
        }

        //TODO: Properly implement this
        public void GoTo(int numbreOfFrames)
        {
            //currentReplayFrame += numbreOfFrames;

            //if (currentReplayFrame < 1)
            //{
            //    currentReplayFrame = 1;
            //}

            //var replay = GetReplay();

            //if (replay.Record.Any())
            //{
            //    var goToRecord = replay.Record.First(r => r.GameState.Frame == currentReplayFrame);
            //    _gameStateService.LoadState(TFGame.Instance.Scene, goToRecord.GameState);
            //    _inputService.UpdateCurrent(goToRecord.Inputs.Select(input => input.ToTFInput()));
            //    _netplayManager.UpdateFramesToReSimulate(1);
            //}
        }

        public void RunFrame()
        {
            Record record = _gameContext.GetCurrentReplayFrame(currentReplayFrame);

            if (record != null)
            {
                _inputService.UpdateCurrent(record.Inputs.Select(input => input.ToTFInput()));
                //_gameStateService.LoadState(Engine.Instance.Scene, record.GameState);
            }

            currentReplayFrame++;
        }

        public Replay GetReplay()
        {
            return _gameContext.GetReplay();
        }

        public Record GetCurrentRecord()
        {
            var replay = GetReplay();

            return replay.Record.FirstOrDefault(r => r.GameState.Frame == currentReplayFrame);
        }

        private void WriteToFile(Replay replay, Stream stream)
        {
            using var gzipStream = new GZipStream(stream, CompressionMode.Compress);
            JsonSerializer.Serialize(gzipStream, replay);
        }

        public static async Task<Replay> ToReplay(string filePath, bool shouldIgnoreRecord = false)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();

            if (shouldIgnoreRecord)
            {
                var modifier = new IgnorePropertiesWithType(typeof(List<Record>));

                options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { modifier.ModifyTypeInfo }
                };
            }

            using var fileStream = new FileStream(filePath, FileMode.Open);
            using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
            return await JsonSerializer.DeserializeAsync<Replay>(gzip, options);
        }

        public void Reset()
        {
            _gameContext.ResetReplay();
        }

        public async Task<IEnumerable<Replay>> LoadAndGetReplays()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var replays = Directory.EnumerateFiles(REPLAYS_FOLDER)
                .Where(f => f.EndsWith(".tow"))
                .Select(f => Path.GetFileName(f)).ToList();

            var res = new ConcurrentBag<Replay>();

            int loaded = 0;
            int obsoleteReplays = 0;
            int impossibleToLoad = 0;
            Loader.Message = $"LOADING REPLAYS... \n\n                {loaded}/{replays.Count}";

            //TODO: There should be a better way to load replays
            await replays.ForEachAsync(4, async replay =>
            {
                var replayPath = $"{REPLAYS_FOLDER}\\{replay}";

                var isCached = ServiceCollections.GetCached<Replay>(replayPath, out var replayRecordless);
                if (!isCached)
                {
                    var attempt = 1;
                    bool loadedReplay = false;
                    while (!loadedReplay)
                    {
                        try
                        {
                            _logger.LogDebug<ReplayService>($"Loading replay {replay} (attemp {attempt})");
                            replayRecordless = await ToReplay(replayPath, true);

                            loadedReplay = true;

                            if (replayRecordless.Informations.Version != ServiceCollections.CurrentReplayVersion)
                            {
                                _logger.LogDebug<ReplayService>($"Replay {replay} is obsolete, will be renamed and ignored");

                                Interlocked.Increment(ref obsoleteReplays);

                                File.Move(replayPath, $"{replayPath}.obsolete");

                                return;
                            }

                            ServiceCollections.AddToCache(replayPath, replayRecordless, TimeSpan.FromMinutes(15));

                            res.Add(replayRecordless);
                        }
                        catch (OutOfMemoryException e)
                        {
                            attempt++;
                            _logger.LogError<ReplayService>($"Error while loading replay {replay}", e);
                            await Task.Delay(500);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError<ReplayService>($"Error while loading replay {replay}", e);
                            break;
                        }
                    }

                    if (!loadedReplay)
                    {
                        _logger.LogError<ReplayService>($"Impossible to load replay {replay}");
                        Interlocked.Increment(ref impossibleToLoad);
                    }
                }
                else
                {
                    res.Add(replayRecordless);
                }

                Interlocked.Increment(ref loaded);
                Interlocked.Exchange(ref Loader.Message, $"LOADING REPLAYS... \n\n                {loaded}/{replays.Count}");
            });

            stopwatch.Stop();

            _logger.LogDebug<ReplayService>($"Loading replays took {stopwatch.ElapsedMilliseconds / 1000}s");

            if (obsoleteReplays > 0)
            {
                _logger.LogDebug<ReplayService>($"{obsoleteReplays} replays are obsolete. A future update might add the ability to migrate from earlier version");
            }

            if (impossibleToLoad > 0)
            {
                _logger.LogDebug<ReplayService>($"{impossibleToLoad} replays could not be loaded");
            }

            return res.OrderBy(replay => replay.Informations.Name);
        }
    }
}
