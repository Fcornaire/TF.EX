using Microsoft.Extensions.Logging;
using MonoMod.Utils;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using TF.EX.Common.Extensions;
using TF.EX.Common.Handle;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Models.WebSocket.Client;
using TF.EX.Domain.Models.WebSocket.Server;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Domain.Services
{
    public class MatchmakingService : IMatchmakingService
    {
        private readonly string SERVER_URL = Config.SERVER;
        private string MATCHMAKING_URL => $"{SERVER_URL}/ws";

        private ClientWebSocket _webSocket;
        private byte[] _buffer = new byte[1056];

        private int _ping = 0;
        private Stopwatch _stopwatch = new Stopwatch();
        private Guid _opponentPeerId = Guid.Empty;

        private ICollection<Lobby> _lobbies = null;
        private Lobby ownLobby = new Lobby();
        private string peerId = string.Empty;
        private string roomChatPeerId = string.Empty;

        private Dictionary<string, Action> onResult = new Dictionary<string, Action>();
        private WSAction currentAction = WSAction.None;

        private readonly INetplayManager _netplayManager;
        private readonly IArcherService _archerService;
        private readonly ILogger _logger;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSourceLobby = new CancellationTokenSource();
        private CancellationToken cancellationTokenLobby;
        public MatchmakingService(INetplayManager netplayManager, IArcherService archerService, ILogger logger)
        {
            _webSocket = new ClientWebSocket();
            _netplayManager = netplayManager;
            cancellationToken = cancellationTokenSource.Token;
            cancellationTokenLobby = cancellationTokenSourceLobby.Token;
            _logger = logger;
            _archerService = archerService;
        }

        public bool ConnectToServerAndListen()
        {
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

                if (_webSocket.State == WebSocketState.Closed)
                {
                    if (_webSocket.CloseStatus.HasValue)
                    {
                        _logger.LogError<MatchmakingService>($"Connection closed with status {_webSocket.CloseStatus.Value} : {_webSocket.CloseStatusDescription}");
                    }

                    Close();
                }

                if (TFGame.Instance.Scene is MainMenu)
                {
                    (TFGame.Instance.Scene as MainMenu).State = MainMenu.MenuState.VersusOptions;
                }

                return false;
            }

            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var segment = new ArraySegment<byte>(_buffer);
                        using (var ms = new MemoryStream())
                        {
                            WebSocketReceiveResult result;
                            do
                            {
                                result = await _webSocket.ReceiveAsync(segment, cancellationToken);
                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    Close();
                                    return;
                                }

                                ms.Write(_buffer, 0, result.Count);
                            } while (!result.EndOfMessage);

                            var data = ms.ToArray();
                            var message = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);

                            await HandleMessageContents(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError<MatchmakingService>("Error while listening to server message", ex); //NOT KILLED

                    if (_webSocket.State == WebSocketState.Closed)
                    {
                        if (_webSocket.CloseStatus.HasValue)
                        {
                            _logger.LogError<MatchmakingService>($"Connection closed with status {_webSocket.CloseStatus.Value} : {_webSocket.CloseStatusDescription}");
                        }
                    }

                    if (currentAction != WSAction.None)
                    {
                        onResult[$"{currentAction}-fail"]();
                        currentAction = WSAction.None;
                    }
                    else
                    {
                        if (TFGame.Instance.Scene is MainMenu)
                        {
                            var mainMenu = TFGame.Instance.Scene as MainMenu;
                            mainMenu.ButtonGuideA.Clear();
                            mainMenu.ButtonGuideB.Clear();
                            mainMenu.ButtonGuideC.Clear();
                            mainMenu.ButtonGuideD.Clear();
                        }

                        Notification.Create(TFGame.Instance.Scene, "Connexion dropped...");
                    }

                    Close();
                    DisconnectFromLobby();

                    if (TFGame.Instance.Scene is MainMenu)
                    {
                        (TFGame.Instance.Scene as MainMenu).State = MainMenu.MenuState.VersusOptions;
                    }
                }
            }, cancellationToken);

            return true;
        }

        private async Task SendGetLobbies()
        {
            var getLobbiesMessage = new GetLobbiesMessage();

            var message = JsonSerializer.Serialize(getLobbiesMessage);
            await Send(message);
        }

        private async Task SendCreateLobby()
        {
            var createLobbyMessage = new CreateLobbyMessage
            {
                CreateLobby = new CreateLobby
                {
                    Lobby = ownLobby
                }
            };

            var message = JsonSerializer.Serialize(createLobbyMessage);
            await Send(message);
        }

        private async Task SendJoinLobby(string roomId)
        {
            var joinLobbyMessage = new JoinLobbyMessage
            {
                JoinLobby = new JoinLobby
                {
                    RoomId = roomId,
                    Name = _netplayManager.GetNetplayMeta().Name
                }
            };

            var message = JsonSerializer.Serialize(joinLobbyMessage);
            await Send(message);
        }

        private async Task SendUpdatePlayer(Models.WebSocket.Player player)
        {
            var updatePlayerMessage = new UpdatePlayerMessage
            {
                UpdatePlayer = new UpdatePlayer
                {
                    Player = player
                }
            };

            var message = JsonSerializer.Serialize(updatePlayerMessage);
            await Send(message);
        }

        private async Task SendLeaveLobby()
        {
            var leaveLobbyMessage = new LeaveLobbyMessage { };

            var message = JsonSerializer.Serialize(leaveLobbyMessage);
            await Send(message);
        }

        private async Task Send(string msg)
        {
            var segment = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(msg));
            await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, cancellationToken);
        }


        private async Task HandleMessageContents(string message)
        {
            if (message.Contains("KeepAlive"))
            {
                await Send(message);
            }

            if (message.Contains("GetLobbiesResponse"))
            {
                var response = JsonSerializer.Deserialize<GetLobbiesResponseMessage>(message);
                _lobbies = response.GetLobbiesResponse.Lobbies;
                if (currentAction == WSAction.GetLobbies)
                {
                    onResult["GetLobbies-success"]();
                    currentAction = WSAction.None;
                }
            }

            if (message.Contains("CreateLobbyResponse"))
            {
                var response = JsonSerializer.Deserialize<CreateLobbyResponseMessage>(message);
                if (currentAction == WSAction.CreateLobby)
                {
                    if (response.CreateLobbyResponse.Success)
                    {
                        _logger.LogDebug<MatchmakingService>("Lobby created!");
                        ownLobby = response.CreateLobbyResponse.Lobby;
                        onResult["CreateLobby-success"]();
                    }
                    else
                    {
                        _logger.LogDebug<MatchmakingService>($"Lobby creation failed! : {response.CreateLobbyResponse.Message}");
                        onResult["CreateLobby-fail"]();
                    }

                    currentAction = WSAction.None;
                }
            };

            if (message.Contains("JoinLobbyResponse"))
            {
                if (currentAction == WSAction.JoinLobby)
                {
                    var response = JsonSerializer.Deserialize<JoinLobbyResponseMessage>(message);
                    if (response.JoinLobbyResponse.Success)
                    {
                        _logger.LogDebug<MatchmakingService>("Lobby joined!");

                        peerId = response.JoinLobbyResponse.RoomPeerId.ToString();
                        roomChatPeerId = response.JoinLobbyResponse.RoomChatPeerId.ToString();

                        onResult["JoinLobby-success"]();
                    }
                    else
                    {
                        _logger.LogDebug<MatchmakingService>($"Lobby join failed! : {response.JoinLobbyResponse.Message}");
                        onResult["JoinLobby-fail"]();
                    }

                    currentAction = WSAction.None;
                }
            };

            if (message.Contains("LobbyUpdate"))
            {
                //Needed because UpdatePlayer response is a LobbyUpdate , but not necessarely all the time
                if (currentAction == WSAction.UpdatePlayer)
                {
                    onResult["UpdatePlayer-success"]();
                    currentAction = WSAction.None;
                }

                _logger.LogDebug<MatchmakingService>("Lobby updated!");

                var response = JsonSerializer.Deserialize<LobbyUpdateMessage>(message);

                var lobby = response.LobbyUpdate.Lobby;

                HandleLobbyUpdate(lobby);
            }

            if (message.Contains("LeaveLobbyResponse"))
            {
                if (currentAction != WSAction.None)
                {
                    var response = JsonSerializer.Deserialize<LeaveLobbyResponseMessage>(message);
                    if (response.LeaveLobbyResponse.Success)
                    {
                        _logger.LogDebug<MatchmakingService>("Lobby left!");
                        onResult["LeaveLobby-success"]();
                    }
                    else
                    {
                        _logger.LogDebug<MatchmakingService>($"Lobby leave failed! : {response.LeaveLobbyResponse.Message}");
                        onResult["LeaveLobby-fail"]();
                    }

                    currentAction = WSAction.None;
                }
            }
        }

        private void HandleLobbyUpdate(Lobby lobby)
        {
            Sounds.ui_altCostumeShift.Play();

            //Leave if host left
            if (!lobby.Players.Any(pl => pl.IsHost))
            {
                if (TFGame.Instance.Scene is MainMenu)
                {
                    (TFGame.Instance.Scene as MainMenu).State = Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                }
                ResetPeer();
                DisconnectFromLobby();
                return;
            }

            var scene = TFGame.Instance.Scene;

            var rollCalls = scene.Layers.SelectMany(layer => layer.Value.Entities)
                .Where(ent => ent is RollcallElement).Select(ent => ent as RollcallElement);

            if (TFGame.Instance.Scene is MainMenu && (TFGame.Instance.Scene as MainMenu).State == MainMenu.MenuState.Rollcall && rollCalls.Any())
            {
                int playerIndex = 1;
                foreach (var player in lobby.Players.Where(pl => pl.RoomChatPeerId != roomChatPeerId))
                {
                    var rollCall = rollCalls.First(rc =>
                    {
                        var dyn = DynamicData.For(rc);
                        var index = dyn.Get<int>("playerIndex");

                        return index == playerIndex;
                    });

                    var dynRollCall = DynamicData.For(rollCall);
                    Monocle.StateMachine state = dynRollCall.Get<Monocle.StateMachine>("state");

                    if (player.Ready)
                    {
                        if (state.State == 0)
                        {
                            TFGame.Characters[playerIndex] = player.ArcherIndex;
                            TFGame.AltSelect[playerIndex] = (ArcherData.ArcherTypes)player.ArcherAltIndex;
                            _archerService.AddArcher(playerIndex, player);

                            state.State = 1;
                        }
                    }
                    else
                    {
                        if (state.State == 1)
                        {
                            _archerService.RemoveArcher(playerIndex);

                            var portrait = dynRollCall.Get<ArcherPortrait>("portrait");
                            portrait.Leave();
                            TFGame.Players[playerIndex] = false;

                            state.State = 0;
                        }
                    }

                    playerIndex++;
                }
            }

            //True for 2 player...
            if (lobby.Players.Count == 1)
            {
                //This is to trigger portrait update on disconnected player
                foreach (var rollCall in rollCalls)
                {
                    rollCall.HandleControlChange();
                }
            }


            UpdateOwnLobby(lobby);
        }

        public void ResetPeer()
        {
            roomChatPeerId = "";
            peerId = "";
        }

        public void ConnectAndListenToLobby(string roomUrl)
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
                        await Task.Delay(TFGame.FrameTime);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError<MatchmakingService>($"Error when listenning to opponent message", e);
                    DisconnectFromLobby();
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
                            if (_opponentPeerId == Guid.Empty)
                            {
                                _opponentPeerId = peerMessage.PeerId;
                            }
                            MatchboxClientFFI.send_message(PeerMessageType.Pong.ToString(), peerMessage.PeerId.ToString());
                            break;
                        case PeerMessageType.Pong:
                            if (_opponentPeerId == Guid.Empty)
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
            }
        }

        /// <summary>
        /// Close all connections
        /// </summary>
        /// <returns></returns>
        public void Close()
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

        private bool EnsureConnection()
        {
            if (_webSocket.State == WebSocketState.Closed || _webSocket.State == WebSocketState.Aborted)
            {
                Close();
            }

            if (_webSocket.State != WebSocketState.Open)
            {
                return ConnectToServerAndListen();
            }

            return true;
        }

        public void DisconnectFromServer()
        {
            Close();
        }

        private void Reset()
        {
            _stopwatch.Reset();
            _opponentPeerId = Guid.Empty;
            _ping = 0;
        }

        public bool IsConnectedToServer()
        {
            return _webSocket.State == WebSocketState.Open;
        }

        //TODO: refactor to handle multiple opponent
        public int GetPingToOpponent()
        {
            return _ping;
        }

        public Guid GetOpponentPeerId()
        {
            return _opponentPeerId;
        }

        public void DisconnectFromLobby()
        {
            MatchboxClientFFI.disconnect();
            cancellationTokenSourceLobby.Cancel();
            cancellationTokenSourceLobby = new CancellationTokenSource();
            cancellationTokenLobby = cancellationTokenSourceLobby.Token;
            _ping = 0;
            _stopwatch.Reset();
            _opponentPeerId = Guid.Empty;
        }

        private async Task Update(WSAction action, Action onSuccess, Action onFail)
        {
            var actionName = action.ToString();
            if (onResult.ContainsKey($"{actionName}-success"))
            {
                onResult[$"{actionName}-success"] = onSuccess;
            }
            else
            {
                onResult.Add($"{actionName}-success", onSuccess);
            }

            if (onResult.ContainsKey($"{actionName}-fail"))
            {
                onResult[$"{actionName}-fail"] = onFail;
            }
            else
            {
                onResult.Add($"{actionName}-fail", onFail);
            }

            if (!EnsureConnection())
            {
                onFail();
                return;
            }

            currentAction = action;

            switch (action)
            {
                case WSAction.GetLobbies:
                    await SendGetLobbies();
                    break;
                case WSAction.CreateLobby:
                    await SendCreateLobby();
                    break;
                case WSAction.JoinLobby:
                    await SendJoinLobby(ownLobby.RoomId);
                    break;
                case WSAction.LeaveLobby:
                    await SendLeaveLobby();
                    break;
                case WSAction.UpdatePlayer:
                    await SendUpdatePlayer(ownLobby.Players.First(pl => pl.RoomChatPeerId == roomChatPeerId));
                    break;
                case WSAction.None:
                    break;
            }
        }

        public async Task GetLobbies(Action onSuccess, Action onFail)
        {
            await Update(WSAction.GetLobbies, onSuccess, onFail);
        }

        public IEnumerable<Lobby> GetLobbies()
        {
            return _lobbies;
        }

        public Lobby GetOwnLobby()
        {
            return ownLobby;
        }

        public void UpdateOwnLobby(Domain.Models.WebSocket.Lobby lobby)
        {
            ownLobby = new Lobby
            {
                Name = lobby.Name,
                RoomId = lobby.RoomId,
                RoomChatId = lobby.RoomChatId,
                Players = lobby.Players,
                GameData = lobby.GameData
            };
        }

        public async Task CreateLobby(Action onSucess, Action onFail)
        {
            await Update(WSAction.CreateLobby, onSucess, onFail);
        }

        public async Task JoinLobby(string roomId, Action onSucess, Action onFail)
        {
            ownLobby.RoomId = roomId;
            await Update(WSAction.JoinLobby, onSucess, onFail);
        }

        public string GetRoomChatPeerId()
        {
            return roomChatPeerId;
        }

        public async Task UpdatePlayer(Models.WebSocket.Player player, Action onSucess, Action onFail)
        {
            await Update(WSAction.UpdatePlayer, onSucess, onFail);
        }

        public async Task LeaveLobby(Action onSucess, Action onFail)
        {
            await Update(WSAction.LeaveLobby, onSucess, onFail);
        }

        public bool IsLobbyReady()
        {
            return ownLobby.Players.Count >= 2 && ownLobby.Players.All(pl => pl.Ready);
        }

        public void ResetLobbies()
        {
            _lobbies = null;
        }

        public void ResetLobby()
        {
            ownLobby = new Lobby();
        }
    }

    public enum WSAction
    {
        None,
        GetLobbies,
        CreateLobby,
        JoinLobby,
        LeaveLobby,
        UpdatePlayer
    }
}
