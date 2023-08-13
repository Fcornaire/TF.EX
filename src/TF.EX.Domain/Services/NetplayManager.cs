using Microsoft.Extensions.Logging;
using MonoMod.Utils;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TF.EX.Common.Extensions;
using TF.EX.Common.Handle;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Domain.Services
{
    public class NetplayManager : INetplayManager
    {
        private bool _isInit;
        private bool _isAttemptingToReconnect;
        private bool _isSyncing = false;
        private double _framesToReSimulate;
        private NetplayMode _netplayMode;
        private bool _isRollbackFrame;
        private double _framesAhead;
        private bool _isUpdating = false;
        private bool _isFirstInit = true;
        private bool _hasFailedInitialConnection = false;
        private (int, ArcherData.ArcherTypes)[] _originalSelection = new (int, ArcherData.ArcherTypes)[4];

        private readonly IInputService _inputService;
        private readonly IGameContext _gameContext;
        private readonly ILogger _logger;

        private List<string> _events;
        private List<NetplayRequest> _netplayRequests;
        private NetworkStats _networkStats;
        private bool _hasDesynch = false;
        private string _player2Name = "PLAYER";
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        public GGRSConfig GGRSConfig { get; internal set; }
        public NetplayMeta NetplayMeta { get; internal set; }

        public NetplayManager(
            IInputService inputService,
            IGameContext gameContext,
            ILogger logger)
        {
            _isInit = false;
            _netplayRequests = new List<NetplayRequest>();
            _events = new List<string>();
            _framesToReSimulate = 0;
            _networkStats = new NetworkStats();
            _isRollbackFrame = false;
            _framesAhead = 0;
            _gameContext = gameContext;

            _netplayMode = NetplayMode.Uninitialized;

            LoadConfig();

            _inputService = inputService;
            _logger = logger;
        }

        public void Init(TowerFall.RoundLogic roundLogic)
        {
            if (!_isInit)
            {
                _hasFailedInitialConnection = false;

                GGRSConfig.Name = NetplayMeta.Name;
                GGRSConfig.InputDelay = NetplayMeta.InputDelay;

                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;

                if (_netplayMode != NetplayMode.Test && _netplayMode != NetplayMode.Replay)
                {
                    Task.Run(async () =>
                            {
                                using var handle = new SafeBytes<GGRSConfig>(GGRSConfig, false);
                                GGRSFFI.IsInInit = true;
                                var status = GGRSFFI.netplay_init(handle.ToBytesFFI()).ToModelGGrsFFI();
                                GGRSFFI.IsInInit = false;

                                if (!status.IsOk)
                                {
                                    var info = status.Info.AsString();
                                    if (info.Contains("Initialization failed"))
                                    {
                                        _logger.LogError<NetplayManager>($"Failed to initialize netplay session : {info}");

                                        _hasFailedInitialConnection = true;

                                        TowerFall.Sounds.ui_invalid.Play();
                                        (TFGame.Instance.Scene as Level).GoToVersusOptions();

                                        return;
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException($"Init error : {status.Info.AsString()}");
                                    }
                                }

                                _logger.LogDebug<NetplayManager>($"Netplay initialization succeeded");
                                _isInit = true;

                                var timer = new Stopwatch();
                                timer.Start();

                                while (!IsSynchronized() && !_cancellationToken.IsCancellationRequested && timer.ElapsedMilliseconds < 20000)
                                {
                                    Poll();
                                    await Task.Delay(TFGame.FrameTime);
                                }

                                timer.Stop();

                                if (!IsSynchronized())
                                {
                                    _isInit = false;
                                    _hasFailedInitialConnection = true;
                                    _logger.LogError<NetplayManager>($"Failed to etablish a connexion to the opponent, aborting session...");
                                    TowerFall.Sounds.ui_invalid.Play();

                                    (TFGame.Instance.Scene as Level).GoToVersusOptions();
                                    return;
                                }

                                _logger.LogDebug<NetplayManager>($"Netplay session etablished with {_player2Name}");
                                ServiceCollections.ResolveMatchmakingService().DisconnectFromLobby();

                                if (_isFirstInit)
                                {
                                    _originalSelection[0] = (TFGame.Characters[0], TFGame.AltSelect[0]);
                                    _originalSelection[1] = (TFGame.Characters[1], TFGame.AltSelect[1]);

                                    var (char0, alt0) = _originalSelection[0];
                                    var (char1, alt1) = _originalSelection[1];

                                    if (ShouldSwapPlayer())
                                    {
                                        TFGame.Characters[0] = char1;
                                        TFGame.AltSelect[0] = alt1;
                                        TFGame.Characters[1] = char0;
                                        TFGame.AltSelect[1] = alt0;
                                    }
                                }
                                else
                                {
                                    var (char0, alt0) = _originalSelection[0];
                                    var (char1, alt1) = _originalSelection[1];

                                    if (ShouldSwapPlayer())
                                    {
                                        TFGame.Characters[0] = char1;
                                        TFGame.AltSelect[0] = alt1;
                                        TFGame.Characters[1] = char0;
                                        TFGame.AltSelect[1] = alt0;
                                    }
                                    else
                                    {
                                        TFGame.Characters[0] = char0;
                                        TFGame.AltSelect[0] = alt0;
                                        TFGame.Characters[1] = char1;
                                        TFGame.AltSelect[1] = alt1;
                                    }
                                }

                                _isFirstInit = false;
                                var dynRoundLogic = DynamicData.For(roundLogic);

                                roundLogic.Session.CurrentLevel.Add(new VersusStart(roundLogic.Session));
                                dynRoundLogic.Set("Players", dynRoundLogic.Invoke("SpawnPlayersFFA"));
                            }, _cancellationToken);
                }
                else
                {
                    using var handle = new SafeBytes<GGRSConfig>(GGRSConfig, false);
                    GGRSFFI.netplay_init(handle.ToBytesFFI()).ToModelGGrsFFI();
                    _isInit = true;
                }
            }
        }

        /// <summary>
        /// Retrieve infomrations since last packet received from the remote client
        /// </summary>
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
                if (status.Info.AsString().Contains("Peer Disconnected!")
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
                if (_events.Any(s => s.Contains(Event.Synchronizing.ToString())))
                {
                    _isSyncing = true;
                }

                if (_events.Any(s => s.Contains(Event.NetworkInterrupted.ToString())))
                {
                    _isAttemptingToReconnect = true;
                }

                if (_events.Any(s => s.Contains(Event.NetworkResumed.ToString())))
                {
                    _isAttemptingToReconnect = false;
                }

                if (_events.Any(s => s.Contains(Event.Synchronized.ToString())))
                {
                    _isSyncing = false;
                }

                if (_events.Any(s => s.Contains(Event.Disconnected.ToString())))
                {
                    ServiceCollections.ResolveMatchmakingService().DisconnectFromServer();
                }

                //TODO: find a way to properly detect desynch
                //if (_events.Any(s => s.Contains(Event.DesyncDetected.ToString())) && !HaveFramesToReSimulate())
                //{
                //    _hasDesynch = true;
                //    var desynchStrings = _events.Where(s => s.Contains(Event.DesyncDetected.ToString())).ToList();

                //    foreach (var desynchString in desynchStrings)
                //    {
                //        var frame = Int32.Parse(desynchString.Split(new[] { "frame" }, StringSplitOptions.None)[1].Split(',')[0].Trim());
                //        Console.WriteLine("Desynched at " + frame);
                //        _desynchedFrames.Add(frame);
                //    }
                //}
            }

            _events.ForEach(evt => _logger.LogDebug<NetplayManager>($"Event {DateTime.UtcNow} : {evt}"));
        }

        private void UpdateNetworkStats()
        {
            using (SafeHandle<NetworkStats> handle = new SafeHandle<NetworkStats>(new NetworkStats()))
            {
                var status_stats = GGRSFFI.netplay_network_stats(handle.Ptr).ToModelGGrsFFI();
                if (status_stats.IsOk)
                {
                    _networkStats = handle.Value;
                }
            }
        }

        public StatusImpl AdvanceFrame(Input input)
        {
            var status = GGRSFFI.netplay_advance_frame(input).ToModelGGrsFFI();

            if (!status.IsOk)
            {
                if (status.Info.AsString().Contains("Peer Disconnected!") || status.Info.AsString().Contains("No session found"))
                {
                    _logger.LogError<NetplayManager>($"Error when advancing frame : {status.Info.AsString()}");

                    return status;
                }

                if (!status.Info.AsString().Equals("PredictionThreshold"))
                {
                    throw new InvalidOperationException($"AdvanceFrame error : {status.Info.AsString()}");
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

        public void SaveGameState(GameState gameState)
        {
            if (_isInit)
            {
                using (var handle = new SafeBytes<GameState>(gameState, true))
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

                    var gameInputs = inputs.Select(inp => inp.ToTFInput()).ToList();

                    if (ShouldSwapPlayer())
                    {
                        gameInputs.Reverse(); //TODO: only true with 2Players
                    }

                    _inputService.UpdateCurrent(gameInputs);
                }
            }
        }


        public GameState LoadGameState()
        {
            if (_isInit)
            {
                GameState toLoad;

                var result = GGRSFFI.netplay_load_game_state();
                var status = result.Status.ToModelGGrsFFI();
                if (!status.IsOk)
                {
                    throw new InvalidOperationException($"LoadGameState error : {status.Info.AsString()}");
                }

                using (var safeBytes = new SafeBytes<GameState>(result.Data, () => { GGRSFFI.netplay_free_game_state(result.Data); }))
                {
                    toLoad = safeBytes.ToStruct(true);
                }

                return toLoad;
            }

            throw new InvalidOperationException($"LoadGameState error : Netplay hasn't been initialized");
        }

        public NetworkStats GetNetworkStats()
        {
            return _networkStats;
        }

        public bool IsTestMode()
        {
            return _netplayMode == NetplayMode.Test || _netplayMode == NetplayMode.Local;
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

        bool INetplayManager.IsInit()
        {
            return _isInit && !IsDisconnected();
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
            if (GGRSFFI.IsInInit)
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

        public bool HasDesynchronized()
        {
            return _hasDesynch;
        }

        public bool IsAttemptingToReconnect()
        {
            return _isAttemptingToReconnect;
        }

        public void Reset()
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

            ServiceCollections.ResolveMatchmakingService().DisconnectFromServer();
            ServiceCollections.ResolveMatchmakingService().DisconnectFromLobby();
            ServiceCollections.ResolveSessionService().Reset();

            _isInit = false;
            _netplayRequests = new List<NetplayRequest>();
            _events = new List<string>();
            _framesToReSimulate = 0;
            _networkStats = new NetworkStats();
            _isRollbackFrame = false;
            _framesAhead = 0;
            _hasDesynch = false;
            _isAttemptingToReconnect = false;
            _isUpdating = false;
            _gameContext.ResetPlayersIndex();

            _cancellationTokenSource.Cancel();
            GGRSFFI.IsInInit = false;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            (var stateMachine, _) = ServiceCollections.ResolveStateMachineService();
            stateMachine.Reset();
            ServiceCollections.ResolveReplayService().Reset();
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
            var jsonToSave = JsonConvert.SerializeObject(NetplayMeta, Formatting.Indented);
            File.WriteAllText($"{Directory.GetCurrentDirectory()}\\netplay_meta.json", jsonToSave);
        }

        private void LoadConfig()
        {
            try
            {
                string filePath = "netplay_conf.json"; //Removing old config file
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                GGRSConfig = new GGRSConfig
                {
                    Netplay = new NetplayConfig(),
                };

                var json = File.ReadAllText("netplay_meta.json");
                NetplayMeta = JsonConvert.DeserializeObject<NetplayMeta>(json);
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
            catch (FileNotFoundException e)
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

        public void SetRoom(string roomUrl)
        {
            GGRSConfig.Netplay.Server = new NetplayServerConfig
            {
                RoomUrl = roomUrl
            };
        }

        public bool ShouldSwapPlayer()
        {
            return _gameContext.ShouldSwapPlayer();
        }

        public PlayerDraw GetPlayerDraw()
        {
            return _gameContext.GetPlayerDraw();
        }

        public void SetPlayersIndex(int playerDraw)
        {
            _gameContext.SetPlayersIndex(playerDraw);
        }

        public void EnableReplayMode()
        {
            _netplayMode = NetplayMode.Replay;
        }

        public void DisableReplayMode()
        {
            _netplayMode = NetplayMode.Unknown;
        }

        public bool HasSetMode()
        {
            return _netplayMode != NetplayMode.Uninitialized;
        }

        public void SetTestMode(int checkDistance)
        {
            _netplayMode = NetplayMode.Test;
            GGRSConfig = GGRSConfig.DefaultTest(checkDistance);
        }

        public void SetLocalMode(string addr, PlayerDraw draw)
        {
            _netplayMode = NetplayMode.Local;
            GGRSConfig = GGRSConfig.DefaultLocal(addr, draw);
        }

        public void SetReplayMode()
        {
            _netplayMode = NetplayMode.Replay;
        }

        public void SetServerMode(string roomUrl)
        {
            _netplayMode = NetplayMode.Server;
            GGRSConfig = GGRSConfig.DefaultServer(roomUrl);
        }

        public void ResetMode()
        {
            _netplayMode = NetplayMode.Uninitialized;
        }

        public bool HasFailedInitialConnection()
        {
            return _hasFailedInitialConnection;
        }

        public void SetIsFirstInit(bool isFirstInit)
        {
            _isFirstInit = isFirstInit;
        }

        public bool IsSyncing()
        {
            return _isSyncing;
        }
    }
}
