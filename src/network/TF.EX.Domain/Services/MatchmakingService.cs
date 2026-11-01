using HarmonyLib;
using MessagePack;
using Microsoft.Extensions.Logging;
using Monocle;
using MonoMod.Utils;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using TF.EX.Common.Extensions;
using TF.EX.Domain.Context;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.Skin;
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
        private static string SERVER_URL => NetplayPreferences.Server;
        private string MATCHMAKING_URL => $"{SERVER_URL}/ws?version={ServiceCollections.ResolveAutoUpdater().GetCurrentVersion()}";

        private readonly SemaphoreSlim _sendGate = new(1, 1);
        private Task _skinBundleSendQueue = Task.CompletedTask;
        private HashSet<string> _skinKnownPeers = [];

        private ClientWebSocket _webSocket;
        private byte[] _buffer = new byte[1056];

        private ICollection<Lobby> _lobbies = null;
        private Lobby ownLobby = new();
        private string peerId = string.Empty;
        private int previousPlayersCount = 1;
        private readonly HashSet<string> modifiedFilesNotified = [];
        private readonly HashSet<string> platformNotified = [];
        private Dictionary<string, string> rollcallMembers;
        private bool hasHostStartedMatch = false;
        private DateTime lastStartRequestAt = DateTime.MinValue;
        private static readonly TimeSpan StartRequestCooldown = TimeSpan.FromSeconds(1);
        private bool isMatchEndReported = false;

        private string pendingSpectatorNotice = string.Empty;
        private Lobby pendingRollcallLobby = null;
        private readonly ConcurrentQueue<Action> gameThreadActions = new();
        private string pendingJoinCode = string.Empty;
        private bool pendingJoinAsPlayer = true;
        private Lobby privateJoinLobby = null;

        private Action<int> onQuickPlayQueued;
        private Action<Lobby> onQuickPlayMatched;
        private const string CONNECTION_LOST_MESSAGE = "Lost connection to the server";

        private Action<string> onQuickPlayFailed;
        private int searchingStandardCount;
        private int searchingWideCount;
        private bool isSearchWide;
        private bool isQuickPlayQueued;

        private Action<Lobby> onRankedMatched;
        private Action onRankedReady;
        private Action<string> onRankedRequeued;
        private Action<string> onRankedFailed;

        private static readonly TimeSpan LeaderboardCacheDuration = TimeSpan.FromSeconds(60);
        private Action<LeaderboardData> onLeaderboard;
        private LeaderboardData cachedLeaderboard;
        private DateTime cachedLeaderboardAt = DateTime.MinValue;

        private Action<PlayerProfile> onPlayerProfile;
        private int? pendingProfileRank;

        private static readonly Microsoft.Xna.Framework.Color EnabledTint = new(205, 245, 205);
        private static readonly Microsoft.Xna.Framework.Color DisabledTint = new(250, 200, 200);
        private static readonly Microsoft.Xna.Framework.Color NeutralTint = Microsoft.Xna.Framework.Color.White;

        private Dictionary<string, Action> onResult = [];
        private WSAction currentAction = WSAction.None;

        private readonly INetplayManager _netplayManager;
        private readonly IArcherService _archerService;
        private readonly IInputService _inputService;
        private readonly ISkinStreamService _skinStreamService;
        private readonly ILogger _logger;

        private CancellationTokenSource cancellationTokenSource = new();
        private CancellationToken cancellationToken;

        private string abandonedRoomId = null;

        private string pingMeasurementUrl = null;
        private string relayPingMeasurementUrl = null;
        private string measuredPingStatsRoomId = null;
        private readonly Dictionary<string, bool> measuredPingStatsRelayed = [];
        private readonly Dictionary<string, PingStats> measuredPingStats = [];
        private readonly HashSet<string> reportedMeasuredPeers = [];

        public MatchmakingService(INetplayManager netplayManager,
            IArcherService archerService,
            IInputService inputService,
            ISkinStreamService skinStreamService,
            ILogger logger)
        {
            _webSocket = new ClientWebSocket();
            _netplayManager = netplayManager;
            cancellationToken = cancellationTokenSource.Token;
            _logger = logger;
            _archerService = archerService;
            _inputService = inputService;
            _skinStreamService = skinStreamService;
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

                RunOnGameThread(() =>
                {
                    if (TFGame.Instance.Scene is MainMenu mainMenu)
                    {
                        mainMenu.State = Models.MenuState.NetplaySelect.ToTFModel();
                    }
                });

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
                                    RunOnGameThread(() => FailSearch());
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
                        onResult[$"{currentAction}-fail"]?.Invoke();
                        currentAction = WSAction.None;
                    }
                    else
                    {
                        RunOnGameThread(() =>
                        {
                            if (FailSearch())
                            {
                                return;
                            }

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
                        });
                    }

                    Close();

                    //if (TFGame.Instance.Scene is MainMenu)
                    //{
                    //    (TFGame.Instance.Scene as MainMenu).State = MainMenu.MenuState.VersusOptions;
                    //}
                }
            }, cancellationToken);

            try
            {
                Task.Run(async () => await SendIdentity()).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.LogError<MatchmakingService>("Error when sending identity", e);
            }

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
                    Lobby = ownLobby,
                    LocalPlayers = _inputService.GetLocalPlayerCount()
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
                    Name = NetplayPreferences.Name,
                    IsPlayer = currentAction == WSAction.JoinLobby,
                    LocalPlayers = _inputService.GetLocalPlayerCount()
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
                    Name = NetplayPreferences.Name,
                    IsPlayer = pendingJoinAsPlayer,
                    LocalPlayers = _inputService.GetLocalPlayerCount()
                }
            };

            var bytes = MessagePackSerializer.Serialize(joinPrivateMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendIdentity()
        {
            var playerId = SteamIdentity.TryGet() ?? NetplayPreferences.PlayerId;

            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }

            var identifyMessage = new IdentifyMessage
            {
                Identify = new Identify
                {
                    PlayerId = playerId,
                    Name = NetplayPreferences.Name
                }
            };

            var bytes = MessagePackSerializer.Serialize(identifyMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendEnterQuickPlay()
        {
            isSearchWide = ServiceCollections.ResolveWiderSetModApi()?.IsWide == true;

            var enterQuickPlayMessage = new EnterQuickPlayMessage
            {
                EnterQuickPlay = new EnterQuickPlay
                {
                    Name = NetplayPreferences.Name,
                    IsWide = isSearchWide,
                    LocalPlayers = _inputService.GetLocalPlayerCount()
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

        private async Task SendEnterRanked()
        {
            isSearchWide = ServiceCollections.ResolveWiderSetModApi()?.IsWide == true;

            var enterRankedMessage = new EnterRankedMessage
            {
                EnterRanked = new EnterRanked
                {
                    Name = NetplayPreferences.Name,
                    IsWide = isSearchWide
                }
            };

            var bytes = MessagePackSerializer.Serialize(enterRankedMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendExitRanked()
        {
            var exitRankedMessage = new ExitRankedMessage { };

            var bytes = MessagePackSerializer.Serialize(exitRankedMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendGetLeaderboard()
        {
            var getLeaderboardMessage = new GetLeaderboardMessage();

            var bytes = MessagePackSerializer.Serialize(getLeaderboardMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendGetPlayerProfile(int? rank)
        {
            var getPlayerProfileMessage = new GetPlayerProfileMessage
            {
                GetPlayerProfile = new GetPlayerProfile { Rank = rank }
            };

            var bytes = MessagePackSerializer.Serialize(getPlayerProfileMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendUpdatePlayer(Models.WebSocket.Player player)
        {
            player.HaveModifiedGameFiles = Utils.GameFilesIntegrity.IsModified;
            player.Platform = Utils.Platform.Current;
            player.InputDelay = _netplayManager.GetEffectiveInputDelay();

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

        private async Task SendLeaveLobby(bool isLinkLost = false)
        {
            var leaveLobbyMessage = new LeaveLobbyMessage { LeaveLobby = new LeaveLobby { IsLinkLost = isLinkLost } };

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

        private async Task SendMatchEnded(int winnerSeat, IList<int> scores, IList<SeatStats> stats, int? tower)
        {
            var matchEndedMessage = new MatchEndedMessage
            {
                MatchEnded = new MatchEnded
                {
                    WinnerSeat = winnerSeat,
                    Frame = ExFlags.CurrentFrame,
                    Checksum = LastStateChecksum(),
                    Scores = scores?.ToList(),
                    Stats = stats?.ToList(),
                    Tower = tower
                }
            };

            var bytes = MessagePackSerializer.Serialize(matchEndedMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendSeriesContinueChoice()
        {
            var continueMessage = new SeriesContinueChoiceMessage { };

            var bytes = MessagePackSerializer.Serialize(continueMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendSeriesPickMap(int mapId)
        {
            var pickMessage = new SeriesPickMapMessage
            {
                SeriesPickMap = new SeriesPickMap { MapId = mapId }
            };

            var bytes = MessagePackSerializer.Serialize(pickMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private static string LastStateChecksum()
        {
            var state = ExFlags.LastCapturedState;

            return state != null
                ? Convert.ToHexString(SHA256.HashData(state))
                : null;
        }

        private async Task SendStartLobbyChoice()
        {
            var startLobbyMessage = new StartLobbyChoiceMessage { };

            var bytes = MessagePackSerializer.Serialize(startLobbyMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        private async Task SendPeerMeasured(string roomPeerId)
        {
            var peerMeasuredMessage = new PeerMeasuredMessage
            {
                PeerMeasured = new PeerMeasured { RoomPeerId = roomPeerId }
            };

            var bytes = MessagePackSerializer.Serialize(peerMeasuredMessage);
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

            await _sendGate.WaitAsync(cancellationToken);

            try
            {
                await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, cancellationToken);
            }
            finally
            {
                _sendGate.Release();
            }
        }

        public void PublishCustomSkin(Models.WebSocket.Player player)
        {
            if (player.ArcherIndex < Extensions.ArcherDataExtensions.VanillaArcherCount || IsSpectator())
            {
                return;
            }

            var bundle = _skinStreamService.GetOrBuildArcherBundle(player.ArcherIndex, player.ArcherAltIndex);

            if (bundle == null)
            {
                return;
            }

            _skinStreamService.MarkPublished(player.LocalIndex, bundle);

            QueueSkinBundle(bundle);
        }

        private void QueueSkinBundle(ArtifactSkinBundle bundle)
        {
            _skinBundleSendQueue = _skinBundleSendQueue.ContinueWith(_ => SendSkinBundle(bundle), TaskScheduler.Default).Unwrap();
        }

        private async Task SendSkinBundle(ArtifactSkinBundle bundle)
        {
            try
            {
                var chunkCount = (uint)((bundle.Bytes.Length + Models.Skin.SkinLimits.MaxChunkBytes - 1) / Models.Skin.SkinLimits.MaxChunkBytes);

                if (chunkCount == 0 || chunkCount > Models.Skin.SkinLimits.MaxChunks)
                {
                    return;
                }

                for (uint index = 0; index < chunkCount; index++)
                {
                    var offset = (int)index * Models.Skin.SkinLimits.MaxChunkBytes;
                    var length = Math.Min(Models.Skin.SkinLimits.MaxChunkBytes, bundle.Bytes.Length - offset);

                    var message = new SkinChunkMessage
                    {
                        SkinChunk = new SkinChunkSend
                        {
                            Chunk = new SkinChunk
                            {
                                BundleId = bundle.BundleId,
                                CustomArcherId = bundle.CustomArcherId,
                                ChunkIndex = index,
                                ChunkCount = chunkCount,
                                Data = Convert.ToBase64String(bundle.Bytes, offset, length),
                            }
                        }
                    };

                    var bytes = MessagePackSerializer.Serialize(message);
                    await Send(MessagePackSerializer.ConvertToJson(bytes));
                }

                _logger.LogDebug<MatchmakingService>($"Skin bundle {bundle.CustomArcherId} sent ({bundle.Bytes.Length} bytes, {chunkCount} chunks)");
            }
            catch (Exception e)
            {
                _logger.LogError<MatchmakingService>("Failed to send the skin bundle", e);
            }
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

            if (IsServerMsg(message, "SkinChunk"))
            {
                try
                {
                    var bytes = MessagePackSerializer.ConvertFromJson(message);
                    var chunkMsg = MessagePackSerializer.Deserialize<SkinChunkServerMessage>(bytes);

                    if (chunkMsg?.SkinChunk != null && chunkMsg.SkinChunk.From != peerId)
                    {
                        _skinStreamService.ReceiveChunk(chunkMsg.SkinChunk.From, chunkMsg.SkinChunk.Chunk);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError<MatchmakingService>("Failed to handle a skin chunk", e);
                }

                return;
            }

            if (IsServerMsg(message, "Leaderboard"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<LeaderboardMessage>(bytes);

                cachedLeaderboard = response.Leaderboard;
                cachedLeaderboardAt = DateTime.UtcNow;

                var callback = onLeaderboard;
                onLeaderboard = null;
                callback?.Invoke(cachedLeaderboard);

                return;
            }

            if (IsServerMsg(message, "PlayerProfile"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<PlayerProfileMessage>(bytes);

                if (response.PlayerProfile.Rank != pendingProfileRank)
                {
                    return;
                }

                var callback = onPlayerProfile;
                onPlayerProfile = null;
                callback?.Invoke(response.PlayerProfile.Profile);

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

                    if (response.JoinLobbyResponse.Lobby != null)
                    {
                        UpdateOwnLobby(response.JoinLobbyResponse.Lobby);
                    }

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

                searchingStandardCount = response.QuickPlayStatus.SearchingStandard;
                searchingWideCount = response.QuickPlayStatus.SearchingWide;

                if (response.QuickPlayStatus.Queued)
                {
                    isQuickPlayQueued = true;
                    onQuickPlayQueued?.Invoke(GetSearchingCount());
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

            if (IsServerMsg(message, "RankedMatchFound"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<RankedMatchFoundMessage>(bytes);
                searchingStandardCount = 0;
                searchingWideCount = 0;

                if (onRankedMatched == null)
                {
                    await SendLeaveLobby();
                    return;
                }

                _logger.LogDebug<MatchmakingService>("Ranked opponent found, checking the connection");

                peerId = response.RankedMatchFound.RoomPeerId.ToString();

                UpdateOwnLobby(response.RankedMatchFound.Lobby);

                onRankedMatched?.Invoke(GetOwnLobby());

                return;
            }

            if (IsServerMsg(message, "RankedReady"))
            {
                onRankedReady?.Invoke();

                return;
            }

            if (IsServerMsg(message, "RankedStatus"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var status = MessagePackSerializer.Deserialize<RankedStatusMessage>(bytes).RankedStatus;
                searchingStandardCount = status.SearchingStandard;
                searchingWideCount = status.SearchingWide;

                if (status.Queued && !string.IsNullOrEmpty(status.Message))
                {
                    RunOnGameThread(() =>
                    {
                        ResetLobby();
                        ResetPeer();
                    });
                    onRankedRequeued?.Invoke(status.Message);
                }
                else if (!status.Queued && !string.IsNullOrEmpty(status.Message))
                {
                    _logger.LogDebug<MatchmakingService>($"Ranked refused : {status.Message}");
                    onRankedFailed?.Invoke(status.Message);
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

                var previous = ownLobby;
                ownLobby = lobby;
                RunOnGameThread(() => HandleLobbyUpdate(lobby, previous));
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

            if (IsServerMsg(message, "RematchLobby"))
            {
                isMatchEndReported = false;

                RunOnGameThread(() =>
                {
                    if (TFGame.Instance.Scene is Level)
                    {
                        if (_netplayManager.IsInit())
                        {
                            ServiceCollections.ResolveReplayService().Export();
                            _netplayManager.Reset();
                        }

                        _inputService.EnableAllControllers();
                        Sounds.ui_click.Play();
                        Engine.Instance.Scene = new MapScene(MainMenu.RollcallModes.Versus);
                        (TFGame.Instance.Scene as Level).Session.MatchSettings.LevelSystem.Dispose();
                    }
                });
            }

            if (IsServerMsg(message, "StartLobby"))
            {
                hasHostStartedMatch = true;
            }

            if (IsServerMsg(message, "SpectatorJoined"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<SpectatorJoinedMessage>(bytes);
                var spectatorPeerId = response.SpectatorJoined.RoomPeerId;

                _logger.LogDebug<MatchmakingService>($"Spectator {spectatorPeerId} joined the running match");

                if (IsHost())
                {
                    RunOnGameThread(() => _netplayManager.AddLateSpectator(spectatorPeerId));
                }
            }

            if (IsServerMsg(message, "ArcherSelectLobby"))
            {
                hasHostStartedMatch = false;
                isMatchEndReported = false;

                RunOnGameThread(() => ReturnToMenu(MainMenu.MenuState.Rollcall));
            }

            if (IsServerMsg(message, "KickedFromLobby"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var response = MessagePackSerializer.Deserialize<KickedFromLobbyMessage>(bytes);
                var reason = response.KickedFromLobby.Reason;

                _logger.LogDebug<MatchmakingService>($"Removed from the lobby : {reason}");

                RunOnGameThread(() => HandleKickedFromLobby(reason));
            }

            if (IsServerMsg(message, "Notice"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var notice = MessagePackSerializer.Deserialize<NoticeMessage>(bytes).Notice.Text;

                _logger.LogDebug<MatchmakingService>($"Server notice : {notice}");

                RunOnGameThread(() => Notification.Create(GetNextScene(), notice, 10, 600));
            }

            if (IsServerMsg(message, "UpdateRequired"))
            {
                var bytes = MessagePackSerializer.ConvertFromJson(message);
                var version = MessagePackSerializer.Deserialize<UpdateRequiredMessage>(bytes).UpdateRequired.Version;

                _logger.LogDebug<MatchmakingService>($"Server requires TF.EX {version}");

                RunOnGameThread(() => HandleUpdateRequired(version));
            }

            if (IsServerMsg(message, "SeriesLobby"))
            {
                hasHostStartedMatch = false;
                isMatchEndReported = false;

                RunOnGameThread(() => ReturnToMenu(Models.MenuState.SeriesLobby.ToTFModel()));
            }
        }

        private void ReturnToMenu(MainMenu.MenuState state)
        {
            if (TFGame.Instance.Scene is not Level level)
            {
                return;
            }

            if (_netplayManager.IsInit())
            {
                ServiceCollections.ResolveReplayService().Export();
                _netplayManager.Reset();
            }

            _inputService.EnableAllControllers();
            Sounds.ui_clickBack.Play();
            Engine.Instance.Scene = new MainMenu(state);
            _archerService.Reset();
            level.Session.MatchSettings.LevelSystem.Dispose();
        }

        public bool IsInUnfinishedRankedGame()
        {
            return ownLobby.IsRanked && TFGame.Instance.Scene is Level level && level.Session.GetWinner() == -1;
        }

        public void LeaveRankedGameOnLinkLost(string message)
        {
            abandonedRoomId = ownLobby.RoomId;
            ResetLobby();
            ResetPeer();
            Task.Run(() => SendLeaveLobby(isLinkLost: true));

            ServiceCollections.ResolveReplayService().Export();
            Sounds.ui_invalid.Play();

            Notification.Create(ExitToRankedMenu((Level)TFGame.Instance.Scene), message, 10, 500);
        }

        private MainMenu ExitToRankedMenu(Level level)
        {
            _netplayManager.Reset();
            _inputService.ResetPolledInput();
            _inputService.EnableAllControllers();
            _inputService.RebindLocalInput();

            return level.GoToNetplayEntryMenu(Models.MenuState.RankedSelect);
        }

        private static Scene GetNextScene()
        {
            return DynamicData.For(TFGame.Instance).Get<Scene>("nextScene") ?? TFGame.Instance.Scene;
        }

        private void HandleKickedFromLobby(string reason)
        {
            var wasRanked = ownLobby.IsRanked;
            abandonedRoomId = ownLobby.RoomId;
            ResetLobby();
            ResetPeer();

            Sounds.ui_invalid.Play();
            Notification.Create(TFGame.Instance.Scene, $"{reason} - REMOVED FROM LOBBY".ToUpperInvariant(), 10, 500);

            if (TFGame.Instance.Scene is MainMenu mainMenu)
            {
                mainMenu.State = (wasRanked ? Domain.Models.MenuState.RankedSelect : Domain.Models.MenuState.NetplaySelect).ToTFModel();
            }
        }

        private void HandleUpdateRequired(string version)
        {
            ResetLobby();
            ResetPeer();
            onQuickPlayQueued = null;
            onQuickPlayMatched = null;
            onQuickPlayFailed = null;
            isQuickPlayQueued = false;
            onRankedMatched = null;
            onRankedReady = null;
            onRankedRequeued = null;
            onRankedFailed = null;
            currentAction = WSAction.None;

            if (Version.TryParse(version, out var required))
            {
                UpdateRequest.RequiredVersion = required;
            }
            else
            {
                Sounds.ui_invalid.Play();
                Notification.Create(TFGame.Instance.Scene, $"ONLINE PLAY REQUIRES TF.EX {version}", 10, 500);
            }

            if (TFGame.Instance.Scene is MainMenu mainMenu)
            {
                mainMenu.RemoveLoader();
                mainMenu.State = MainMenu.MenuState.Main;
            }
        }

        public bool IsSeriesPicker()
        {
            var series = ownLobby.Series;

            return series != null
                && series.IsInProgress
                && series.PickerSide.HasValue
                && !IsSpectator()
                && GetLocalPlayers().Any(player => series.GetSideOfSeat(player.Seat) == series.PickerSide);
        }

        public void RestoreArchersFromLobbyIfNeeded()
        {
            if (ownLobby.IsEmpty)
            {
                return;
            }

            _archerService.Reset();

            var resolvedArchers = ArcherDataExtensions.ResolveArchers(ownLobby);

            foreach (var player in ownLobby.SessionPlayers.OrderBy(entry => entry.Seat))
            {
                if (player.Seat < 0 || player.Seat >= TFGame.Characters.Length)
                {
                    continue;
                }

                (var archerIndex, var altIndex) = resolvedArchers.TryGetValue(player.Seat, out var resolved)
                    ? resolved
                    : (player.ArcherIndex, player.ArcherAltIndex);

                TFGame.Characters[player.Seat] = archerIndex;
                TFGame.AltSelect[player.Seat] = (ArcherData.ArcherTypes)altIndex;

                _archerService.AddArcher(player.Seat, player.WithResolvedArcher(archerIndex, altIndex));
            }
        }

        private static void NormaliseSeats(ICollection<Models.WebSocket.Player> seated)
        {
            var players = seated.ToArray();

            if (players.Select(player => player.Seat).Distinct().Count() == players.Length)
            {
                return;
            }

            for (int index = 0; index < players.Length; index++)
            {
                players[index].Seat = index;
            }
        }

        private void HandleLobbyUpdate(Lobby lobby, Lobby previous)
        {
            if (!string.IsNullOrEmpty(abandonedRoomId) && lobby.RoomId == abandonedRoomId)
            {
                ownLobby = new Lobby();
                return;
            }

            NormaliseSeats(lobby.Players);
            NormaliseSeats(lobby.MatchPlayers);

            if (Context.LobbyBuilderContext.IsEditing)
            {
                lobby.MaxPlayers = previous.MaxPlayers;
                lobby.GameData.MapId = previous.GameData.MapId;
                lobby.GameData.Mode = previous.GameData.Mode;
                lobby.GameData.CustomMode = previous.GameData.CustomMode;
                lobby.GameData.MatchLength = previous.GameData.MatchLength;
                lobby.GameData.Variants = previous.GameData.Variants;
                lobby.GameData.BestOf = previous.GameData.BestOf;
            }

            Sounds.ui_clickSpecialAsc.Play();

            if (lobby.GameData.Seed != StateApi.Current.GetSeed())
            {
                StateApi.Current.SetSeed(lobby.GameData.Seed);
            }

            if (lobby.Players.Count == 1 && TFGame.Instance.Scene is Level level)
            {
                var isRankedGameAborted = lobby.IsRanked && level.Session.GetWinner() == -1;

                Sounds.ui_invalid.Play();
                if (!isRankedGameAborted)
                {
                    Notification.Create(TFGame.Instance.Scene, "All players left...", 10, 500);
                }

                Task.Run(() => SendLeaveLobby());
                ServiceCollections.ResolveReplayService().Export();

                abandonedRoomId = lobby.RoomId;
                ownLobby = new Lobby();

                _inputService.EnableAllControllers();
                _inputService.EnsureRemoteController(Math.Max(2, ownLobby.Players.Count));
                _inputService.EnsureEveryControllerSlot();

                ResetPeer();

                if (isRankedGameAborted)
                {
                    ExitToRankedMenu(level);
                }

                return;
            }

            //Leave if host left
            if (!lobby.Players.Any(pl => pl.IsHost))
            {
                abandonedRoomId = lobby.RoomId;

                if (TFGame.Instance.Scene is MainMenu)
                {
                    (TFGame.Instance.Scene as MainMenu).State = Domain.Models.MenuState.NetplaySelect.ToTFModel();
                    ownLobby = new Lobby();
                }

                if (TFGame.Instance.Scene is Level)
                {
                    Sounds.ui_invalid.Play();
                    Notification.Create(TFGame.Instance.Scene, "Host left the game...");
                    ServiceCollections.ResolveReplayService().Export();
                    ownLobby = new Lobby();
                }

                ResetPeer();
                return;
            }

            if (KickSpectatorIfMissingVariants(lobby))
            {
                return;
            }

            if (TFGame.Instance.Scene is MainMenu)
            {
                ApplyTeamsToMatchSettings();
            }

            ApplyGameSettingsToMatch(lobby);

            NotifySeriesAbort(lobby, previous);
            NotifyConnectionUpdate(lobby, previous);
            PublishSettingsChanges(lobby, previous);
            UnreadyOwnRollcall(lobby, previous);

            var seatChanged = _inputService.EnsureLocalControllerSeats();
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

            pendingRollcallLobby = lobby;

            var isSpectatorInLobby = lobby.Spectators.Any(s => s.RoomPeerId == peerId);

            if (isSpectatorInLobby && !_netplayManager.IsSpectatorMode())
            {
                var roomUrl = $"{SERVER_URL}/room/{lobby.RoomId}?peer={peerId}";
                var hostPeerId = lobby.Players.First(pl => pl.IsHost).RoomPeerId;

                _netplayManager.SetSpectatorMode(roomUrl, hostPeerId);
            }

            if (isSpectatorInLobby && lobby.InGame)
            {
                hasHostStartedMatch = true;
            }

            if (isSpectatorInLobby && !lobby.InGame && lobby.IsSeriesInProgress
                && TFGame.Instance.Scene is MainMenu spectatorMenu && spectatorMenu.State == MainMenu.MenuState.Rollcall)
            {
                spectatorMenu.State = Models.MenuState.SeriesLobby.ToTFModel();
            }

            if (someoneLeft)
            {
                foreach (var rollCall in rollCalls)
                {
                    rollCall.HandleControlChange();
                }
            }

            ResendSkinToNewMembers(lobby);

            UpdateOwnLobby(lobby);
        }

        private void ResendSkinToNewMembers(Lobby lobby)
        {
            var members = lobby.Players.Concat(lobby.Spectators)
                .Select(member => member.RoomPeerId)
                .Where(id => !string.IsNullOrEmpty(id) && id != peerId)
                .ToHashSet();

            var hasNewMember = members.Any(id => !_skinKnownPeers.Contains(id));

            _skinKnownPeers = members;

            if (!hasNewMember)
            {
                return;
            }

            foreach (var published in _skinStreamService.GetPublished())
            {
                QueueSkinBundle(published);
            }
        }

        public void RunOnGameThread(Action action)
        {
            gameThreadActions.Enqueue(action);
        }

        public void DrainGameThreadActions()
        {
            while (gameThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    _logger.LogError<MatchmakingService>("Error while handling deferred server message", e);
                }
            }
        }

        private void ApplyGameSettingsToMatch(Lobby lobby)
        {
            var settings = MainMenu.VersusMatchSettings;

            if (settings == null
                || lobby.InGame
                || lobby.IsQuickPlay
                || lobby.IsRanked
                || Context.LobbyBuilderContext.IsEditing
                || TFGame.Instance.Scene is not MainMenu)
            {
                return;
            }

            settings.Variants.ApplyVariants(lobby.GameData.Variants);
            settings.MatchLength = (MatchSettings.MatchLengths)lobby.GameData.MatchLength;
        }

        private void NotifySeriesAbort(Lobby lobby, Lobby previous)
        {
            if (TFGame.Instance.Scene is MainMenu menu && menu.State.ToDomainModel() == Models.MenuState.RankedSearch)
            {
                return;
            }

            if (lobby.Series?.IsAborted != true || previous?.Series?.IsAborted == true || previous?.RoomId != lobby.RoomId)
            {
                return;
            }

            Sounds.ui_invalid.Play();

            var text = lobby.IsRanked && lobby.Series.AbortReason == Models.WebSocket.Series.OpponentLeft
                ? "YOUR OPPONENT LEFT"
                : $"SERIES ABORTED: {lobby.Series.AbortReason ?? "UNKNOWN"}";

            Notification.Create(TFGame.Instance.Scene, text, 12, 500);
        }

        private void NotifyConnectionUpdate(Lobby lobby, Lobby previous)
        {
            if (previous?.RoomId != lobby.RoomId || lobby.InGame || IsSpectator() || GetRelayedPeersFrom(lobby).SetEquals(GetRelayedPeersFrom(previous)))
            {
                return;
            }

            Notification.Create(TFGame.Instance.Scene, "CONNECTION UPDATED", 10, 300);
        }

        private void PublishSettingsChanges(Lobby lobby, Lobby previous)
        {
            if (previous == null || previous.IsEmpty || previous.RoomId != lobby.RoomId || lobby.InGame)
            {
                return;
            }

            var variants = MainMenu.VersusMatchSettings?.Variants;

            var added = lobby.GameData.Variants.Except(previous.GameData.Variants).ToList();
            var removed = previous.GameData.Variants.Except(lobby.GameData.Variants).ToList();

            var anyChange = added.Count > 0
                || removed.Count > 0
                || lobby.GameData.MapId != previous.GameData.MapId
                || lobby.MaxPlayers != previous.MaxPlayers
                || lobby.GameData.MatchLength != previous.GameData.MatchLength
                || lobby.GameData.Mode != previous.GameData.Mode
                || lobby.GameData.CustomMode != previous.GameData.CustomMode
                || lobby.GameData.BestOf != previous.GameData.BestOf;

            if (!anyChange)
            {
                return;
            }

            LobbyUpdateFeed.Push("LOBBY SETTINGS UPDATED", null, NeutralTint);

            foreach (var title in added)
            {
                var icon = variants?.FindVariantIcon(title);

                if (icon == null && variants != null && variants.MissingVariants([title]).Count > 0)
                {
                    LobbyUpdateFeed.Push($"MISSING {title}", null, DisabledTint);
                    continue;
                }

                LobbyUpdateFeed.Push(title, icon, EnabledTint);
            }

            foreach (var title in removed)
            {
                LobbyUpdateFeed.Push(title, variants?.FindVariantIcon(title), DisabledTint);
            }

            if (lobby.GameData.MapId != previous.GameData.MapId)
            {
                if (lobby.GameData.MapId >= 0 && lobby.GameData.MapId < TowerFall.GameData.VersusTowers.Count)
                {
                    var theme = TowerFall.GameData.VersusTowers[lobby.GameData.MapId].Theme;

                    LobbyUpdateFeed.Push(theme.Name, theme.Icon, NeutralTint);
                }
                else
                {
                    LobbyUpdateFeed.Push("RANDOM MAP", null, NeutralTint);
                }
            }

            if (lobby.MaxPlayers != previous.MaxPlayers)
            {
                LobbyUpdateFeed.Push($"LOBBY SIZE {lobby.MaxPlayers}", null, NeutralTint);
            }

            if (lobby.GameData.MatchLength != previous.GameData.MatchLength)
            {
                LobbyUpdateFeed.Push($"{(MatchSettings.MatchLengths)lobby.GameData.MatchLength} MATCH".ToUpperInvariant(), null, NeutralTint);
            }

            if (lobby.GameData.Mode != previous.GameData.Mode || lobby.GameData.CustomMode != previous.GameData.CustomMode)
            {
                LobbyUpdateFeed.Push(Interop.NetplayVersusModes.Resolve(lobby.GameData.CustomMode)?.VersusGameMode.Name.ToUpperInvariant() ?? ModeName(lobby.GameData.Mode), null, NeutralTint);
            }

            if (lobby.GameData.BestOf != previous.GameData.BestOf)
            {
                LobbyUpdateFeed.Push(lobby.GameData.BestOf > 0 ? $"SERIES BEST OF {lobby.GameData.BestOf}" : "SERIES OFF", null, NeutralTint);
            }
        }

        private static string ModeName(int mode)
        {
            return (TowerFall.Modes)mode switch
            {
                TowerFall.Modes.LastManStanding => "LAST MAN STANDING",
                TowerFall.Modes.HeadHunters => "HEADHUNTERS",
                TowerFall.Modes.TeamDeathmatch => "TEAM DEATHMATCH",
                _ => ((TowerFall.Modes)mode).ToString().ToUpperInvariant(),
            };
        }

        private bool KickSpectatorIfMissingVariants(Lobby lobby)
        {
            if (lobby.InGame || !lobby.Spectators.Any(s => s.RoomPeerId == peerId))
            {
                return false;
            }

            var variants = MainMenu.VersusMatchSettings?.Variants;

            if (variants == null)
            {
                return false;
            }

            var missing = variants.MissingVariants(lobby.GameData.Variants);

            if (Interop.NetplayVersusModes.MissingMode(lobby) is string missingMode)
            {
                missing.Insert(0, missingMode);
            }

            if (missing.Count == 0)
            {
                return false;
            }

            abandonedRoomId = lobby.RoomId;
            Task.Run(() => SendLeaveLobby());
            ownLobby = new Lobby();

            Sounds.ui_invalid.Play();
            Notification.Create(TFGame.Instance.Scene, $"MISSING {missing[0]} , REMOVED FROM LOBBY", 10, 450);

            if (TFGame.Instance.Scene is MainMenu mainMenu)
            {
                mainMenu.State = Domain.Models.MenuState.LobbyBrowser.ToTFModel();
            }

            ResetPeer();

            return true;
        }

        private void UnreadyOwnRollcall(Lobby lobby, Lobby previous)
        {
            if (TFGame.Instance.Scene is not MainMenu mainMenu || mainMenu.State != MainMenu.MenuState.Rollcall || IsSpectator())
            {
                return;
            }

            var rollCalls = GetRollcallElements(mainMenu);

            foreach (var ownPlayer in lobby.Players.Where(pl => pl.RoomPeerId == peerId && !pl.Ready))
            {
                var previousSelf = previous?.Players?.FirstOrDefault(pl => pl.RoomPeerId == peerId && pl.LocalIndex == ownPlayer.LocalIndex);

                if (previousSelf == null || !previousSelf.Ready)
                {
                    continue;
                }

                var own = rollCalls.FirstOrDefault(rc => DynamicData.For(rc).Get<int>("playerIndex") == ownPlayer.Seat);

                if (own == null)
                {
                    continue;
                }

                var dynOwn = DynamicData.For(own);
                var state = dynOwn.Get<Monocle.StateMachine>("state");

                if (state.State != 1)
                {
                    continue;
                }

                _archerService.RemoveArcher(ownPlayer.Seat);
                dynOwn.Get<ArcherPortrait>("portrait").Leave();
                TFGame.Players[ownPlayer.Seat] = false;
                state.State = 0;
            }
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

            if (rollCalls.Length == 0)
            {
                return;
            }

            var lobby = pendingRollcallLobby;
            pendingRollcallLobby = null;

            _inputService.EnsureLocalControllerSeats();
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
                var player = ownLobby.SessionPlayers.FirstOrDefault(entry => entry.Seat == seat);

                settings.Teams[seat] = player == null ? Allegiance.Neutral : (Allegiance)player.Team;
            }
        }

        private static RollcallElement[] GetRollcallElements(Monocle.Scene scene)
        {
            return [.. scene.Layers
                .SelectMany(layer => layer.Value.Entities)
                .OfType<RollcallElement>()];
        }

        private void ReconcileRollcall(Lobby lobby, RollcallElement[] rollCalls)
        {
            foreach (var player in lobby.Players.Where(player => player.HaveModifiedGameFiles && player.RoomPeerId != peerId && modifiedFilesNotified.Add(player.RoomPeerId)))
            {
                Sounds.ui_invalid.Play();
                Notification.Create(TFGame.Instance.Scene, $"{player.Name} HAS MODIFIED GAME FILES - NETPLAY MAY DESYNC".ToUpperInvariant(), 10, 500);
            }

            foreach (var player in lobby.Players.Where(player => !string.IsNullOrEmpty(player.Platform) && player.Platform != Utils.Platform.Current && player.RoomPeerId != peerId && platformNotified.Add(player.RoomPeerId)))
            {
                Sounds.ui_clickSpecial.Play();
                Notification.Create(TFGame.Instance.Scene, $"{player.Name} IS PLAYING ON {player.Platform} - NETPLAY MAY DESYNC".ToUpperInvariant(), 10, 500);
            }

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

                if (player.Ready)
                {
                    var resolvedArchers = ArcherDataExtensions.ResolveArchers(lobby);

                    (var archerIndex, var altIndex) = resolvedArchers.TryGetValue(player.Seat, out var resolved)
                        ? resolved
                        : (0, (int)ArcherData.ArcherTypes.Normal);

                    var updatedPlayer = player.WithResolvedArcher(archerIndex, altIndex);

                    TFGame.Characters[playerIndex] = archerIndex;
                    TFGame.AltSelect[playerIndex] = (ArcherData.ArcherTypes)altIndex;
                    _archerService.AddArcher(playerIndex, updatedPlayer);

                    if (state.State == 0)
                    {
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

            NotifyMembershipChanges(lobby);
            previousPlayersCount = lobby.Players.Count;
        }

        private void NotifyMembershipChanges(Lobby lobby)
        {
            var members = new Dictionary<string, string>();

            foreach (var player in lobby.Players.Where(player => player.RoomPeerId != peerId && !player.IsGuest))
            {
                var guests = lobby.Players.Count(guest => guest.RoomPeerId == player.RoomPeerId && guest.IsGuest);

                members[player.RoomPeerId] = guests > 0 ? $"{player.Name} +{guests}" : player.Name;
            }

            if (rollcallMembers != null)
            {
                foreach (var joined in members.Where(member => !rollcallMembers.ContainsKey(member.Key)))
                {
                    Notification.Create(TFGame.Instance.Scene, $"{joined.Value} joined", 10, 200);
                }

                foreach (var left in rollcallMembers.Where(member => !members.ContainsKey(member.Key)))
                {
                    Notification.Create(TFGame.Instance.Scene, $"{left.Value} left", 10, 200);
                }
            }

            rollcallMembers = members;
        }

        public void ResetPeer()
        {
            peerId = "";
            _skinKnownPeers = new HashSet<string>();
            _skinStreamService.Reset();
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

        private bool FailSearch()
        {
            var state = (TFGame.Instance.Scene as MainMenu)?.State.ToDomainModel();
            var onFail = state == Models.MenuState.QuickPlaySearch
                ? onQuickPlayFailed
                : state == Models.MenuState.RankedSearch ? onRankedFailed : null;

            onFail?.Invoke(CONNECTION_LOST_MESSAGE);

            return onFail != null;
        }

        public int? GetPingTo(Models.WebSocket.Player player)
        {
            RefreshPingStats(player);

            return measuredPingStats.TryGetValue(player.RoomPeerId, out var stats) && stats.rtt >= 0 ? stats.rtt : null;
        }

        public ConnectionQuality? GetConnectionQualityTo(Models.WebSocket.Player player)
        {
            RefreshPingStats(player);

            return measuredPingStats.TryGetValue(player.RoomPeerId, out var stats) ? ConnectionQualities.GetQuality(stats) : null;
        }

        public bool? IsRelayedWith(Models.WebSocket.Player player)
        {
            if (IsSpectator() || string.IsNullOrEmpty(peerId))
            {
                return null;
            }

            return GetRelayedPeersFrom(ownLobby).Contains(player.RoomPeerId);
        }

        public List<string> GetRelayedPeers()
        {
            return [.. GetRelayedPeersFrom(ownLobby)];
        }

        private HashSet<string> GetRelayedPeersFrom(Lobby lobby)
        {
            return [.. lobby.RelayedPairs
                .Where(pair => pair.Contains(peerId))
                .SelectMany(pair => pair)
                .Where(peer => peer != peerId)];
        }

        private void RefreshPingStats(Models.WebSocket.Player player)
        {
            var isRelayed = IsRelayedWith(player) == true;

            if (measuredPingStatsRelayed.TryGetValue(player.RoomPeerId, out var wasRelayed) && wasRelayed != isRelayed)
            {
                measuredPingStats.Remove(player.RoomPeerId);
            }

            measuredPingStatsRelayed[player.RoomPeerId] = isRelayed;

            if ((isRelayed ? relayPingMeasurementUrl : pingMeasurementUrl) == null)
            {
                return;
            }

            GGRSFFI.ping_measurement_stats(player.RoomPeerId, isRelayed, out var stats);

            if (stats.samples > 0)
            {
                measuredPingStats[player.RoomPeerId] = stats;
            }
        }

        public void StartOrStopPingMeasurementIfNeeded(bool inLobbyMenu)
        {
            var isMeasuring = inLobbyMenu && IsConnectedToServer() && !string.IsNullOrEmpty(ownLobby.RoomId) && !string.IsNullOrEmpty(peerId);

            if (isMeasuring && ownLobby.RoomId != measuredPingStatsRoomId)
            {
                measuredPingStats.Clear();
                measuredPingStatsRelayed.Clear();
                reportedMeasuredPeers.Clear();
                measuredPingStatsRoomId = ownLobby.RoomId;
            }

            var url = isMeasuring ? $"{SERVER_URL}/ping_measurement/{ownLobby.RoomId}?peer={peerId}" : null;
            var relayUrl = isMeasuring && GetRelayedPeers().Count > 0 ? $"{SERVER_URL}/relay/ping_measurement/{ownLobby.RoomId}?peer={peerId}" : null;

            pingMeasurementUrl = SwitchPingMeasurement(pingMeasurementUrl, url, false);
            relayPingMeasurementUrl = SwitchPingMeasurement(relayPingMeasurementUrl, relayUrl, true);

            ReportMeasuredPeers();
        }

        private string SwitchPingMeasurement(string currentUrl, string url, bool isRelay)
        {
            if (url == currentUrl)
            {
                return currentUrl;
            }

            if (currentUrl != null)
            {
                GGRSFFI.ping_measurement_stop(isRelay);
            }

            if (url == null)
            {
                return null;
            }

            using var status = GGRSFFI.ping_measurement_start(url, isRelay).ToModelGGrsFFI();

            if (!status.IsOk)
            {
                _logger.LogError<MatchmakingService>($"Failed to start ping measurement : {status.Info.AsString()}");
            }

            return url;
        }

        private void ReportMeasuredPeers()
        {
            if (pingMeasurementUrl == null || IsSpectator())
            {
                return;
            }

            foreach (var player in ownLobby.Players)
            {
                if (player.RoomPeerId == peerId || reportedMeasuredPeers.Contains(player.RoomPeerId))
                {
                    continue;
                }

                GGRSFFI.ping_measurement_stats(player.RoomPeerId, false, out var stats);

                if (stats.rtt < 0)
                {
                    continue;
                }

                reportedMeasuredPeers.Add(player.RoomPeerId);

                var roomPeerId = player.RoomPeerId;
                Task.Run(() => SendPeerMeasured(roomPeerId));
            }
        }

        private async Task Update(WSAction action, Action onSuccess, Action onFail, Models.WebSocket.Player player = null)
        {
            var actionName = action.ToString();
            onResult[$"{actionName}-success"] = onSuccess == null ? null : () => RunOnGameThread(onSuccess);
            onResult[$"{actionName}-fail"] = onFail == null ? null : () => RunOnGameThread(onFail);

            if (!EnsureConnection())
            {
                RunOnGameThread(onFail);
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
                    await SendUpdatePlayer(player);
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
            abandonedRoomId = null;

            NormaliseSeats(lobby.Players);
            NormaliseSeats(lobby.MatchPlayers);

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
                JoinCode = lobby.JoinCode,
                Series = lobby.Series,
                RelayedPairs = lobby.RelayedPairs,
                MatchPlayers = lobby.MatchPlayers,
                IsCheckingConnections = lobby.IsCheckingConnections,
                IsReportEnabled = lobby.IsReportEnabled,
                RankedSeats = lobby.RankedSeats,
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

        public async Task JoinPrivate(string code, bool asPlayer, Action<Lobby> onSuccess, Action onFail)
        {
            pendingJoinCode = code;
            pendingJoinAsPlayer = asPlayer;

            await Update(WSAction.JoinPrivate, () => onSuccess(privateJoinLobby), onFail);
        }

        public async Task EnterQuickPlay(Action<int> onQueued, Action<Lobby> onMatched, Action<string> onFail)
        {
            onQuickPlayQueued = onQueued == null ? null : count => RunOnGameThread(() => onQueued(count));
            onQuickPlayMatched = onMatched == null ? null : lobby => RunOnGameThread(() => onMatched(lobby));
            onQuickPlayFailed = onFail == null ? null : message => RunOnGameThread(() => onFail(message));
            searchingStandardCount = 0;
            searchingWideCount = 0;

            if (!EnsureConnection())
            {
                if (onFail != null)
                {
                    RunOnGameThread(() => onFail("Cannot reach the server"));
                }

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

        public async Task EnterRanked(Action<Lobby> onMatched, Action onReady, Action<string> onRequeued, Action<string> onFail)
        {
            onRankedMatched = onMatched == null ? null : lobby => RunOnGameThread(() => onMatched(lobby));
            onRankedReady = onReady == null ? null : () => RunOnGameThread(onReady);
            onRankedRequeued = onRequeued == null ? null : message => RunOnGameThread(() => onRequeued(message));
            onRankedFailed = onFail == null ? null : message => RunOnGameThread(() => onFail(message));
            searchingStandardCount = 0;
            searchingWideCount = 0;

            if (!EnsureConnection())
            {
                onRankedFailed?.Invoke("Cannot reach the server");
                return;
            }

            await SendEnterRanked();
        }

        public async Task ExitRanked()
        {
            onRankedMatched = null;
            onRankedReady = null;
            onRankedRequeued = null;
            onRankedFailed = null;
            searchingStandardCount = 0;
            searchingWideCount = 0;

            if (IsConnectedToServer())
            {
                await SendExitRanked();
            }
        }

        public async Task RequestLeaderboard(Action<LeaderboardData> onResult, Action onFail)
        {
            if (cachedLeaderboard != null && DateTime.UtcNow - cachedLeaderboardAt < LeaderboardCacheDuration)
            {
                var cached = cachedLeaderboard;
                RunOnGameThread(() => onResult?.Invoke(cached));
                return;
            }

            onLeaderboard = onResult == null ? null : data => RunOnGameThread(() => onResult(data));

            if (!EnsureConnection())
            {
                onLeaderboard = null;

                if (onFail != null)
                {
                    RunOnGameThread(onFail);
                }

                return;
            }

            await SendGetLeaderboard();
        }

        public async Task RequestPlayerProfile(int? rank, Action<PlayerProfile> onResult, Action onFail)
        {
            pendingProfileRank = rank;
            onPlayerProfile = onResult == null ? null : profile => RunOnGameThread(() => onResult(profile));

            if (!EnsureConnection())
            {
                onPlayerProfile = null;

                if (onFail != null)
                {
                    RunOnGameThread(onFail);
                }

                return;
            }

            await SendGetPlayerProfile(rank);
        }

        public bool IsSearchingQuickPlay()
        {
            return isQuickPlayQueued;
        }

        public int GetSearchingCount()
        {
            return isSearchWide ? searchingWideCount : searchingStandardCount;
        }

        public int GetStandardSearchingCount()
        {
            return searchingStandardCount;
        }

        public int GetWideSearchingCount()
        {
            return searchingWideCount;
        }

        public bool IsSearchWide()
        {
            return isSearchWide;
        }

        public bool IsQuickPlayStarting()
        {
            return ownLobby.IsQuickPlay && IsAwaitingHostStart();
        }

        public int GetLocalSeat()
        {
            return GetOwnPlayer()?.Seat ?? 0;
        }

        public Models.WebSocket.Player GetOwnPlayer()
        {
            return ownLobby.Players.FirstOrDefault(player => player.RoomPeerId == peerId && !player.IsGuest);
        }

        public IReadOnlyList<Models.WebSocket.Player> GetLocalPlayers()
        {
            return [.. ownLobby.Players
                .Where(player => player.RoomPeerId == peerId)
                .OrderBy(player => player.LocalIndex)];
        }

        public string GetRoomPeerId()
        {
            return peerId;
        }

        public string GetRelayUrl()
        {
            return $"{SERVER_URL}/relay/room/{ownLobby.RoomId}?peer={peerId}";
        }

        public async Task UpdatePlayer(Models.WebSocket.Player player, Action onSucess, Action onFail)
        {
            await Update(WSAction.UpdatePlayer, onSucess, onFail, player);
        }

        public void PublishInputDelay()
        {
            var own = GetOwnPlayer();

            if (own == null || !EnsureConnection())
            {
                return;
            }

            _ = SendUpdatePlayer(own);
        }

        public async Task LeaveLobby(Action onSucess, Action onFail)
        {
            await Update(WSAction.LeaveLobby, onSucess, onFail);
        }

        public bool IsLobbyReady()
        {
            if (IsSpectator() && ownLobby.InGame)
            {
                return hasHostStartedMatch;
            }

            return AreAllPlayersReady() && hasHostStartedMatch;
        }

        public bool IsLobbyFull()
        {
            return ownLobby.Players.Count >= ownLobby.MaxPlayers;
        }

        public bool CanHostStart()
        {
            return !ownLobby.IsQuickPlay && !ownLobby.IsRanked && IsAwaitingHostStart() && IsHost();
        }

        public bool IsWaitingForHostStart()
        {
            return !ownLobby.IsQuickPlay && !ownLobby.IsRanked && IsAwaitingHostStart() && !IsHost();
        }

        public bool IsCheckingConnections()
        {
            return AreAllPlayersReady() && !hasHostStartedMatch && ownLobby.IsCheckingConnections;
        }

        private bool IsAwaitingHostStart()
        {
            return AreAllPlayersReady() && !hasHostStartedMatch && !ownLobby.IsCheckingConnections;
        }

        private bool IsHost()
        {
            return ownLobby.Players.Any(pl => pl.IsHost && pl.RoomPeerId == peerId);
        }

        public void RequestStart()
        {
            var now = DateTime.UtcNow;
            if (now - lastStartRequestAt < StartRequestCooldown)
            {
                return;
            }

            lastStartRequestAt = now;
            Task.Run(SendStartLobbyChoice);
        }

        private bool AreAllPlayersReady()
        {
            return ownLobby.Players.Select(pl => pl.RoomPeerId).Distinct().Count() >= 2
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

        public void ShowMatchStartWarnings(Scene scene)
        {
            if (ownLobby.IsEmpty)
            {
                return;
            }

            var reasons = ownLobby.Players
                .Where(player => player.RoomPeerId != peerId)
                .SelectMany(DesyncRisks)
                .ToList();

            if (reasons.Count == 0)
            {
                return;
            }

            Sounds.ui_clickSpecial.Play();

            foreach (var reason in reasons)
            {
                Notification.Create(scene, $"{reason} - NETPLAY MAY DESYNC".ToUpperInvariant(), 10, 500);
            }
        }

        private static IEnumerable<string> DesyncRisks(Models.WebSocket.Player player)
        {
            if (player.HaveModifiedGameFiles)
            {
                yield return $"{player.Name} HAS MODIFIED GAME FILES";
            }

            if (!string.IsNullOrEmpty(player.Platform) && player.Platform != Utils.Platform.Current)
            {
                yield return $"{player.Name} IS PLAYING ON {player.Platform}";
            }
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
            hasHostStartedMatch = false;
            isMatchEndReported = false;
            ownLobby = new Lobby();
            previousPlayersCount = 1;
            pendingRollcallLobby = null;
            modifiedFilesNotified.Clear();
            platformNotified.Clear();
            rollcallMembers = null;
        }

        public bool IsSpectator()
        {
            return ownLobby.Spectators.Any(spectator => spectator.RoomPeerId == peerId);
        }

        public async Task RematchChoice()
        {
            await SendRematchChoice();
        }

        public bool CanEditLobbySettings()
        {
            return !ownLobby.IsEmpty
                && !ownLobby.IsQuickPlay
                && !ownLobby.IsRanked
                && !ownLobby.InGame
                && !ownLobby.IsSeriesInProgress
                && IsHost();
        }

        public async Task UpdateLobbySettings(int maxPlayers, Models.WebSocket.GameData gameData, ICollection<CustomMod> mods)
        {
            var updateMessage = new UpdateLobbySettingsMessage
            {
                UpdateLobbySettings = new Models.WebSocket.Client.UpdateLobbySettings
                {
                    MaxPlayers = maxPlayers,
                    GameData = gameData,
                    Mods = mods,
                }
            };

            var bytes = MessagePackSerializer.Serialize(updateMessage);
            var message = MessagePackSerializer.ConvertToJson(bytes);
            await Send(message);
        }

        public void RequestRollcallReconcile()
        {
            if (!ownLobby.IsEmpty)
            {
                pendingRollcallLobby = ownLobby;
            }
        }

        public void NotifyMatchEnded(int winnerSeat, IList<int> scores, IList<SeatStats> stats, int? tower)
        {
            if (ownLobby.IsEmpty || isMatchEndReported)
            {
                return;
            }

            isMatchEndReported = true;

            Task.Run(async () => await SendMatchEnded(winnerSeat, scores, stats, tower));
        }

        public async Task SeriesContinueChoice()
        {
            await SendSeriesContinueChoice();
        }

        public async Task PickSeriesMap(int mapId)
        {
            await SendSeriesPickMap(mapId);
        }

        public IEnumerable<EndGameStatus> GetEndGameStatus()
        {
            var votes = ownLobby.EndGameChoice ?? [];

            return [.. ownLobby.Players
                .OrderBy(player => player.Seat)
                .Select(player => new EndGameStatus
                {
                    Seat = player.Seat,
                    Name = player.Name,
                    Choice = votes.FirstOrDefault(vote => vote.RoomPeerId == player.RoomPeerId)?.Choice
                })];
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
