using MessagePack;
using Microsoft.Extensions.Logging;
using Monocle;
using MonoMod.Utils;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TF.EX.Common;
using TF.EX.Common.Extensions;
using TF.EX.Common.Handle;
using TF.EX.Domain.Context;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;
namespace TF.EX.Domain.Services
{
    public class NetplayManager : INetplayManager
    {
        //TODO: refactor this to only use native GGRSFFI
        private bool _isInitInternal;
        private double _framesToReSimulateInternal;
        private NetplayMode _netplayModeInternal;
        private bool _isRollbackFrameInternal;
        private bool _isUpdatingInternal;
        private int _framesAhead;
        private bool _spectatorCatchupEnabled = false;

        private bool _isInit
        {
            get => _isInitInternal;
            set
            {
                _isInitInternal = value;
                PublishCaptureFlag();
            }
        }

        private NetplayMode _netplayMode
        {
            get => _netplayModeInternal;
            set
            {
                _netplayModeInternal = value;
                ExFlags.IsTestMode = value == NetplayMode.Test;
                ExFlags.IsReplayMode = value == NetplayMode.Replay;
                ExFlags.Push();
                PublishCaptureFlag();
            }
        }

        private bool _isRollbackFrame
        {
            get => _isRollbackFrameInternal;
            set
            {
                _isRollbackFrameInternal = value;
                ExFlags.IsRollbackFrame = value;
                ExFlags.Push();
            }
        }

        private bool _isUpdating
        {
            get => _isUpdatingInternal;
            set
            {
                _isUpdatingInternal = value;
                ExFlags.IsRestoring = value;
                StateApi.Current.SetRestoring(value);
            }
        }

        private double _framesToReSimulate
        {
            get => _framesToReSimulateInternal;
            set
            {
                _framesToReSimulateInternal = value;
                ExFlags.FramesToReSimulate = value;
                ExFlags.Push();
            }
        }
        private bool _hasFailedInitialConnection = false;
        private volatile bool _pendingAbortToVersusOptions = false;

        private const int SYNCHRONIZATION_TIMEOUT_MS = 20000;

        private readonly IInputService _inputService;
        private readonly IGameContext _gameContext;
        private readonly IArcherService _archerService;
        private readonly ISyncTestUtilsService _syncTestUtilsService;
        private readonly ILogger _logger;

        private List<string> _events;
        private List<NetplayRequest> _netplayRequests;
        private NetworkStats _networkStats;
        private IReadOnlyDictionary<int, NetworkStats> _networkStatsPerSeat = new Dictionary<int, NetworkStats>();
        private bool _supportsPerSeatStats = true;
        private string _player2Name = "PLAYER";
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        public GGRSConfig GGRSConfig { get; internal set; }
        public NetplayMeta NetplayMeta { get; internal set; }

        public NetplayManager(
            IInputService inputService,
            IGameContext gameContext,
            IArcherService archerService,
            ISyncTestUtilsService syncTestUtilsService,
            ILogger logger)
        {
            _isInit = false;
            _netplayRequests = new List<NetplayRequest>();
            _events = new List<string>();
            _framesToReSimulate = 0;
            _networkStats = new NetworkStats();
            _isRollbackFrame = false;
            _isUpdating = false;
            _framesAhead = 0;
            _gameContext = gameContext;

            _netplayMode = NetplayMode.Uninitialized;

            LoadConfig();

            _inputService = inputService;
            _logger = logger;
            _archerService = archerService;
            _syncTestUtilsService = syncTestUtilsService;
        }

        public void Init(TowerFall.RoundLogic roundLogic)
        {
            if (_isInit)
            {
                return;
            }

            _hasFailedInitialConnection = false;
            _pendingAbortToVersusOptions = false;

            StateApi.Current.SetFrameDriver(Models.Constants.DRIVER_NAME);

            GGRSConfig.Name = NetplayMeta.Name;
            GGRSConfig.InputDelay = NetplayMeta.InputDelay;

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            if (_netplayMode == NetplayMode.Test || _netplayMode == NetplayMode.Replay)
            {
                NativeInit();
                _isInit = true;
                return;
            }

            Task.Run(() => Connect(roundLogic), _cancellationToken);
        }

