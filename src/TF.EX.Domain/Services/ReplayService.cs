using MessagePack;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using TF.EX.Common.Extensions;
using TF.EX.Common.Interop;
using TF.EX.Domain.Context;
using TF.EX.Domain.CustomComponent;
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

        public void AddRecord(GameState gameState)
        {
            _gameContext.AddRecord(gameState);
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

            replay.Informations.LocalSeat = _netplayManager.LocalSeat;
            replay.Informations.MatchLenght = FrameToTimestamp(replay.Record.Count);
            replay.Informations.Archers = _netplayManager.GetArchersInfo();

            Directory.CreateDirectory(REPLAYS_FOLDER);

            var filePath = NextFreeReplayPath(out var filename);

            replay.Informations.Name = filename;

            using var fileStream = new FileStream(filePath, FileMode.Create);
            WriteToFile(replay, fileStream);

            _gameContext.ResetReplay();
        }

        private string NextFreeReplayPath(out string filename)
        {
            var stamp = DateTime.UtcNow.ToString("dd'-'MM'-'yyy'T'HH'-'mm'-'ss");

            for (int attempt = 0; ; attempt++)
            {
                filename = attempt == 0 ? $"{stamp}.tow" : $"{stamp}-{attempt}.tow";

                var candidate = $"{REPLAYS_FOLDER}\\{filename}";

                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        public void Initialize(Domain.Models.WebSocket.GameData gameData = null, ICollection<Domain.Models.WebSocket.CustomMod> mods = null)
        {
            _gameContext.InitializeReplay(MainMenu.VersusMatchSettings.LevelSystem.ID.X, gameData, mods);
        }

        public void RemovePredictedRecords(int frame)
        {
            _gameContext.RemovePredictedRecords(frame);
        }

        public async Task LoadAndStart(string replayFilename, string currentSong = "")
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

                       if (replay.Informations.Mods.Any())
                       {
                           foreach (var mod in replay.Informations.Mods)
                           {
                               if (mod.Name == WiderSetModApiData.Name)
                               {
                                   var widerSetModApi = ServiceCollections.ResolveWiderSetModApi();

                                   (bool canUse, string reason) = WiderSetModApiData.CanUseWiderSet(mod.Data, widerSetModApi, true);
                                   if (canUse)
                                   {
                                       if (!widerSetModApi.IsWide)
                                       {
                                           widerSetModApi.IsWide = true;
                                           await Task.Delay(1000);
                                       }
                                   }
                                   else
                                   {
                                       Notification.Create(TFGame.Instance.Scene, $"Cannot start replay: {reason}");
                                       Monocle.Music.Play(currentSong);

                                       return;
                                   }
                               }
                           }
                       }

                       _netplayManager.SetLocalSeat(replay.Informations.LocalSeat);

                       var archers = replay.Informations.Archers.ToArray();
                       var usedArchers = archers.Select(archer => archer.Index);

                       for (int seat = 0; seat < archers.Length; seat++)
                       {
                           (var archerIndex, var altIndex) = ArcherDataExtensions.EnsureArcherDataExist(archers[seat].Index, (int)archers[seat].Type, usedArchers);

                           TFGame.Characters[seat] = archerIndex;
                           TFGame.AltSelect[seat] = (ArcherData.ArcherTypes)altIndex;
                       }

                       var firstRemote = archers.Where((_, seat) => seat != replay.Informations.LocalSeat).FirstOrDefault();
                       if (firstRemote != null)
                       {
                           _netplayManager.UpdatePlayer2Name(firstRemote.NetplayName);
                       }

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

        public int GetFrame()
        {
            return currentReplayFrame;
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
                _inputService.UpdateCurrent(record.Inputs.ToList());
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
