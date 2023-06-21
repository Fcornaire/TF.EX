using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.WebSockets;
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

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSourceLobby = new CancellationTokenSource();
        private CancellationToken cancellationTokenLobby;
        public MatchmakingService(IRngService rngService, INetplayManager netplayManager)
        {
            _webSocket = new ClientWebSocket();
            _rngService = rngService;
            _netplayManager = netplayManager;
            cancellationToken = cancellationTokenSource.Token;
            cancellationTokenLobby = cancellationTokenSourceLobby.Token;
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
                        await _webSocket.ConnectAsync(new Uri(MATCHMAKING_URL), CancellationToken.None);
                    }
                }).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
                            var result = await _webSocket.ReceiveAsync(segment, CancellationToken.None);
                            await HandleMessage(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Listenning message : " + ex.Message);
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
            registerMessage.RegisterDirect.Name = _netplayManager.GetConfig().Name;

            var message = JsonConvert.SerializeObject(registerMessage);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private async Task SendCode(string text)
        {
            var matchWithDirectMsg = new MatchWithDirectCodeMessage();
            matchWithDirectMsg.MatchWithDirectCode.Code = text;
            var message = JsonConvert.SerializeObject(matchWithDirectMsg);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private async Task SendRegisterQuickPlayMessage()
        {
            var registerMessage = new RegisterQuickPlayMessage();
            registerMessage.Register.Name = _netplayManager.GetConfig().Name;

            var message = JsonConvert.SerializeObject(registerMessage);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private async Task SendCancelQuickPlayMessage()
        {
            var cancelMessage = new CancelQuickPlayMessage();

            var message = JsonConvert.SerializeObject(cancelMessage);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private async Task SendAcceptQuickPlayMessage()
        {
            var acceptMessage = new AcceptQuickPlayMessage();

            var message = JsonConvert.SerializeObject(acceptMessage);
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
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
                var registerResponse = JsonConvert.DeserializeObject<RegisterDirectResponseMessage>(message);

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
                var matchResponse = JsonConvert.DeserializeObject<MatchWithDirectCodeResponseMessage>(message);
                if (matchResponse.MatchWithDirectCodeResponse.Success)
                {
                    _hasMatched = true;

                    var roomUrl = $"{ROOM_URL}/{matchResponse.MatchWithDirectCodeResponse.RoomId}";
                    var roomChatUrl = $"{ROOM_URL}/{matchResponse.MatchWithDirectCodeResponse.RoomChatId}";

                    _netplayManager.SetRoom(roomUrl);
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
                var registerResponse = JsonConvert.DeserializeObject<RegisterQuickPlayResponseMessage>(message);

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
                _OpponentDeclined = false;
                var matchResponse = JsonConvert.DeserializeObject<QuickPlayPossibleMatchMessage>(message);

                var roomUrl = $"{ROOM_URL}/{matchResponse.QuickPlayPossibleMatch.RoomId}";
                var roomChatUrl = $"{ROOM_URL}/{matchResponse.QuickPlayPossibleMatch.RoomChatId}";

                _netplayManager.SetRoom(roomUrl);
                ConnectAndListenToLobby(roomChatUrl);

                _netplayManager.UpdatePlayer2Name(matchResponse.QuickPlayPossibleMatch.OpponentName);
                _hasFoundOpponentForQuickPlay = true;
            }

            if (message.Contains("AcceptQuickPlayResponse"))
            {
                var matchResponse = JsonConvert.DeserializeObject<AcceptQuickPlayResponseMessage>(message);
                if (matchResponse.AcceptQuickPlayResponse.Success)
                {
                    _hasMatched = true;
                    _rngService.SetSeed(matchResponse.AcceptQuickPlayResponse.Seed);
                    _netplayManager.UpdatePlayer2Name(matchResponse.AcceptQuickPlayResponse.OpponentName);
                }
                else
                {
                    throw new InvalidOperationException("Accepting  opponent on quick play failed");
                }

                _hasAcceptedOpponentInQuickPlay = true;
            }

            if (message.Contains("DeniedQuickPlay"))
            {
                MatchboxClientFFI.disconnect();
                _hasFoundOpponentForQuickPlay = false;
                _OpponentDeclined = true;
            }

            if (message.Contains("TotalAvailablePlayersInQuickPlayQueue"))
            {
                var response = JsonConvert.DeserializeObject<TotalAvailablePlayersInQuickPlayQueueMessage>(message);
                _totalAvailablePlayersInQuickPlayQueue = response.TotalAvailablePlayersInQuickPlayQueue.Total;
            }
        }

        public void StopRoomCommunication()
        {
            cancellationTokenSourceLobby.Cancel();
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
                        await Task.Delay(100);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception lobby msg : {e}");
                }
            }, cancellationTokenLobby);

        }

        private void HandleLobbyMessage(IEnumerable<PeerMessage> data)
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
                        _ping = (int)_stopwatch.ElapsedMilliseconds / 2;
                        _stopwatch.Restart();
                        MatchboxClientFFI.send_message(PeerMessageType.Ping.ToString(), peerMessage.PeerId.ToString());

                        break;
                    case PeerMessageType.Archer:
                        var archerMessage = peerMessage.Message.Split(':')[1];

                        var splitted = archerMessage.Split('-');

                        Enum.TryParse(splitted[1], out ArcherData.ArcherTypes alt);

                        TFGame.Characters[1] = int.Parse(splitted[0]);
                        TFGame.AltSelect[1] = alt;
                        _hasOpponentChoosed = true;

                        break;
                    case PeerMessageType.Greetings:
                        _opponentPeerId = peerMessage.PeerId;
                        _stopwatch.Start();
                        MatchboxClientFFI.send_message(PeerMessageType.Ping.ToString(), peerMessage.PeerId.ToString());
                        break;
                    default:
                        Console.WriteLine($"Unknown message type {peerMessage.Type}");
                        break;
                }
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
                        Console.WriteLine(e);
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
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            MatchboxClientFFI.disconnect();

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
            if (_webSocket.State == WebSocketState.Open)
            {
                Task.Run(async () =>
                    {
                        await CloseAsync();
                    }).GetAwaiter().GetResult();
            }
        }

        private void Reset()
        {
            _hasDirectResponse = false;
            _hasMatched = false;
            _hasFoundOpponentForQuickPlay = false;
            _hasAcceptedOpponentInQuickPlay = false;
            _OpponentDeclined = false;
            _hasRegisteredForQuickPlay = false;
            _isListening = false;
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
    }
}