        private StatusImpl NativeInit()
        {
            using var handle = new SafeBytes<GGRSConfig>(GGRSConfig, true);

            GGRSFFI.IsInInit = true;

            try
            {
                return GGRSFFI.netplay_init(handle.ToBytesFFI()).ToModelGGrsFFI();
            }
            finally
            {
                GGRSFFI.IsInInit = false;
            }
        }

        private async Task Connect(TowerFall.RoundLogic roundLogic)
        {
            try
            {
                var status = NativeInit();

                if (!status.IsOk)
                {
                    var info = status.Info.AsString();

                    if (!info.Contains("Initialization failed"))
                    {
                        throw new InvalidOperationException($"Init error : {info}");
                    }

                    _logger.LogError<NetplayManager>($"Failed to initialize netplay session : {info}");
                    AbortToVersusOptions();

                    return;
                }

                _logger.LogDebug<NetplayManager>("Netplay initialization succeeded");
                _isInit = true;

                if (!await WaitForSynchronization())
                {
                    _logger.LogError<NetplayManager>("Failed to etablish a connection to the opponent, aborting session");

                    Reset();

                    _isInit = false;
                    AbortToVersusOptions();

                    return;
                }

                OnSessionEstablished(roundLogic);
            }
            catch (Exception e)
            {
                _logger.LogError<NetplayManager>($"Error when initializing netplay session : {e.Message}");
                AbortToVersusOptions();
            }
        }

        private async Task<bool> WaitForSynchronization()
        {
            var timer = Stopwatch.StartNew();

            while (!IsSynchronized() && !_cancellationToken.IsCancellationRequested && timer.ElapsedMilliseconds < SYNCHRONIZATION_TIMEOUT_MS)
            {
                Poll();
                await Task.Delay(TFGame.FrameTime);
            }

            timer.Stop();

            return IsSynchronized();
        }

        private void AbortToVersusOptions()
        {
            _hasFailedInitialConnection = true;
            _pendingAbortToVersusOptions = true;
        }

        public bool ConsumeAbortToVersusOptions()
        {
            if (!_pendingAbortToVersusOptions)
            {
                return false;
            }

            _pendingAbortToVersusOptions = false;

            return true;
        }

        private void OnSessionEstablished(TowerFall.RoundLogic roundLogic)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var isSpectator = matchmakingService.IsSpectator();

            _logger.LogDebug<NetplayManager>(isSpectator
                ? "Netplay session etablished as spectator"
                : $"Netplay session etablished with {_player2Name}");

            _gameContext.ResetPlayersIndex();

            if (_netplayMode == NetplayMode.Local)
            {
                return;
            }

            _archerService.ApplyToGame();

            if (!isSpectator)
            {
                _player2Name = _gameContext.GetPlayers()
                    .FirstOrDefault(entry => entry.Item1 == 1).Item2?.Name ?? _player2Name;
            }

            SpawnFirstRound(roundLogic);
        }

        private static void SpawnFirstRound(TowerFall.RoundLogic roundLogic)
        {
            if (roundLogic is IRoundPlayerSpawner spawner)
            {
                spawner.SpawnRoundPlayers();
                return;
            }

            var dynRoundLogic = DynamicData.For(roundLogic);

            roundLogic.Session.CurrentLevel.Add(new VersusStart(roundLogic.Session));
            dynRoundLogic.Set("Players", dynRoundLogic.Invoke("SpawnPlayersFFA"));
        }

        public void Poll()
        {
            if (_isInit)
            {
                NativePoll();
                if (!IsDisconnected())
                {
                    _framesAhead = GGRSFFI.netplay_frames_ahead();
                    GetEventAndUpdate();
                    if (IsSynchronized())
                    {
                        UpdateNetworkStats();
                    }
                }
            }
        }

        private void NativePoll()
        {
            var status = GGRSFFI.netplay_poll().ToModelGGrsFFI();
            if (!status.IsOk)
            {
                if (status.Info.AsString().Contains("Disconnected")
                    || status.Info.AsString().Contains("local_frame_advantage bigger than")
                    || status.Info.AsString().Contains("No session found"))
                {
                    _logger.LogError<NetplayManager>($"Error when handling opponent communication : {status.Info.AsString()}");
                }
                else
                {
                    throw new InvalidOleVariantTypeException($"Error when polling remote client : {status.Info.AsString()}");
                }
            }
        }

