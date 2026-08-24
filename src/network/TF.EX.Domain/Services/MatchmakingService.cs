using HarmonyLib;
using TF.EX.Domain.Interop;
using MessagePack;
using Microsoft.Extensions.Logging;
using Monocle;
using MonoMod.Utils;
using System.Net.WebSockets;
using TF.EX.Common.Extensions;
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

        private ICollection<Lobby> _lobbies = null;
        private Lobby ownLobby = new Lobby();
        private string peerId = string.Empty;
        private int previousPlayersCount = 1;
        private bool hostStartedMatch = false;
        private bool matchEndReported = false;

        private string pendingSpectatorNotice = string.Empty;
        private Lobby pendingRollcallLobby = null;
        private string pendingJoinCode = string.Empty;
        private Lobby privateJoinLobby = null;

        private Action<int> onQuickPlayQueued;
        private Action<Lobby> onQuickPlayMatched;
        private Action<string> onQuickPlayFailed;
        private int searchingCount;
        private bool isQuickPlayQueued;

        private Dictionary<string, Action> onResult = new Dictionary<string, Action>();
        private WSAction currentAction = WSAction.None;

        private readonly INetplayManager _netplayManager;
        private readonly IArcherService _archerService;
        private readonly IInputService _inputService;
        private readonly ILogger _logger;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken cancellationToken;
        public MatchmakingService(INetplayManager netplayManager,
            IArcherService archerService,
            IInputService inputService,
            ILogger logger)
        {
            _webSocket = new ClientWebSocket();
            _netplayManager = netplayManager;
            cancellationToken = cancellationTokenSource.Token;
            _logger = logger;
            _archerService = archerService;
            _inputService = inputService;
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
                    (TFGame.Instance.Scene as MainMenu).State = Context.MenuReturn.NetplayEntry ?? MainMenu.MenuState.VersusOptions;
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
                        if (_webSocket.CloseStatus != null && _webSocket.CloseStatus.HasValue)
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

                    //if (TFGame.Instance.Scene is MainMenu)
                    //{
                    //    (TFGame.Instance.Scene as MainMenu).State = MainMenu.MenuState.VersusOptions;
                    //}
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

        private async Task SendJoinPrivate()
        {
            var joinPrivateMessage = new JoinPrivateMessage
            {
                JoinPrivate = new JoinPrivate
                {
                    Code = pendingJoinCode,
                    Name = _netplayManager.GetNetplayMeta().Name,
                    IsPlayer = true
                }
            };

            var bytes = MessagePackSerializer.Serialize(joinPrivateMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendEnterQuickPlay()
        {
            var enterQuickPlayMessage = new EnterQuickPlayMessage
            {
                EnterQuickPlay = new EnterQuickPlay
                {
                    Name = _netplayManager.GetNetplayMeta().Name,
                    IsWide = ServiceCollections.ResolveWiderSetModApi()?.IsWide == true
                }
            };

            var bytes = MessagePackSerializer.Serialize(enterQuickPlayMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendExitQuickPlay()
        {
            var exitQuickPlayMessage = new ExitQuickPlayMessage { };

            var bytes = MessagePackSerializer.Serialize(exitQuickPlayMessage);
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

        private async Task SendMatchEnded()
        {
            var matchEndedMessage = new MatchEndedMessage { };

            var bytes = MessagePackSerializer.Serialize(matchEndedMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendStartLobbyChoice()
        {
            var startLobbyMessage = new StartLobbyChoiceMessage { };

            var bytes = MessagePackSerializer.Serialize(startLobbyMessage);
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

        private static bool IsServerMsg(string message, string tag)
        {
            return message.StartsWith($"{{\"{tag}\"", StringComparison.Ordinal);
        }

        private async Task HandleMessageContents(string message)
        {
            if (IsServerMsg(message, "KeepAlive"))
            {
                await Send(message);
                return;
            }

            if (IsServerMsg(message, "GetLobbiesResponse"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<GetLobbiesResponseMessage>(bytes);
                _lobbies = response.GetLobbiesResponse.Lobbies;

                onResult["GetLobbies-success"]?.Invoke();
            }

            if (IsServerMsg(message, "CreateLobbyResponse"))
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

            if (IsServerMsg(message, "JoinLobbyResponse"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<JoinLobbyResponseMessage>(bytes);
                if (response.JoinLobbyResponse.Success)
                {
                    _logger.LogDebug<MatchmakingService>("Lobby joined!");

                    peerId = response.JoinLobbyResponse.RoomPeerId.ToString();

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

            if (IsServerMsg(message, "QuickPlayMatchFound"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<QuickPlayMatchFoundMessage>(bytes);

                _logger.LogDebug<MatchmakingService>("Quickplay match found!");

                peerId = response.QuickPlayMatchFound.RoomPeerId.ToString();
                isQuickPlayQueued = false;

                var lobby = response.QuickPlayMatchFound.Lobby;
                UpdateOwnLobby(lobby);

                onQuickPlayMatched?.Invoke(GetOwnLobby());

                return;
            }

            if (IsServerMsg(message, "QuickPlayStatus"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<QuickPlayStatusMessage>(bytes);

                searchingCount = response.QuickPlayStatus.Searching;

                if (response.QuickPlayStatus.Queued)
                {
                    isQuickPlayQueued = true;
                    onQuickPlayQueued?.Invoke(searchingCount);
                }
                else
                {
                    isQuickPlayQueued = false;

                    if (!string.IsNullOrEmpty(response.QuickPlayStatus.Message))
                    {
                        _logger.LogDebug<MatchmakingService>($"Quickplay refused : {response.QuickPlayStatus.Message}");
                        onQuickPlayFailed?.Invoke(response.QuickPlayStatus.Message);
                    }
                }

                return;
            }

            if (IsServerMsg(message, "PrivateJoinResult"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<PrivateJoinResultMessage>(bytes);

                if (response.PrivateJoinResult.Success)
                {
                    _logger.LogDebug<MatchmakingService>("Private lobby joined!");

                    peerId = response.PrivateJoinResult.RoomPeerId.ToString();
                    privateJoinLobby = response.PrivateJoinResult.Lobby;

                    onResult["JoinPrivate-success"]?.Invoke();
                }
                else
                {
                    _logger.LogDebug<MatchmakingService>($"Private lobby join failed! : {response.PrivateJoinResult.Message}");

                    onResult["JoinPrivate-fail"]?.Invoke();
                }

                currentAction = WSAction.None;

                return;
            }

            if (IsServerMsg(message, "PingUpdate"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<PingUpdateMessage>(bytes);

                foreach (var entry in response.PingUpdate.Pings)
                {
                    foreach (var player in ownLobby.Players.Concat(ownLobby.Spectators).Where(pl => pl.Addr == entry.Addr))
                    {
                        player.Ping = entry.Ping;
                    }
                }

                return;
            }

            if (IsServerMsg(message, "LobbyUpdate"))
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

            if (IsServerMsg(message, "LeaveLobbyResponse"))
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

            if (IsServerMsg(message, "LeaveLobbyForce"))
            {
                var mainMenu = new MainMenu(Context.MenuReturn.NetplayEntry ?? MainMenu.MenuState.VersusOptions);
                Engine.Instance.Scene = mainMenu;
                (TFGame.Instance.Scene as Level).Session.MatchSettings.LevelSystem.Dispose();

                Sounds.ui_invalid.Play();
                Notification.Create(mainMenu, "No choice made! dropped from lobby");
                ownLobby = new Lobby();
                matchEndReported = false;
            }

            if (IsServerMsg(message, "RematchLobby"))
            {
                matchEndReported = false;

                if (TFGame.Instance.Scene is Level)
                {
                    if (_netplayManager.IsInit())
                    {
                        _netplayManager.Reset();
                    }

                    _inputService.EnableAllControllers();
                    Sounds.ui_click.Play();
                    Engine.Instance.Scene = new MapScene(MainMenu.RollcallModes.Versus);
                    (TFGame.Instance.Scene as Level).Session.MatchSettings.LevelSystem.Dispose();
                }
            }

            if (IsServerMsg(message, "StartLobby"))
            {
                hostStartedMatch = true;
            }

            if (IsServerMsg(message, "SpectatorJoined"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<SpectatorJoinedMessage>(bytes);
                var spectatorPeerId = response.SpectatorJoined.RoomPeerId;

                _logger.LogDebug<MatchmakingService>($"Spectator {spectatorPeerId} joined the running match");

                if (IsHost())
                {
                    _netplayManager.AddLateSpectator(spectatorPeerId);
                }
            }

            if (IsServerMsg(message, "ArcherSelectLobby"))
            {
                hostStartedMatch = false;
                matchEndReported = false;

                if (TFGame.Instance.Scene is Level)
                {
                    if (_netplayManager.IsInit())
                    {
                        _netplayManager.Reset();
                    }

                    _inputService.EnableAllControllers();
                    Sounds.ui_clickBack.Play();
                    Engine.Instance.Scene = new MainMenu(MainMenu.MenuState.Rollcall);
                    _archerService.Reset();
                    (TFGame.Instance.Scene as Level).Session.MatchSettings.LevelSystem.Dispose();
                }
            }
        }

        public void RestoreArchersFromLobbyIfNeeded()
        {
            if (ownLobby.IsEmpty)
            {
                return;
            }

            _archerService.Reset();

            foreach (var player in ownLobby.Players.OrderBy(entry => entry.Seat))
            {
                if (player.Seat < 0 || player.Seat >= TFGame.Characters.Length)
                {
                    continue;
                }

                TFGame.Characters[player.Seat] = player.ArcherIndex;
                TFGame.AltSelect[player.Seat] = (ArcherData.ArcherTypes)player.ArcherAltIndex;

                _archerService.AddArcher(player.Seat, player);
            }
        }

        private static void NormaliseSeats(Lobby lobby)
        {
            var players = lobby.Players.ToArray();

            if (players.Select(player => player.Seat).Distinct().Count() == players.Length)
            {
                return;
            }

            for (int index = 0; index < players.Length; index++)
            {
                players[index].Seat = index;
            }
        }

        private void HandleLobbyUpdate(Lobby lobby)
        {
            NormaliseSeats(lobby);

            Sounds.ui_clickSpecialAsc.Play();

            if (lobby.GameData.Seed != StateApi.Current.GetSeed())
            {
                StateApi.Current.SetSeed(lobby.GameData.Seed);
            }

            if (lobby.Players.Count == 1 && TFGame.Instance.Scene is Level)
            {
                Sounds.ui_invalid.Play();
                Notification.Create(TFGame.Instance.Scene, "All players left...", 10, 500);
                Task.Run(SendLeaveLobby);

                ownLobby = new Lobby();

                _inputService.EnableAllControllers();
                _inputService.EnsureRemoteController(Math.Max(2, ownLobby.Players.Count));
                _inputService.EnsureEveryControllerSlot();

                ResetPeer();
                return;
            }

            //Leave if host left
            if (!lobby.Players.Any(pl => pl.IsHost))
            {
                if (TFGame.Instance.Scene is MainMenu)
                {
                    (TFGame.Instance.Scene as MainMenu).State = Domain.Models.MenuState.NetplaySelect.ToTFModel();
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

            ApplyTeamsToMatchSettings();

            var seatChanged = _inputService.EnsureLocalControllerSeat();
            var someoneLeft = lobby.Players.Count < previousPlayersCount;

            var scene = TFGame.Instance.Scene;

            var rollCalls = GetRollcallElements(scene);

            if (seatChanged)
            {
                foreach (var rollCall in rollCalls)
                {
                    rollCall.HandleControlChange();
                }
            }

            if (TFGame.Instance.Scene is MainMenu && (TFGame.Instance.Scene as MainMenu).State == MainMenu.MenuState.Rollcall && rollCalls.Any())
            {
                ReconcileRollcall(lobby, rollCalls);
            }
            else
            {
                pendingRollcallLobby = lobby;
            }

            var isSpectatorInLobby = lobby.Spectators.Any(s => s.RoomPeerId == peerId);

            if (isSpectatorInLobby && !_netplayManager.IsSpectatorMode())
            {
                var roomUrl = $"{SERVER_URL}/room/{lobby.RoomId}?peer={peerId}";
                var hostPeerId = lobby.Players.First(pl => pl.IsHost).RoomPeerId;

                _netplayManager.SetSpectatorMode(roomUrl, hostPeerId);
            }

            if (isSpectatorInLobby && lobby.InGame)
            {
                hostStartedMatch = true;
            }

            if (someoneLeft)
            {
                foreach (var rollCall in rollCalls)
                {
                    rollCall.HandleControlChange();
                }
            }

            UpdateOwnLobby(lobby);
        }

        public void ReconcileRollcallIfPending()
        {
            if (pendingRollcallLobby == null
                || TFGame.Instance.Scene is not MainMenu mainMenu
                || mainMenu.State != MainMenu.MenuState.Rollcall)
            {
                return;
            }

            var rollCalls = GetRollcallElements(mainMenu);

            if (!rollCalls.Any())
            {
                return;
            }

            var lobby = pendingRollcallLobby;
            pendingRollcallLobby = null;

            _inputService.EnsureLocalControllerSeat();
            ReconcileRollcall(lobby, rollCalls);
        }

        public void ApplyTeamsToMatchSettings()
        {
            var settings = MainMenu.VersusMatchSettings;

            if (settings?.Teams == null || !ownLobby.IsTeamMode)
            {
                return;
            }

            for (int seat = 0; seat < 4; seat++)
            {
                var player = ownLobby.Players.FirstOrDefault(entry => entry.Seat == seat);

                settings.Teams[seat] = player == null ? Allegiance.Neutral : (Allegiance)player.Team;
            }
        }

        private static RollcallElement[] GetRollcallElements(Monocle.Scene scene)
        {
            return scene.Layers
                .SelectMany(layer => layer.Value.Entities)
                .OfType<RollcallElement>()
                .ToArray();
        }

        private void ReconcileRollcall(Lobby lobby, RollcallElement[] rollCalls)
        {
            var seatedPlayers = lobby.Players
                .Where(player => player.RoomPeerId != peerId)
                .ToArray();

            foreach (var player in seatedPlayers)
            {
                var playerIndex = player.Seat;

                var rollCall = rollCalls.FirstOrDefault(rc =>
                    DynamicData.For(rc).Get<int>("playerIndex") == playerIndex);

                if (rollCall == null)
                {
                    continue;
                }

                var dynRollCall = DynamicData.For(rollCall);
                Monocle.StateMachine state = dynRollCall.Get<Monocle.StateMachine>("state");

                if (previousPlayersCount < lobby.Players.Count)
                {
                    previousPlayersCount = lobby.Players.Count;
                    Notification.Create(TFGame.Instance.Scene, $"{player.Name} joined", 10, 200);
                }

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
                            RoomPeerId = player.RoomPeerId,
                            Seat = player.Seat
                        };

                        TFGame.Characters[playerIndex] = archerIndex;
                        TFGame.AltSelect[playerIndex] = (ArcherData.ArcherTypes)altIndex;
                        _archerService.AddArcher(playerIndex, updatedPlayer);

                        _inputService.EnsureRemoteControllers(lobby.Players.Select(pl => pl.Seat));

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
            }

            foreach (var rollCall in rollCalls)
            {
                var dynVacant = DynamicData.For(rollCall);
                var seat = dynVacant.Get<int>("playerIndex");

                if (lobby.Players.Any(player => player.Seat == seat))
                {
                    continue;
                }

                var vacantState = dynVacant.Get<Monocle.StateMachine>("state");

                if (vacantState.State != 0)
                {
                    _archerService.RemoveArcher(seat);
                    dynVacant.Get<ArcherPortrait>("portrait").Leave();
                    TFGame.Players[seat] = false;
                    vacantState.State = 0;
                }
            }

            if (previousPlayersCount > lobby.Players.Count)
            {
                previousPlayersCount = lobby.Players.Count;
                Notification.Create(TFGame.Instance.Scene, $"Player left", 10, 200);
            }
        }

        public void ResetPeer()
        {
            peerId = "";
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

        public bool IsConnectedToServer()
        {
            return _webSocket.State == WebSocketState.Open;
        }

        public int GetPingTo(Models.WebSocket.Player player)
        {
            var self = ownLobby.Players.Concat(ownLobby.Spectators)
                .FirstOrDefault(pl => pl.RoomPeerId == peerId);

            return (self?.Ping ?? 0) + player.Ping;
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
                case WSAction.JoinPrivate:
                    await SendJoinPrivate();
                    break;
                case WSAction.LeaveLobby:
                    await SendLeaveLobby();
                    break;
                case WSAction.UpdatePlayer:
                    await SendUpdatePlayer(ownLobby.Players.First(pl => pl.RoomPeerId == peerId));
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
            NormaliseSeats(lobby);

            ownLobby = new Lobby
            {
                Name = lobby.Name,
                RoomId = lobby.RoomId,
                MaxPlayers = lobby.MaxPlayers,
                Players = lobby.Players,
                GameData = lobby.GameData,
                Spectators = lobby.Spectators,
                EndGameChoice = lobby.EndGameChoice,
                Mods = lobby.Mods,
                Kind = lobby.Kind,
                JoinCode = lobby.JoinCode
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

        public async Task JoinPrivate(string code, Action<Lobby> onSuccess, Action onFail)
        {
            pendingJoinCode = code;

            await Update(WSAction.JoinPrivate, () => onSuccess(privateJoinLobby), onFail);
        }

        public async Task EnterQuickPlay(Action<int> onQueued, Action<Lobby> onMatched, Action<string> onFail)
        {
            onQuickPlayQueued = onQueued;
            onQuickPlayMatched = onMatched;
            onQuickPlayFailed = onFail;
            searchingCount = 0;

            if (!EnsureConnection())
            {
                onFail?.Invoke("Cannot reach the server");
                return;
            }

            await SendEnterQuickPlay();
        }

        public async Task ExitQuickPlay()
        {
            onQuickPlayQueued = null;
            onQuickPlayMatched = null;
            onQuickPlayFailed = null;
            isQuickPlayQueued = false;

            if (IsConnectedToServer())
            {
                await SendExitQuickPlay();
            }
        }

        public bool IsSearchingQuickPlay()
        {
            return isQuickPlayQueued;
        }

        public int GetSearchingCount()
        {
            return searchingCount;
        }

        public bool IsQuickPlayStarting()
        {
            return ownLobby.IsQuickPlay && IsAwaitingHostStart();
        }

        public int GetLocalSeat()
        {
            var self = ownLobby.Players.FirstOrDefault(player => player.RoomPeerId == peerId);

            return self?.Seat ?? 0;
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
            if (IsSpectator() && ownLobby.InGame)
            {
                return hostStartedMatch;
            }

            return AreAllPlayersReady() && hostStartedMatch;
        }

        public bool IsLobbyFull()
        {
            return ownLobby.Players.Count >= ownLobby.MaxPlayers;
        }

        public bool CanHostStart()
        {
            return !ownLobby.IsQuickPlay && IsAwaitingHostStart() && IsHost();
        }

        public bool IsWaitingForHostStart()
        {
            return !ownLobby.IsQuickPlay && IsAwaitingHostStart() && !IsHost();
        }

        private bool IsAwaitingHostStart()
        {
            return AreAllPlayersReady() && !hostStartedMatch;
        }

        private bool IsHost()
        {
            return ownLobby.Players.Any(pl => pl.IsHost && pl.RoomPeerId == peerId);
        }

        public void RequestStart()
        {
            Task.Run(SendStartLobbyChoice);
        }

        private bool AreAllPlayersReady()
        {
            return ownLobby.Players.Count >= 2
                && ownLobby.Players.All(pl => pl.Ready)
                && AreTeamsPlayable();
        }
        
        private bool AreTeamsPlayable()
        {
            if (!ownLobby.IsTeamMode)
            {
                return true;
            }

            var blue = ownLobby.Players.Count(pl => pl.Team == (int)Allegiance.Blue);
            var red = ownLobby.Players.Count(pl => pl.Team == (int)Allegiance.Red);

            return blue + red == ownLobby.Players.Count && blue > 0 && red > 0;
        }

        public void ResetLobbies()
        {
            _lobbies = null;
        }

        public void QueueSpectatorNotice(string text)
        {
            pendingSpectatorNotice = text;
        }

        public void ShowPendingSpectatorNoticeIfAny()
        {
            if (string.IsNullOrEmpty(pendingSpectatorNotice))
            {
                return;
            }

            var scene = TFGame.Instance.Scene;
            if (scene is not Level && scene is not MainMenu)
            {
                return;
            }

            Notification.Create(scene, pendingSpectatorNotice, 20, 450);
            pendingSpectatorNotice = string.Empty;
        }

        public void ResetLobby()
        {
            hostStartedMatch = false;
            matchEndReported = false;
            ownLobby = new Lobby();
            previousPlayersCount = 1;
            pendingRollcallLobby = null;
        }

        public bool IsSpectator()
        {
            return ownLobby.Spectators.Any(spectator => spectator.RoomPeerId == peerId);
        }

        public async Task RematchChoice()
        {
            await SendRematchChoice();
        }

        public void NotifyMatchEnded()
        {
            if (ownLobby.IsEmpty || matchEndReported)
            {
                return;
            }

            matchEndReported = true;

            Task.Run(async () => await SendMatchEnded());
        }

        public IEnumerable<EndGameStatus> GetEndGameStatus()
        {
            var votes = ownLobby.EndGameChoice ?? new List<EndGameVote>();

            return ownLobby.Players
                .OrderBy(player => player.Seat)
                .Select(player => new EndGameStatus
                {
                    Seat = player.Seat,
                    Name = player.Name,
                    Choice = votes.FirstOrDefault(vote => vote.Addr == player.Addr)?.Choice
                })
                .ToList();
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
        JoinAsSpectator,
        JoinPrivate
    }
}
