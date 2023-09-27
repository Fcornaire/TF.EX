using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using TF.EX.Common.Extensions;
using TF.EX.Common.Handle;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Models.WebSocket.Client;
using TF.EX.Domain.Models.WebSocket.Server;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Domain.Services
{
    public class MatchmakingService : IMatchmakingService
    {
        private readonly string SERVER_URL = Config.SERVER;
        private string MATCHMAKING_URL => $"{SERVER_URL}/ws";
        private string ROOM_URL => $"{SERVER_URL}/room";

        private ClientWebSocket _webSocket;
        private byte[] _buffer = new byte[1056];
        private string _directCode = string.Empty;
        private bool _hasDirectResponse = false;
        private bool _hasMatched = false;

        private bool _hasRegisteredForQuickPlay = false;
        private bool _hasFoundOpponentForQuickPlay = false;
        private bool _hasAcceptedOpponentInQuickPlay = false;
        private bool _OpponentDeclined = false;
        private bool _isListening = false;
        private int _totalAvailablePlayersInQuickPlayQueue = 0;
        private int _ping = 0;
        private Stopwatch _stopwatch = new Stopwatch();
        private Guid _opponentPeerId = Guid.Empty;
        private bool _hasOpponentChoosed = false;

        private readonly IRngService _rngService;
        private readonly INetplayManager _netplayManager;
        private readonly IArcherService _archerService;
        private readonly ILogger _logger;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSourceLobby = new CancellationTokenSource();
        private CancellationToken cancellationTokenLobby;
        public MatchmakingService(IRngService rngService, INetplayManager netplayManager, IArcherService archerService, ILogger logger)
        {
            _webSocket = new ClientWebSocket();
            _rngService = rngService;
            _netplayManager = netplayManager;
            cancellationToken = cancellationTokenSource.Token;
            cancellationTokenLobby = cancellationTokenSourceLobby.Token;
            _logger = logger;
            _archerService = archerService;
        }

        public bool ConnectToServerAndListen()
        {
            Reset();
            try
            {
                Task.Run(async () =>
                {
                    if (_webSocket.State != WebSocketState.Open)
                    {
                        await _webSocket.ConnectAsync(new Uri(MATCHMAKING_URL), cancellationToken);
                    }
                }).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.LogError<MatchmakingService>("Error when connecting to server", e);
                return false;
            }

            Task.Run(async () =>
            {
                try
                {
                    if (!_isListening)
                    {
                        _isListening = true;
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var segment = new ArraySegment<byte>(_buffer);
                            var result = await _webSocket.ReceiveAsync(segment, cancellationToken);
                            await HandleMessage(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError<MatchmakingService>("Error while listening to server message", ex); //NOT KILLED
                    Reset();
                }
            }, cancellationToken);

            return true;
        }

        public void RegisterForDirect()
        {
            Task.Run(async () =>
            {
                EnsureConnection();
                await SendRegisterMessage();
            });
        }

        public async Task<bool> SendOpponentCode(string text)
        {
            EnsureConnection();
            await SendCode(text);

            _hasDirectResponse = false;

            while (!_hasDirectResponse)
            {
                await Task.Delay(500);
            }

            return _hasMatched;
        }


        public string GetDirectCode()
        {
            return _directCode;
        }

        private async Task SendRegisterMessage()
        {
            var registerMessage = new RegisterDirectMessage();
            registerMessage.RegisterDirect.Name = _netplayManager.GetNetplayMeta().Name;

            var message = JsonSerializer.Serialize(registerMessage);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, cancellationToken);
        }

        private async Task SendCode(string text)
        {
            var matchWithDirectMsg = new MatchWithDirectCodeMessage();
            matchWithDirectMsg.MatchWithDirectCode.Code = text;
            var message = JsonSerializer.Serialize(matchWithDirectMsg);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, cancellationToken);
        }

        private async Task SendRegisterQuickPlayMessage()
        {
            var registerMessage = new RegisterQuickPlayMessage();
            registerMessage.Register.Name = _netplayManager.GetNetplayMeta().Name;

            var message = JsonSerializer.Serialize(registerMessage);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, cancellationToken);
        }

        private async Task SendCancelQuickPlayMessage()
        {
            var cancelMessage = new CancelQuickPlayMessage();

            var message = JsonSerializer.Serialize(cancelMessage);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, cancellationToken);
        }

        private async Task SendAcceptQuickPlayMessage()
        {
            var acceptMessage = new AcceptQuickPlayMessage();

            var message = JsonSerializer.Serialize(acceptMessage);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, cancellationToken);
        }

        private async Task HandleMessage(WebSocketReceiveResult result)
        {
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    break;
                case WebSocketMessageType.Binary:
                    var message = System.Text.Encoding.UTF8.GetString(_buffer, 0, result.Count);
                    HandleMessageContents(message);
                    break;
                case WebSocketMessageType.Close:
                    await CloseAsync();
                    break;
                default:
                    break;
            }
        }

        private void HandleMessageContents(string message)
        {
            if (message.Contains("RegisterDirectResponse"))
            {
                var registerResponse = JsonSerializer.Deserialize<RegisterDirectResponseMessage>(message);

                if (registerResponse.RegisterDirectResponse.Success)
                {
                    _directCode = registerResponse.RegisterDirectResponse.Code;
                }
                else
                {
                    throw new InvalidOperationException("Registering to server failed");
                }
            }

            if (message.Contains("MatchWithDirectCodeResponse"))
            {
                var matchResponse = JsonSerializer.Deserialize<MatchWithDirectCodeResponseMessage>(message);
                if (matchResponse.MatchWithDirectCodeResponse.Success)
                {
                    _hasMatched = true;

                    var roomUrl = $"{ROOM_URL}/{matchResponse.MatchWithDirectCodeResponse.RoomId}";
                    var roomChatUrl = $"{ROOM_URL}/{matchResponse.MatchWithDirectCodeResponse.RoomChatId}";

                    _netplayManager.SetRoomAndServerMode(roomUrl);
                    ConnectAndListenToLobby(roomChatUrl);

                    _rngService.SetSeed(matchResponse.MatchWithDirectCodeResponse.Seed);
                    _netplayManager.UpdatePlayer2Name(matchResponse.MatchWithDirectCodeResponse.OpponentName);
                }
                else
                {
                    throw new InvalidOperationException("Matching with direct code failed");
                }

                _hasDirectResponse = true;
            }

            if (message.Contains("RegisterQuickPlayResponse"))
            {
                var registerResponse = JsonSerializer.Deserialize<RegisterQuickPlayResponseMessage>(message);

                if (registerResponse.RegisterQuickPlayResponse.Success)
                {
                    _hasRegisteredForQuickPlay = registerResponse.RegisterQuickPlayResponse.Success;
                }
                else
                {
                    throw new InvalidOperationException("Registering to server failed");
                }
            }

            if (message.Contains("QuickPlayPossibleMatch"))
            {
                if (!_hasFoundOpponentForQuickPlay)
                {
                    _OpponentDeclined = false;
                    var matchResponse = JsonSerializer.Deserialize<QuickPlayPossibleMatchMessage>(message);

                    var roomUrl = $"{ROOM_URL}/{matchResponse.QuickPlayPossibleMatch.RoomId}";
                    var roomChatUrl = $"{ROOM_URL}/{matchResponse.QuickPlayPossibleMatch.RoomChatId}";

                    _netplayManager.SetRoomAndServerMode(roomUrl);
                    ConnectAndListenToLobby(roomChatUrl);

                    _netplayManager.UpdatePlayer2Name(matchResponse.QuickPlayPossibleMatch.OpponentName);
                    _hasFoundOpponentForQuickPlay = true;
                }
            }

            if (message.Contains("AcceptQuickPlayResponse"))
            {
                var matchResponse = JsonSerializer.Deserialize<AcceptQuickPlayResponseMessage>(message);

                if (matchResponse.AcceptQuickPlayResponse.Success)
                {
                    _hasMatched = true;
                    _rngService.SetSeed(matchResponse.AcceptQuickPlayResponse.Seed);
                    _netplayManager.UpdatePlayer2Name(matchResponse.AcceptQuickPlayResponse.OpponentName);
                    _hasAcceptedOpponentInQuickPlay = true;
                }
                else
                {
                    _logger.LogDebug<MatchmakingService>("Accepting opponent on quick play failed. (Probably a decline)");
                    _hasFoundOpponentForQuickPlay = false;
                    _OpponentDeclined = true;
                    _ping = 0;
                }
            }

            if (message.Contains("DeniedQuickPlay"))
            {
                DisconnectFromLobby();
                _hasFoundOpponentForQuickPlay = false;
                _OpponentDeclined = true;
            }

            if (message.Contains("TotalAvailablePlayersInQuickPlayQueue"))
            {
                var response = JsonSerializer.Deserialize<TotalAvailablePlayersInQuickPlayQueueMessage>(message);
                _totalAvailablePlayersInQuickPlayQueue = response.TotalAvailablePlayersInQuickPlayQueue.Total;
            }
        }


        private void ConnectAndListenToLobby(string roomUrl)
        {
            MatchboxClientFFI.initialize(roomUrl);

            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationTokenLobby.IsCancellationRequested)
                    {
                        var message = MatchboxClientFFI.poll_message();
                        using (var safeBytes = new SafeBytes<IEnumerable<PeerMessage>>(message, () => { MatchboxClientFFI.free_messages(message); }))
                        {
                            var data = safeBytes.ToStruct(false);
                            if (data != null)
                            {
                                HandleLobbyMessage(data);
                            }
                        }
                        await Task.Delay(10);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError<MatchmakingService>($"Error when listenning to opponent message", e);
                    DisconnectFromLobby();
                    _OpponentDeclined = true;
                }
            }, cancellationTokenLobby);

        }

        private void HandleLobbyMessage(IEnumerable<PeerMessage> data)
        {
            try
            {
                foreach (PeerMessage peerMessage in data)
                {
                    switch (peerMessage.Type)
                    {
                        case PeerMessageType.Ping:
                            if (_opponentPeerId == null)
                            {
                                _opponentPeerId = peerMessage.PeerId;
                            }
                            MatchboxClientFFI.send_message(PeerMessageType.Pong.ToString(), peerMessage.PeerId.ToString());
                            break;
                        case PeerMessageType.Pong:
                            if (_opponentPeerId == null)
                            {
                                _opponentPeerId = peerMessage.PeerId;
                            }
                            _stopwatch.Stop();
                            _ping = (int)_stopwatch.ElapsedMilliseconds;

                            Task.Run(async () =>
                            {
                                await Task.Delay(1000);

                                _stopwatch.Restart();
                                MatchboxClientFFI.send_message(PeerMessageType.Ping.ToString(), peerMessage.PeerId.ToString());
                            });

                            break;
                        case PeerMessageType.Archer:
                            var archerMessage = peerMessage.Message.Split(':')[1];

                            _archerService.AddArcher(1, archerMessage);

                            var splitted = archerMessage.Split('-');

                            Enum.TryParse(splitted[1], out ArcherData.ArcherTypes alt);

                            var archer = int.Parse(splitted[0]);
                            TFGame.Characters[1] = archer;
                            TFGame.AltSelect[1] = alt;

                            if (archer == TFGame.Characters[0])
                            {
                                if (TFGame.AltSelect[1] == ArcherData.ArcherTypes.Alt)
                                {
                                    TFGame.AltSelect[0] = ArcherData.ArcherTypes.Normal;
                                }

                                if (TFGame.AltSelect[1] == ArcherData.ArcherTypes.Normal)
                                {
                                    TFGame.AltSelect[0] = ArcherData.ArcherTypes.Alt;
                                }
                            }

                            _hasOpponentChoosed = true;

                            break;
                        case PeerMessageType.Greetings:
                            _opponentPeerId = peerMessage.PeerId;
                            _stopwatch.Restart();
                            MatchboxClientFFI.send_message(PeerMessageType.Ping.ToString(), peerMessage.PeerId.ToString());
                            break;
                        default:
                            _logger.LogError<MatchmakingService>($"Unknown message type received from opponent : {peerMessage.Type}");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError<MatchmakingService>($"Error when handling opponent msg", e);
                DisconnectFromLobby();
                _OpponentDeclined = true;
            }
        }

        public void RegisterForQuickPlay()
        {
            if (!_hasRegisteredForQuickPlay)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        EnsureConnection();
                        await SendRegisterQuickPlayMessage();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError<MatchmakingService>($"Error when registering for quick play", e);
                    }
                });
            }
        }

        public void AcceptOpponentInQuickPlay()
        {
            Task.Run(async () =>
            {
                EnsureConnection();
                await SendAcceptQuickPlayMessage();
            });
        }

        public bool HasRegisteredForQuickPlay()
        {
            return _hasRegisteredForQuickPlay;
        }

        public bool HasFoundOpponentForQuickPlay()
        {
            return _hasFoundOpponentForQuickPlay;
        }

        public void CancelQuickPlay()
        {
            Task.Run(async () =>
            {
                EnsureConnection();
                await SendCancelQuickPlayMessage();
            }).GetAwaiter().GetResult();

        }

        /// <summary>
        /// Close all connections
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {

            cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            _webSocket = new ClientWebSocket();

            cancellationTokenSourceLobby.Cancel();
            cancellationTokenSourceLobby = new CancellationTokenSource();
            cancellationTokenLobby = cancellationTokenSourceLobby.Token;

            Reset();
        }

        public bool HasAcceptedOpponentForQuickPlay()
        {
            return _hasAcceptedOpponentInQuickPlay;
        }

        private void EnsureConnection()
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                ConnectToServerAndListen();
            }
        }

        public bool HasOpponentDeclined()
        {
            return _OpponentDeclined;
        }

        public void DisconnectFromServer()
        {
            Task.Run(CloseAsync).GetAwaiter().GetResult();
        }

        private void Reset()
        {
            _hasDirectResponse = false;
            _hasMatched = false;
            _hasFoundOpponentForQuickPlay = false;
            _hasAcceptedOpponentInQuickPlay = false;
            _hasOpponentChoosed = false;
            _OpponentDeclined = false;
            _hasRegisteredForQuickPlay = false;
            _isListening = false;
            _stopwatch.Reset();
            _ping = 0;
        }

        public int GetTotalAvailablePlayersInQuickPlayQueue()
        {
            return _totalAvailablePlayersInQuickPlayQueue;
        }

        public bool IsConnectedToServer()
        {
            return _webSocket.State == WebSocketState.Open;
        }

        public int GetPingToOpponent()
        {
            return _ping;
        }

        public Guid GetOpponentPeerId()
        {
            return _opponentPeerId;
        }

        public bool HasOpponentChoosed()
        {
            return _hasOpponentChoosed;
        }

        public void DisconnectFromLobby()
        {
            MatchboxClientFFI.disconnect();
            cancellationTokenSourceLobby.Cancel();
            cancellationTokenSourceLobby = new CancellationTokenSource();
            cancellationTokenLobby = cancellationTokenSourceLobby.Token;
            _ping = 0;
            _stopwatch.Reset();
        }
    }
}