        private void GetEventAndUpdate()
        {
            var netplayEvents = GGRSFFI.netplay_events();

            using (var handle = netplayEvents.ToModel())
            {
                _events = handle._events.ToList();
                handle.Dispose();
            }

            if (_events.ToList().Count > 0)
            {
                if (_events.Any(s => s.Contains(Event.NetworkInterrupted.ToString())))
                {
                    if (!ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty)
                    {
                        Notification.Create(TFGame.Instance.Scene, "Trying to re sync", 10, 300, false, true);
                    }
                }

                if (_events.Any(s => s.Contains(Event.NetworkResumed.ToString())))
                {
                    Notification.Clear(TFGame.Instance.Scene, 4);
                }

                if (_events.Any(s => s.Contains(Event.Synchronized.ToString())))
                {
                    Sounds.ui_clickSpecial.Play();
                    Notification.Create(TFGame.Instance.Scene, "Synchronized!", 10, 150);
                }

                if (_events.Any(s => s.Contains(Event.Disconnected.ToString())))
                {
                    Notification.Clear(TFGame.Instance.Scene, 4);

                    if (!IsDisconnected()
                        && IsSpectatorMode()
                        && GGRSFFI.netplay_frames_behind() > 120)
                    {
                        ServiceCollections.ResolveMatchmakingService().QueueSpectatorNotice("MATCH OVER, WILL END OR RESTART");
                    }

                    if (IsDisconnected())
                    {
                        ServiceCollections.ResolveReplayService().Export();

                        //Leave lobby if the game is not over and we are disconnected
                        if ((TFGame.Instance.Scene as TowerFall.Level).Session.GetWinner() == -1)
                        {
                            ServiceCollections.ResolveMatchmakingService().LeaveLobby(() => { }, () => { });
                        }
                    }
                }

                var desynchStrings = _events.Where(s => s.Contains(Event.DesyncDetected.ToString())).ToList();

                if (desynchStrings.Count > 0)
                {
                    foreach (var desynchString in desynchStrings)
                    {
                        _logger.LogWarning<NetplayManager>(desynchString);
                    }
                }
            }
        }

        private void UpdateNetworkStats()
        {
            using (SafeHandle<NetworkStats> handle = new SafeHandle<NetworkStats>(new NetworkStats()))
            {
                var status_stats = GGRSFFI.netplay_network_stats(-1, handle.Ptr).ToModelGGrsFFI();
                if (status_stats.IsOk)
                {
                    _networkStats = handle.Value;
                }
            }

            if (!_supportsPerSeatStats)
            {
                return;
            }

            // one ping per remote seat,
            var perSeat = new Dictionary<int, NetworkStats>();

            try
            {
                var remoteCount = GGRSFFI.netplay_remote_player_handle_count();

                for (int index = 0; index < remoteCount; index++)
                {
                    var seat = GGRSFFI.netplay_remote_player_handle_at(index);

                    using SafeHandle<NetworkStats> seatHandle = new SafeHandle<NetworkStats>(new NetworkStats());

                    if (GGRSFFI.netplay_network_stats(seat, seatHandle.Ptr).ToModelGGrsFFI().IsOk)
                    {
                        perSeat[seat] = seatHandle.Value;
                    }
                }
            }
            catch (EntryPointNotFoundException)
            {
                _supportsPerSeatStats = false;
                return;
            }

            _networkStatsPerSeat = perSeat;
        }

