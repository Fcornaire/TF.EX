using Newtonsoft.Json;
using System.Runtime.InteropServices;
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
        private bool _isSynchronized;
        private bool _isAttemptingToReconnect;
        private double _framesToReSimulate;
        private NetplayMode _netplayMode;
        private bool _isRollbackFrame;
        private double _framesAhead;
        private bool _isUpdating = false;
        private bool _isFirstInit = true;
        private (int, ArcherData.ArcherTypes)[] _originalSelection = new (int, ArcherData.ArcherTypes)[4];

        private readonly IInputService _inputService;
        private readonly IGameContext _gameContext;

        private List<string> _events;
        private List<NetplayRequest> _netplayRequests;
        private NetworkStats _networkStats;
        private bool _isDisconnected = false;
        private bool _hasDesynch = false;
        private bool _isSyncing = false;
        private string _player2Name = "PLAYER";

        public GGRSConfig GGRSConfig { get; internal set; }

        public NetplayManager(
            IInputService inputService,
            IGameContext gameContext)
        {
            _isInit = false;
            _netplayRequests = new List<NetplayRequest>();
            _events = new List<string>();
            _isSynchronized = false;
            _framesToReSimulate = 0;
            _networkStats = new NetworkStats();
            _isRollbackFrame = false;
            _framesAhead = 0;
            _gameContext = gameContext;

            _netplayMode = NetplayMode.Uninitialized;

            LoadConfig();

            _inputService = inputService;
        }

        public void Init()
        {
            if (!_isInit)
            {
                _isDisconnected = false;

                using (var handle = new SafeBytes<GGRSConfig>(GGRSConfig, false))
                {
                    var status = GGRSFFI.netplay_init(handle.ToBytesFFI()).ToModelGGrsFFI();

                    if (!status.IsOk)
                    {
                        throw new InvalidOperationException($"Init error : {status.Info.AsString()}");
                    }

                    _isInit = true;
                }


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

                _gameContext.GetLocalPlayerIndex();
                _gameContext.GetRemotePlayerIndex();

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
                if (!_isDisconnected)
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
                if (status.Info.AsString().Contains("Peer Disconnected!"))
                {
                    _isDisconnected = true;
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
                if (_events.Contains(Event.Synchronizing.ToString()))
                {
                    _isSyncing = true;
                    _isDisconnected = false;
                }

                if (!_isSynchronized
                    && (_events.Any(s => s.Contains(Event.NetworkResumed.ToString())) || _events.Any(s => s.Contains(Event.Synchronized.ToString()))))
                {
                    _isSynchronized = true;
                    _isSyncing = false;
                    _isDisconnected = false;
                }

                if (_isSynchronized &&
                    _events.Any(s => s.Contains(Event.NetworkInterrupted.ToString())))
                {
                    _isSynchronized = false;
                    _isAttemptingToReconnect = true;
                }

                if (_events.Any(s => s.Contains(Event.Disconnected.ToString())))
                {
                    _isDisconnected = true;
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

            if (IsTestMode())
            {
                _events.ForEach(evt => Console.WriteLine("event : " + evt));
            }
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
                else
                {
                    Console.WriteLine("NetworkStats : " + status_stats.Info.AsString());
                }
            }
        }

        public StatusImpl AdvanceFrame(Input input)
        {
            var status = GGRSFFI.netplay_advance_frame(input).ToModelGGrsFFI();

            if (!status.IsOk)
            {
                if (status.Info.AsString().Contains("Peer Disconnected!"))
                {
                    _isDisconnected = true;
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
            return _isInit && !_isDisconnected;
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
            return _isSynchronized;
        }

        public bool IsDisconnected()
        {
            return _isDisconnected;
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

        public bool IsSynchronizing()
        {
            return _isSyncing;
        }

        public void Reset()
        {
            if (_isInit)
            {
                using (var status = GGRSFFI.netplay_reset().ToModelGGrsFFI())
                {
                    if (!status.IsOk)
                    {
                        throw new InvalidOperationException($"Reset error : {status.Info.AsString()}");
                    }
                }

                _isInit = false;
                _netplayRequests = new List<NetplayRequest>();
                _events = new List<string>();
                _isSynchronized = false;
                _framesToReSimulate = 0;
                _networkStats = new NetworkStats();
                _isRollbackFrame = false;
                _framesAhead = 0;
                _isDisconnected = true;
                _hasDesynch = false;
                _isAttemptingToReconnect = false;
                _isSyncing = false;
                _isUpdating = false;
                _gameContext.ResetPlayersIndex();
            }
        }

        public GGRSConfig GetConfig()
        {
            return GGRSConfig;
        }

        public void UpdateConfig(GGRSConfig config)
        {
            GGRSConfig = config;
        }

        public void SaveConfig()
        {
            var jsonToSave = JsonConvert.SerializeObject(GGRSConfig, Formatting.Indented);
            File.WriteAllText($"{Directory.GetCurrentDirectory()}\\netplay_conf.json", jsonToSave);
        }

        private void LoadConfig()
        {
            try
            {
                var json = File.ReadAllText("netplay_conf.json");
                GGRSConfig = JsonConvert.DeserializeObject<GGRSConfig>(json);
                if (GGRSConfig.InputDelay == 0)
                {
                    GGRSConfig.InputDelay = 2;
                    SaveConfig();
                }

                if (string.IsNullOrEmpty(GGRSConfig.Name))
                {
                    GGRSConfig.Name = "PLAYER";
                    SaveConfig();
                }

                if (GGRSConfig.Name.Length > 10)
                {
                    GGRSConfig.Name = GGRSConfig.Name.Substring(0, Math.Min(GGRSConfig.Name.Length, 10));
                    SaveConfig();
                }

                GGRSConfig.Name = GGRSConfig.Name.ToUpper();
                if (GGRSConfig.Netplay == null)
                {
                    GGRSConfig.Netplay = new NetplayConfig();
                }
            }
            catch (FileNotFoundException e)
            {
                GGRSConfig = new GGRSConfig
                {
                    InputDelay = 2,
                    Name = "PLAYER",
                    Netplay = new NetplayConfig(),
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
    }
}
