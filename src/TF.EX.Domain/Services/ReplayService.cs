using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;
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
        private int currentReplayFrame = 1;

        private string REPLAYS_FOLDER => $"{Directory.GetCurrentDirectory()}\\Replays";


        public ReplayService(IGameContext gameContext, IInputService inputService, IRngService rngService, INetplayManager netplayManager)
        {
            _gameContext = gameContext;
            _inputService = inputService;
            _rngService = rngService;
            _netplayManager = netplayManager;
        }

        public void AddRecord(GameState gameState, bool shouldSwapPlayer)
        {
            _gameContext.AddRecord(gameState, shouldSwapPlayer);
        }

        public void Export()
        {
            var replay = _gameContext.GetReplay();

            if (replay == null)
            {
                return;
            }

            replay.Informations.PlayerDraw = _netplayManager.GetPlayerDraw();

            var filename = $"{DateTime.UtcNow.ToString("dd'-'MM'-'yyy'T'HH'-'mm'-'ss")}.tow";
            //var filenameJson = $"{DateTime.Now.ToString("dd'-'MM'-'yyy'T'HH'-'mm'-'ss")}_players.json";

            Directory.CreateDirectory(REPLAYS_FOLDER);

            var filePath = $"{REPLAYS_FOLDER}\\{filename}";
            //var filePathJson = $"{REPLAYS_FOLDER}\\{filenameJson}";

            var players = replay.Record.SelectMany(r => r.GameState.Entities.Players).ToList();

            //var json = JsonConvert.SerializeObject(players, Formatting.Indented);
            //File.WriteAllText(filePathJson, json);

            //File.WriteAllBytes(filePath, ToBytes(replay));
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

        public void LoadAndStart(string replayFilename)
        {
            var replaysFolder = $"{Directory.GetCurrentDirectory()}\\Replays";

            var filePath = $"{replaysFolder}\\{replayFilename}";

            var replay = ToReplay(filePath);

            _gameContext.LoadReplay(replay);

            _netplayManager.SetPlayersIndex((int)replay.Informations.PlayerDraw);

            if (replay.Record.Any())
            {
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

        private Replay ToReplay(string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open);
            using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
            using var sr = new StreamReader(gzip, Encoding.UTF8);
            using var reader = new JsonTextReader(sr);

            var serializer = JsonSerializer.CreateDefault();
            return serializer.Deserialize<Replay>(reader);
        }

        public void Reset()
        {
            _gameContext.ResetReplay();
        }

        public List<string> GetReplays()
        {
            var replays = Directory.GetFiles(REPLAYS_FOLDER).Select(f => Path.GetFileName(f)).ToList();
            replays.Sort();
            return replays;
        }
    }
}