        public StatusImpl AdvanceFrame(Input input)
        {
            var status = GGRSFFI.netplay_advance_frame(input).ToModelGGrsFFI();

            if (!status.IsOk)
            {
                var info = status.Info.AsString();

                if (info.Contains("Peer Disconnected!") || info.Contains("No session found"))
                {
                    _logger.LogError<NetplayManager>($"Error when advancing frame : {info}");

                    return status;
                }
                else if (info.Contains("Detected checksum mismatch"))
                {
                    string mismatch = "";
                    string patternToFindNumbers = @"\b(\d+)\b";
                    MatchCollection matches = Regex.Matches(info, patternToFindNumbers);
                    int frame = -1;
                    if (matches.Count > 0)
                    {
                        string lastNumber = matches[matches.Count - 1].Value;
                        frame = int.Parse(lastNumber);
                        mismatch = $"\n\n {_syncTestUtilsService.Compare(frame)}";
                    }

                    if (IsTestMode())
                    {
                        _logger.LogError<NetplayManager>($"Sync test desync detected : {info} {mismatch}");

                        TowerFall.Sounds.ui_invalid.Play();
                        Reset();
                        ResetMode();

                        if (ScenarioSweeper.IsRunning)
                        {
                            ScenarioSweeper.OnDesync(frame);

                            return status;
                        }

                        TFGame.Instance.Scene = new MainMenu(MainMenu.MenuState.PressStart);

                        return status;
                    }

                    _logger.LogError<NetplayManager>($"Cross-peer desync detected : {info} {mismatch}");

                    TowerFall.Sounds.ui_invalid.Play();
                    Reset();

                    if (TFGame.Instance.Scene is Level desyncedLevel)
                    {
                        desyncedLevel.GoToNetplayEntryMenu();
                    }

                    Notification.Create(TFGame.Instance.Scene, "DESYNC DETECTED - match ended", 15, 450);

                    return status;
                }
                else if (!info.Equals("PredictionThreshold"))
                {
                    if (_netplayMode == NetplayMode.Spectator)
                    {
                        Task.Run(async () =>
                        {
                            await ServiceCollections.ResolveMatchmakingService().LeaveLobby(() => { }, () => { });
                        }).GetAwaiter().GetResult();

                        var mainMenu = new MainMenu(Context.MenuReturn.NetplayEntry ?? MainMenu.MenuState.VersusOptions);
                        Engine.Instance.Scene = mainMenu;
                        (TFGame.Instance.Scene as Level).Session.MatchSettings.LevelSystem.Dispose();

                        Sounds.ui_invalid.Play();
                        Notification.Create(TFGame.Instance.Scene, "A error occured while spectating");

                        return status;
                    }

                    throw new InvalidOperationException($"AdvanceFrame error : {info}");
                }
            }

            return status;
        }


