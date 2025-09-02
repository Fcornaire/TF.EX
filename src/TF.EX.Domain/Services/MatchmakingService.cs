using HarmonyLib;
using MessagePack;
using Microsoft.Extensions.Logging;
using Monocle;
using MonoMod.Utils;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using TF.EX.Common.Extensions;
using TF.EX.Common.Handle;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
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
        private readonly IInputService _inputService;
        private readonly IRngService _rngService;
        private readonly ILogger _logger;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSourceLobby = new CancellationTokenSource();
        private CancellationToken cancellationTokenLobby;
        public MatchmakingService(INetplayManager netplayManager,
            IArcherService archerService,
            IInputService inputService,
            IRngService rngService,
            ILogger logger)
        {
            _webSocket = new ClientWebSocket();
            _netplayManager = netplayManager;
            cancellationToken = cancellationTokenSource.Token;
            cancellationTokenLobby = cancellationTokenSourceLobby.Token;
            _logger = logger;
            _archerService = archerService;
            _inputService = inputService;
            _rngService = rngService;
        }

        public bool ConnectToServerAndListen()
        {
            try
            {
                Task.Run(async () =>
                {
                    if (_webSocket.State != WebSocketState.Open)
                    {
                        if (!IPAddress.TryParse(SERVER_URL.Split('/', '/', ':')[3], out var _))
                        {
                            using var webClient = new WebClient();
                            var ipv4 = webClient.DownloadString("https://ipv4.icanhazip.com");
                            var ipv6 = string.Empty;
                            try
                            {
                                ipv6 = webClient.DownloadString("https://ipv6.icanhazip.com");
                            }
                            catch (Exception)
                            {
                                _logger.LogDebug<MatchmakingService>("Network/computer does not support ipv6");
                                ipv6 = string.Empty;
                            }

                            _webSocket.Options.SetRequestHeader("x-tfex-real-ipv4", ipv4);

                            if (!string.IsNullOrEmpty(ipv6))
                            {
                                _webSocket.Options.SetRequestHeader("x-tfex-real-ipv6", ipv6);
                            }
                        }

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
                    _logger.LogError<MatchmakingService>("Error while listening to server message", ex);
                    _logger.LogError<MatchmakingService>("inner exception", ex.InnerException);

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

                        Sounds.ui_clickSpecial.Play(160, 5);
                        Notification.Create(TFGame.Instance.Scene, "Connection dropped");
                    }

                    Close();
                    if (!IsSpectator())
                    {
                        DisconnectFromLobby();
                    }

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

            var bytes = MessagePackSerializer.Serialize(getLobbiesMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
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

            var bytes = MessagePackSerializer.Serialize(createLobbyMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendJoinLobby(string roomId)
        {
            var joinLobbyMessage = new JoinLobbyMessage
            {
                JoinLobby = new JoinLobby
                {
                    RoomId = roomId,
                    Name = _netplayManager.GetNetplayMeta().Name,
                    IsPlayer = currentAction == WSAction.JoinLobby
                }
            };

            var bytes = MessagePackSerializer.Serialize(joinLobbyMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
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

            var bytes = MessagePackSerializer.Serialize(updatePlayerMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendLeaveLobby()
        {
            var leaveLobbyMessage = new LeaveLobbyMessage { };

            var bytes = MessagePackSerializer.Serialize(leaveLobbyMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendRematchChoice()
        {
            var lobbyRematchMessage = new RematchLobbyChoiceMessage { };

            var bytes = MessagePackSerializer.Serialize(lobbyRematchMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendArcherSelectChoice()
        {
            var sendArcherSelectChoice = new ArcherSelectChoiceMessage { };

            var bytes = MessagePackSerializer.Serialize(sendArcherSelectChoice);
            var message = MessagePackSerializer.ConvertToJson(bytes);
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
                return;
            }

            if (message.Contains("GetLobbiesResponse"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<GetLobbiesResponseMessage>(bytes);
                _lobbies = response.GetLobbiesResponse.Lobbies;

                onResult["GetLobbies-success"]?.Invoke();
            }

            if (message.Contains("CreateLobbyResponse"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<CreateLobbyResponseMessage>(bytes);
                if (currentAction == WSAction.CreateLobby)
                {
                    if (response.CreateLobbyResponse.Success)
                    {
                        _logger.LogDebug<MatchmakingService>("Lobby created!");
                        ownLobby = response.CreateLobbyResponse.Lobby;
                        peerId = ownLobby.Players.First(pl => pl.IsHost).RoomPeerId.ToString();
                        roomChatPeerId = ownLobby.Players.First(pl => pl.IsHost).RoomChatPeerId.ToString();
                        onResult["CreateLobby-success"]?.Invoke();
                    }
                    else
                    {
                        _logger.LogDebug<MatchmakingService>($"Lobby creation failed! : {response.CreateLobbyResponse.Message}");
                        onResult["CreateLobby-fail"]?.Invoke();
                    }

                    currentAction = WSAction.None;
                }
            }
            ;

            if (message.Contains("JoinLobby"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<JoinLobbyResponseMessage>(bytes);
                if (response.JoinLobbyResponse.Success)
                {
                    _logger.LogDebug<MatchmakingService>("Lobby joined!");

                    peerId = response.JoinLobbyResponse.RoomPeerId.ToString();
                    roomChatPeerId = response.JoinLobbyResponse.RoomChatPeerId.ToString();

                    if (currentAction == WSAction.JoinLobby)
                    {
                        onResult["JoinLobby-success"]?.Invoke();
                    }
                    else
                    {
                        onResult["JoinAsSpectator-success"]?.Invoke();
                    }
                }
                else
                {
                    _logger.LogDebug<MatchmakingService>($"Lobby join failed! : {response.JoinLobbyResponse.Message}");

                    if (currentAction == WSAction.JoinAsSpectator)
                    {
                        onResult["JoinAsSpectator-fail"]?.Invoke();
                    }
                    else
                    {
                        onResult["JoinLobby-fail"]?.Invoke();
                    }
                }

                currentAction = WSAction.None;
            }
            ;

            if (message.Contains("LobbyUpdate"))
            {
                //Needed because UpdatePlayer response is a LobbyUpdate , but not necessarely all the time
                if (currentAction == WSAction.UpdatePlayer)
                {
                    onResult["UpdatePlayer-success"]?.Invoke();
                    currentAction = WSAction.None;
                }

                _logger.LogDebug<MatchmakingService>("Lobby updated!");

                var bytes = MessagePackSerializer.ConvertFromJson(message);

                var response = MessagePackSerializer.Deserialize<LobbyUpdateMessage>(bytes);

                var lobby = response.LobbyUpdate.Lobby;

                ownLobby = lobby;
                HandleLobbyUpdate(lobby);
            }

            if (message.Contains("LeaveLobbyResponse"))
            {
                if (currentAction != WSAction.None)
                {
                    var bytes = MessagePackSerializer.ConvertFromJson(message);

                    var response = MessagePackSerializer.Deserialize<LeaveLobbyResponseMessage>(bytes);
                    if (response.LeaveLobbyResponse.Success)
                    {
                        _logger.LogDebug<MatchmakingService>("Lobby left!");
                        onResult["LeaveLobby-success"]?.Invoke();
                    }
                    else
                    {
                        _logger.LogDebug<MatchmakingService>($"Lobby leave failed! : {response.LeaveLobbyResponse.Message}");
                        onResult["LeaveLobby-fail"]?.Invoke();
                    }

                    currentAction = WSAction.None;
                }
            }

            if (message.Contains("LeaveLobbyForce"))
            {
                var mainMenu = new MainMenu(MainMenu.MenuState.VersusOptions);
                Engine.Instance.Scene = mainMenu;
                (TFGame.Instance.Scene as Level).Session.MatchSettings.LevelSystem.Dispose();

                Sounds.ui_invalid.Play();
                Notification.Create(mainMenu, "All players left...");
                ownLobby = new Lobby();
            }

            if (message.Contains("RematchLobby"))
            {
                if (TFGame.Instance.Scene is Level)
                {
                    _inputService.EnableAllControllers();
                    Sounds.ui_click.Play();
                    Engine.Instance.Scene = new MapScene(MainMenu.RollcallModes.Versus);
                    (TFGame.Instance.Scene as Level).Session.MatchSettings.LevelSystem.Dispose();
                }
            }

            if (message.Contains("ArcherSelect"))
            {
                if (TFGame.Instance.Scene is Level)
                {
                    _inputService.EnableAllControllers();
                    Sounds.ui_clickBack.Play();
                    Engine.Instance.Scene = new MainMenu(MainMenu.MenuState.Rollcall);
                    _archerService.Reset();
                    (TFGame.Instance.Scene as Level).Session.MatchSettings.LevelSystem.Dispose();
                }
            }
        }

        private void HandleLobbyUpdate(Lobby lobby)
        {
            Sounds.ui_altCostumeShift.Play();

            if (lobby.GameData.Seed != _rngService.GetSeed())
            {
                _rngService.SetSeed(lobby.GameData.Seed);
            }

            if (lobby.Players.Count == 1 && TFGame.Instance.Scene is Level)
            {
                Sounds.ui_invalid.Play();
                Notification.Create(TFGame.Instance.Scene, "All players left...", 10, 500);
                Task.Run(SendLeaveLobby);

                ownLobby = new Lobby();

                _inputService.EnableAllControllers();

                ResetPeer();
                return;
            }

            //Leave if host left
            if (!lobby.Players.Any(pl => pl.IsHost))
            {
                if (TFGame.Instance.Scene is MainMenu)
                {
                    (TFGame.Instance.Scene as MainMenu).State = Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                    ownLobby = new Lobby();
                }

                if (TFGame.Instance.Scene is Level)
                {
                    Sounds.ui_invalid.Play();
                    Notification.Create(TFGame.Instance.Scene, "Host left the game...");
                    ownLobby = new Lobby();
                }

                ResetPeer();
                return;
            }

            var scene = TFGame.Instance.Scene;

            var rollCalls = scene.Layers.SelectMany(layer => layer.Value.Entities)
                .Where(ent => ent is RollcallElement).Select(ent => ent as RollcallElement);

            if (TFGame.Instance.Scene is MainMenu && (TFGame.Instance.Scene as MainMenu).State == MainMenu.MenuState.Rollcall && rollCalls.Any())
            {
                int playerIndex = IsSpectator() ? 0 : 1;
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
                            var usedArchers = lobby.Players.Select(pl => pl.ArcherIndex);

                            (var archerIndex, var altIndex) = ArcherDataExtensions.EnsureArcherDataExist(player.ArcherIndex, player.ArcherAltIndex, usedArchers);

                            var updatedPlayer = new Domain.Models.WebSocket.Player
                            {
                                ArcherIndex = archerIndex,
                                ArcherAltIndex = altIndex,
                                IsHost = player.IsHost,
                                Name = player.Name,
                                Ready = player.Ready,
                                RoomChatPeerId = player.RoomChatPeerId,
                                RoomPeerId = player.RoomPeerId
                            };

                            TFGame.Characters[playerIndex] = archerIndex;
                            TFGame.AltSelect[playerIndex] = (ArcherData.ArcherTypes)altIndex;
                            _archerService.AddArcher(playerIndex, updatedPlayer);

                            _inputService.EnsureRemoteController(); //TODO: should be sooner

                            var input = Traverse.Create(rollCall).Field("input").GetValue<PlayerInput>();
                            if (input == null)
                            {
                                Traverse.Create(rollCall).Field("input").SetValue(TFGame.PlayerInputs[playerIndex]);
                            }

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

            if (IsSpectator() && !_netplayManager.IsSpectatorMode())
            {
                var roomUrl = $"{SERVER_URL}/room/{lobby.RoomId}";
                var hostPeerId = lobby.Players.First(pl => pl.IsHost).RoomPeerId;

                _netplayManager.SetSpectatorMode(roomUrl, hostPeerId);
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
                        using (var safeBytes = new SafeBytes<List<PeerMessage>>(message, () => { MatchboxClientFFI.free_messages(message); }))
                        {
                            var data = safeBytes.ToStruct(true);
                            if (data != null && data.Count > 0)
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
                    PeerMessageType type;

                    if (Enum.TryParse(peerMessage.Type, out type))
                    {
                        switch (type)
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
                    else
                    {
                        _logger.LogError<MatchmakingService>($"Unknown message type received from opponent : {peerMessage.Type}");
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
                case WSAction.JoinAsSpectator:
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
                GameData = lobby.GameData,
                Spectators = lobby.Spectators
            };
        }

        public async Task CreateLobby(Action onSucess, Action onFail)
        {
            await Update(WSAction.CreateLobby, onSucess, onFail);
        }

        public async Task JoinLobby(string roomId, bool isPlayer, Action onSucess, Action onFail)
        {
            ownLobby.RoomId = roomId;
            var action = isPlayer ? WSAction.JoinLobby : WSAction.JoinAsSpectator;

            await Update(action, onSucess, onFail);
        }

        public string GetRoomChatPeerId()
        {
            return roomChatPeerId;
        }

        public string GetRoomPeerId()
        {
            return peerId;
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

        public bool IsSpectator()
        {
            return ownLobby.Spectators.Any(spectator => spectator.RoomPeerId == peerId);
        }

        public async Task RematchChoice()
        {
            await SendRematchChoice();
        }

        public async Task ArcherSelectChoice()
        {
            await SendArcherSelectChoice();
        }
    }

    public enum WSAction
    {
        None,
        GetLobbies,
        CreateLobby,
        JoinLobby,
        LeaveLobby,
        UpdatePlayer,
        JoinAsSpectator
    }
}
