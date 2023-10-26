using MessagePack;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using TF.EX.Common.Extensions;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
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
            replay.Informations.MatchLenght = FrameToTimestamp(replay.Record.Count);
            replay.Informations.Archers = _netplayManager.GetArchersInfo();

            var filename = $"{DateTime.UtcNow.ToString("dd'-'MM'-'yyy'T'HH'-'mm'-'ss")}.tow";

            replay.Informations.Name = filename;

            Directory.CreateDirectory(REPLAYS_FOLDER);

            var filePath = $"{REPLAYS_FOLDER}\\{filename}";

            using var fileStream = new FileStream(filePath, FileMode.Create);
            WriteToFile(replay, fileStream);

            _gameContext.ResetReplay();
        }

        public void Initialize(Domain.Models.WebSocket.GameData gameData = null)
        {
            _gameContext.InitializeReplay(MainMenu.VersusMatchSettings.LevelSystem.ID.X, gameData);
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

                   ServiceCollections.GetCached(filePath, out Replay replay);
                   if (replay == null || (replay != null && replay.Record == null)) //null-conditional member access somehow not working ?
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
                       matchSettings.Variants.ApplyVariants(replay.Informations.Variants);
                       matchSettings.MatchLength = (MatchSettings.MatchLengths)replay.Informations.VersusMatchLength;
                       MainMenu.VersusMatchSettings = matchSettings;

                       _netplayManager.SetReplayMode();

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
            MessagePackSerializer.Serialize(stream, replay, Common.SerializationOptions.GetDefaultOptionWithCompression());
        }

        public static async Task<Replay> ToReplay(string filePath, bool shouldIgnoreRecord = false)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open);
            if (shouldIgnoreRecord)
            {
                return await MessagePackSerializer.DeserializeAsync<Replay>(fileStream, SerializationOptions.GetDefaultOptionWithIgnore());
            }

            return await MessagePackSerializer.DeserializeAsync<Replay>(fileStream, Common.SerializationOptions.GetDefaultOptionWithCompression());
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

            await replays.ForEachAsync(10, async replay =>
            {
                var replayPath = $"{REPLAYS_FOLDER}\\{replay}";

                var isCached = ServiceCollections.GetCached<Replay>(replayPath, out var replayRecordless);
                if (!isCached)
                {
                    var attempt = 0;
                    while (attempt < 3)
                    {
                        try
                        {
                            replayRecordless = await ToReplay(replayPath, true);

                            if (replayRecordless.Informations.Version != ServiceCollections.CurrentReplayVersion)
                            {
                                _logger.LogDebug<ReplayService>($"Replay {replay} is obsolete, will be renamed and ignored");

                                Interlocked.Increment(ref obsoleteReplays);

                                File.Move(replayPath, $"{replayPath}.obsolete");

                                return;
                            }

                            ServiceCollections.AddToCache(replayPath, replayRecordless, TimeSpan.FromMinutes(10));

                            res.Add(replayRecordless);
                            break;
                        }
                        catch (Exception e)
                        {
                            attempt++;
                            _logger.LogError<ReplayService>($"Error while loading replay {replay}", e);
                            await Task.Delay(500);
                            _logger.LogDebug<ReplayService>($"Loading replay {replay} (attemp {attempt + 1})");
                        }
                    }

                    if (attempt >= 3)
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