        public void UpdateNetplayRequests()
        {
            if (_netplayRequests != null && _netplayRequests.ToList().Count == 0)
            {
                var netplayReq = GGRSFFI.netplay_get_requests();

                using (var handle = netplayReq.ToModel())
                {
                    _netplayRequests = handle._requests;
                }

                if (_netplayRequests.ToList().Count > 0)
                {
                    var isRollbackFrame = _netplayRequests.ToList()[0].Equals(NetplayRequest.LoadGameState);

                    if (isRollbackFrame)
                    {
                        _isRollbackFrame = true; //Mark the start of the First RBF
                        for (int i = 0; i < _netplayRequests.ToList().Count; i++)
                        {
                            if (_netplayRequests.ToList()[i].Equals(NetplayRequest.AdvanceFrame))
                            {
                                _framesToReSimulate++;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Netplay request are not empty/null. Please use all before getting new requests");
            }
        }

        public void SaveGameState(byte[] state)
        {
            if (_isInit)
            {
                //Already encoded by TF.State, EX only moves the blob.
                using (var handle = new SafeRawBytes(state))
                {
                    var status = GGRSFFI.netplay_save_game_state(handle.ToBytesFFI()).ToModelGGrsFFI();
                    if (!status.IsOk)
                    {
                        throw new InvalidOperationException($"Save game state error : {status.Info.AsString()}");
                    }
                }
            }
        }

        public void AdvanceGameState()
        {
            if (_isInit)
            {
                var nativeInputs = GGRSFFI.netplay_advance_game_state();

                using (var handle = nativeInputs.ToModel())
                {
                    var inputs = handle._inputs;

                    _inputService.UpdateCurrent(inputs.ToList());
                }
            }
        }


        public byte[] LoadGameState()
        {
            if (_isInit)
            {
                byte[] toLoad;

                var result = GGRSFFI.netplay_load_game_state();
                var status = result.Status.ToModelGGrsFFI();
                if (!status.IsOk)
                {
                    throw new InvalidOperationException($"LoadGameState error : {status.Info.AsString()}");
                }

                using (var safeBytes = new SafeRawBytes(result.Data, () => { GGRSFFI.netplay_free_game_state(result.Data); }))
                {
                    toLoad = safeBytes.PtrToBytes();
                }

                return toLoad;
            }

            throw new InvalidOperationException($"LoadGameState error : Netplay hasn't been initialized");
        }

        public NetworkStats GetNetworkStats()
        {
            return _networkStats;
        }

        public IReadOnlyDictionary<int, NetworkStats> GetNetworkStatsPerSeat()
        {
            return _networkStatsPerSeat;
        }

        public bool IsTestMode()
        {
            return _netplayMode == NetplayMode.Test;
        }

        public bool IsRollbackFrame()
        {
            return _isRollbackFrame;
        }

        public void SetIsRollbackFrame(bool isRollbackFrame)
        {
            _isRollbackFrame = isRollbackFrame;
        }

        public bool HaveFramesToReSimulate()
        {
            return _framesToReSimulate > 0;
        }

        public void UpdateFramesToReSimulate(int frame)
        {
            _framesToReSimulate = frame;
        }

        public bool IsInit()
        {
            return _isInit && !IsDisconnected();
        }

        public void PublishCaptureFlag()
        {
            ExFlags.IsCaptureActive = _netplayModeInternal == NetplayMode.Replay || (_netplayModeInternal != NetplayMode.Uninitialized && !(_isInitInternal && IsDisconnected()));
            ExFlags.Push();
        }

        public NetplayMode GetNetplayMode()
        {
            return _netplayMode;
        }

        public bool CanAdvanceFrame()
        {
            return _netplayRequests.Count == 1 && _netplayRequests.FirstOrDefault().Equals(NetplayRequest.AdvanceFrame);
        }

        public bool HaveRequestToHandle()
        {
            return _netplayRequests.ToList().Count > 0;
        }

        public NetplayRequest ConsumeNetplayRequest()
        {
            var req = _netplayRequests.First();

            _netplayRequests.RemoveAt(0);

            return req;
        }

        public bool IsSynchronized()
        {
            if (GGRSFFI.IsInInit)
            {
                return false;
            }

            var res = GGRSFFI.netplay_is_synchronized().ToModelGGrsFFI();

            return res.IsOk;
        }

        public bool IsDisconnected()
        {
            if (GGRSFFI.IsInInit || IsTestMode())
            {
                return false;
            }

            var res = GGRSFFI.netplay_is_disconnected().ToModelGGrsFFI();

            return res.IsOk;
        }

        public double GetFramesToReSimulate()
        {
            return _framesToReSimulate;
        }

        public bool IsFramesAhead()
        {
            return _framesAhead > 0;
        }

        public bool IsUpdating()
        {
            return _isUpdating;
        }

        public void SetIsUpdating(bool isUpdating)
        {
            _isUpdating = isUpdating;
        }

        public bool IsReplayMode()
        {
            return _netplayMode == NetplayMode.Replay;
        }

        public void Reset()
        {
            StateApi.Current.SetFrameDriver(null);

            if (_isInit)
            {
                using var status = GGRSFFI.netplay_reset().ToModelGGrsFFI();

                if (!status.IsOk)
                {
                    if (status.Info.AsString().Contains("No session found"))
                    {
                        _logger.LogError<NetplayManager>($"Netplay error when reseting : {status.Info.AsString()}, skipping netplay reset");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Reset error : {status.Info.AsString()}");
                    }
                }

                StateApi.Current.ResetSession();
                StateApi.Current.ResetSfx();

                _isInit = false;
                _netplayRequests = new List<NetplayRequest>();
                _events = new List<string>();
                _framesToReSimulate = 0;
                _networkStats = new NetworkStats();
                _isRollbackFrame = false;
                _framesAhead = 0;
                _isUpdating = false;
                _gameContext.Reset();

                _cancellationTokenSource.Cancel();
                GGRSFFI.IsInInit = false;
                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;
            }
        }

        public NetplayMeta GetNetplayMeta()
        {
            return NetplayMeta;
        }

        public void UpdateMeta(NetplayMeta config)
        {
            NetplayMeta = config;
        }

        public void SaveConfig()
        {
            var bytes = MessagePackSerializer.Serialize(NetplayMeta, SerializationOptions.GetContractlessOptions());

            var jsonToSave = MessagePackSerializer.ConvertToJson(bytes, SerializationOptions.GetContractlessOptions());
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "netplay_meta.json"), jsonToSave);
        }

        private void LoadConfig()
        {
            try
            {
                string filePath = "netplay_conf.json";
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                GGRSConfig = new GGRSConfig
                {
                    Netplay = new NetplayConfig(),
                };

                var json = File.ReadAllText("netplay_meta.json");

                var bytes = MessagePackSerializer.ConvertFromJson(json, SerializationOptions.GetContractlessOptions());
                NetplayMeta = MessagePackSerializer.Deserialize<NetplayMeta>(bytes, SerializationOptions.GetContractlessOptions());
                if (NetplayMeta.InputDelay == 0)
                {
                    NetplayMeta.InputDelay = 2;
                    SaveConfig();
                }

                if (string.IsNullOrEmpty(NetplayMeta.Name))
                {
                    NetplayMeta.Name = "PLAYER";
                    SaveConfig();
                }

                if (NetplayMeta.Name.Length > 10)
                {
                    NetplayMeta.Name = NetplayMeta.Name.Substring(0, Math.Min(NetplayMeta.Name.Length, 10));
                    SaveConfig();
                }

                NetplayMeta.Name = NetplayMeta.Name.ToUpper();
            }
            catch (FileNotFoundException)
            {
                NetplayMeta = new NetplayMeta
                {
                    InputDelay = 2,
                    Name = "PLAYER",
                };

                SaveConfig();
            }

        }

        public string GetPlayer2Name()
        {
            return _player2Name;
        }

        public void UpdatePlayer2Name(string player2Name)
        {
            _player2Name = player2Name;
        }

        public void SetRoomAndServerMode(string roomUrl, bool isHost)
        {
            GGRSConfig.Netplay.SpectatorConf = null;
            GGRSConfig.Netplay.ServerConf = new NetplayServerConfig
            {
                RoomUrl = roomUrl,
                IsHost = isHost
            };
            _netplayMode = NetplayMode.Server;
        }

        public int LocalSeat => _gameContext.GetLocalPlayerIndex();

        public string GetNameForSeat(int seat)
        {
            if (seat == _gameContext.GetLocalPlayerIndex())
            {
                return NetplayMeta.Name;
            }

            var player = _gameContext.GetPlayers().FirstOrDefault(entry => entry.Item1 == seat).Item2;

            return player?.Name ?? _player2Name;
        }

        public void SetLocalSeat(int seat)
        {
            _gameContext.SetLocalSeat(seat);
        }

        public bool HasSetMode()
        {
            return _netplayMode != NetplayMode.Uninitialized;
        }

        public void SetTestMode(int checkDistance, int numPlayers)
        {
            _netplayMode = NetplayMode.Test;
            GGRSConfig = GGRSConfig.DefaultTest(checkDistance, numPlayers);
            _syncTestUtilsService.Reset();
        }

        public void SetLocalMode(string addr, ushort localPort, PlayerDraw draw)
        {
            _netplayMode = NetplayMode.Local;
            GGRSConfig = GGRSConfig.DefaultLocal(addr, localPort, draw);
        }

        public void SetReplayMode()
        {
            _netplayMode = NetplayMode.Replay;
        }

        public void SetServerMode(string roomUrl)
        {
            _netplayMode = NetplayMode.Server;
            GGRSConfig = GGRSConfig.DefaultServer(roomUrl, true);
        }

        public void ResetMode()
        {
            _netplayMode = NetplayMode.Uninitialized;

            StateApi.Current.SetFrameDriver(null);
        }

        public bool HasFailedInitialConnection()
        {
            return _hasFailedInitialConnection;
        }

        public ICollection<Models.ArcherSeatInfo> GetArchersInfo()
        {
            var archersInfo = new List<Models.ArcherSeatInfo>();

            if (TFGame.Instance.Scene is not Level)
            {
                _logger.LogError<NetplayManager>($"Scene is not a Level, it is a {TFGame.Instance.Scene.GetType().Name}");
                return new List<Models.ArcherSeatInfo>();
            }

            var level = TFGame.Instance.Scene as Level;

            var localSeat = _gameContext.GetLocalPlayerIndex();
            var players = _gameContext.GetPlayers().ToDictionary(p => p.Item1, p => p.Item2);

            foreach ((var seat, var archerAlt) in _archerService.GetArchers().OrderBy(archer => archer.Item1))
            {
                var splitted = archerAlt.Split('-');
                Enum.TryParse(splitted[1], out ArcherData.ArcherTypes alt);

                archersInfo.Add(new Models.ArcherSeatInfo
                {
                    Seat = seat,
                    NetplayName = seat == localSeat
                        ? NetplayMeta.Name
                        : players.TryGetValue(seat, out var player) ? player.Name : _player2Name,
                    Index = int.Parse(splitted[0]),
                    HasWon = seat < level.Session.MatchStats.Length && level.Session.MatchStats[seat].Won,
                    Score = GetScore(level.Session, seat),
                    Type = (int)alt,
                });
            }

            return archersInfo;
        }

        private static int GetScore(TowerFall.Session session, int seat)
        {
            var scoreIndex = session.GetScoreIndex(seat);

            return scoreIndex >= 0 && scoreIndex < session.Scores.Length ? session.Scores[scoreIndex] : 0;
        }

        public bool IsServerMode()
        {
            return _netplayMode == NetplayMode.Server;
        }

        public void SetSpectatorMode(string roomUrl, string toSpectate)
        {
            _netplayMode = NetplayMode.Spectator;
            _spectatorCatchupEnabled = false;
            GGRSConfig.Netplay.ServerConf = null;
            GGRSConfig.Netplay.SpectatorConf = new NetplaySpectatorConfig
            {
                ToSpectate = toSpectate,
                RoomUrl = roomUrl,
            };
        }

        public void SetSpectatorCatchup(bool enabled)
        {
            _spectatorCatchupEnabled = enabled;
        }

        public bool IsSpectatorCatchupEnabled()
        {
            return _spectatorCatchupEnabled;
        }

        public bool IsSpectatorMode()
        {
            return _netplayMode == NetplayMode.Spectator;
        }

        public void AddLateSpectator(string peerId)
        {
            if (GGRSFFI.IsInInit || !_isInit)
            {
                _logger.LogDebug<NetplayManager>($"Ignoring late spectator {peerId} (no active session)");
                return;
            }

            var status = GGRSFFI.netplay_add_spectator(peerId).ToModelGGrsFFI();
            if (!status.IsOk)
            {
                _logger.LogError<NetplayManager>($"Failed to add late spectator {peerId} : {status.Info.AsString()}");
                return;
            }

            _logger.LogDebug<NetplayManager>($"Late spectator {peerId} added to the session");
        }

        public void AddSpectators(IEnumerable<Domain.Models.WebSocket.Player> spectators)
        {
            foreach (var spectator in spectators)
            {
                var peerId = spectator.RoomPeerId;
                GGRSConfig.Netplay.Spectators.Add(peerId);
            }
        }

        public void UpdatePlayers(ICollection<Domain.Models.WebSocket.Player> players, ICollection<Domain.Models.WebSocket.Player> spectators)
        {
            GGRSConfig.Netplay.Players = new List<string>();
            GGRSConfig.Netplay.Spectators = new List<string>();
            GGRSConfig.Netplay.LocalPeerId = ServiceCollections.ResolveMatchmakingService().GetRoomPeerId();

            foreach (var player in players.OrderBy(player => player.Seat))
            {
                var peerId = player.RoomPeerId;
                GGRSConfig.Netplay.Players.Add(peerId);
            }

            foreach (var spectator in spectators)
            {
                var peerId = spectator.RoomPeerId;
                GGRSConfig.Netplay.Spectators.Add(peerId);
            }

            if (_netplayMode == NetplayMode.Spectator)
            {
                var hostPeerId = players.First(p => p.IsHost).RoomPeerId;
                GGRSConfig.Netplay.SpectatorConf.ToSpectate = hostPeerId;
            }
        }

        public void UpdateNumPlayers(int count)
        {
            GGRSConfig.Netplay.NumPlayers = count;
        }

        public int GetNumPlayers()
        {
            return GGRSConfig.Netplay.NumPlayers;
        }
    }
}
