using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using TF.EX.Common.Extensions;
using TF.EX.Domain.Context;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
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
                record.GameState.SFXs = Enumerable.Empty<SFX>();
            }

            replay.Informations.PlayerDraw = _netplayManager.GetPlayerDraw();
            replay.Informations.Mode = TowerFall.Modes.LastManStanding;
            replay.Informations.MatchLenght = FrameToTimestamp(replay.Record.Count);
            replay.Informations.Archers = _netplayManager.GetArchersInfo();
            replay.Informations.Version = ServiceCollections.CurrentReplayVersion;

            var filename = $"{DateTime.UtcNow.ToString("dd'-'MM'-'yyy'T'HH'-'mm'-'ss")}.tow";

            replay.Informations.Name = filename;

            Directory.CreateDirectory(REPLAYS_FOLDER);

            var filePath = $"{REPLAYS_FOLDER}\\{filename}";

            var players = replay.Record.SelectMany(r => r.GameState.Entities.Players).ToList();

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
                await Task.Run(() =>
               {
                   var replaysFolder = $"{Directory.GetCurrentDirectory()}\\Replays";

                   var filePath = $"{replaysFolder}\\{replayFilename}";

                   Replay replay = ServiceCollections.GetCached<string, Replay>(filePath);
                   if (replay == null || replay.Record == null || replay.Record.Count == 0)
                   {
                       replay = ToReplay(filePath);
                       if (string.IsNullOrEmpty(replay.Informations.Name))
                       {
                           replay.Informations.Name = replayFilename;
                       }
                       ServiceCollections.AddToCache(filePath, replay);
                   }

                   if (replay.Record.Any())
                   {
                       _gameContext.LoadReplay(replay);

                       _netplayManager.SetPlayersIndex((int)replay.Informations.PlayerDraw);
                       _netplayManager.UpdatePlayer2Name(replay.Informations.Archers.ToArray()[1].NetplayName);

                       TFGame.Characters[0] = replay.Informations.Archers.ToArray()[0].Index; //TODO: number of players dependent
                       TFGame.Characters[1] = replay.Informations.Archers.ToArray()[1].Index;
                       TFGame.AltSelect[0] = replay.Informations.Archers.ToArray()[0].Type;
                       TFGame.AltSelect[1] = replay.Informations.Archers.ToArray()[1].Type;

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

        //private byte[] ToBytes(Replay replay)
        //{
        //    using (var memoryStream = new MemoryStream())
        //    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        //    using (var streamWriter = new StreamWriter(gzipStream, Encoding.UTF8))
        //    using (var jsonWriter = new JsonTextWriter(streamWriter))
        //    {
        //        var serializer = new JsonSerializer();
        //        serializer.Serialize(jsonWriter, replay);
        //        return memoryStream.ToArray();
        //    }
        //}

        private void WriteToFile(Replay replay, Stream stream)
        {
            using var gzipStream = new GZipStream(stream, CompressionMode.Compress);
            using var streamWriter = new StreamWriter(gzipStream, Encoding.UTF8);
            using var jsonWriter = new JsonTextWriter(streamWriter);

            var serializer = new JsonSerializer();
            serializer.Serialize(jsonWriter, replay);
        }

        private Replay ToReplay(string filePath, bool shouldInfoOnly = false)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open);
            using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
            using var sr = new StreamReader(gzip, Encoding.UTF8);
            using var reader = new JsonTextReader(sr);

            var serializer = shouldInfoOnly ?
                JsonSerializer.Create(new JsonSerializerSettings
                {
                    ContractResolver = new ReplayContractResolver()
                }) :
                JsonSerializer.CreateDefault();
            return serializer.Deserialize<Replay>(reader);
        }

        public void Reset()
        {
            _gameContext.ResetReplay();
        }

        public IEnumerable<Replay> LoadAndGetReplays()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var replays = Directory.EnumerateFiles(REPLAYS_FOLDER)
                .Where(f => f.EndsWith(".tow"))
                .Select(f => Path.GetFileName(f)).ToList();

            var res = new ConcurrentBag<Replay>();

            int i = 0;
            int obsoleteReplays = 0;
            Loader.Message = $"LOADING REPLAYS... \n\n                {i}/{replays.Count}";

            Parallel.ForEach(replays, replay =>
            {
                var replayPath = $"{REPLAYS_FOLDER}\\{replay}";

                var replayWithInfo = ServiceCollections.GetCached<string, Replay>(replayPath);
                if (replayWithInfo == null)
                {
                    try
                    {
                        replayWithInfo = ToReplay(replayPath, true);
                        if (replayWithInfo.Informations.Version != ServiceCollections.CurrentReplayVersion)
                        {
                            _logger.LogDebug<ReplayService>($"Replay {replay} is obsolete, will be renamed and ignored");
                            Interlocked.Increment(ref obsoleteReplays);

                            File.Move(replayPath, $"{replayPath}.obsolete");

                            return;
                        }

                        replayWithInfo.Informations.Name = replay;

                        ServiceCollections.AddToCache(replayPath, replayWithInfo);

                        res.Add(replayWithInfo);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError<ReplayService>($"Error while loading replay {replay} , replay will be deleted", e);
                        File.Delete(replayPath);
                    }
                }
                else
                {
                    res.Add(replayWithInfo);
                }

                Interlocked.Increment(ref i);
                Interlocked.Exchange(ref Loader.Message, $"LOADING REPLAYS... \n\n                {i}/{replays.Count}");
            });

            stopwatch.Stop();

            _logger.LogDebug<ReplayService>($"Loading replay took {stopwatch.ElapsedMilliseconds / 1000}s");

            if (obsoleteReplays > 0)
            {
                _logger.LogDebug<ReplayService>($"{obsoleteReplays} replays are obsolete. A future update might add the ability to migrate from earlier version");
            }

            return res.OrderBy(replay => replay.Informations.Name);
        }
    }
}
