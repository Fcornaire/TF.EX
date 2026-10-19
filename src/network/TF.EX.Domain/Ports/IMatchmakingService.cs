using TF.EX.Domain.Models;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Models.WebSocket.Server;

namespace TF.EX.Domain.Ports
{
    public interface IMatchmakingService
    {
        int? GetPingTo(Player player);
        ConnectionQuality? GetConnectionQualityTo(Player player);
        bool? IsRelayedWith(Player player);
        List<string> GetRelayedPeers();
        void StartOrStopPingMeasurementIfNeeded(bool inLobbyMenu);
        bool IsConnectedToServer();
        bool ConnectToServerAndListen();

        void DisconnectFromServer();

        Task GetLobbies(Action onSuccess, Action onFail);

        void ResetLobbies();

        IEnumerable<Lobby> GetLobbies();

        Lobby GetOwnLobby();

        void UpdateOwnLobby(Lobby lobby);
        Task CreateLobby(Action onSuccess, Action onFail);
        Task JoinLobby(string roomId, bool isPlayer, Action onSucess, Action onFail);
        Task JoinPrivate(string code, bool asPlayer, Action<Lobby> onSuccess, Action onFail);
        Task EnterQuickPlay(Action<int> onQueued, Action<Lobby> onMatched, Action<string> onFail);
        Task ExitQuickPlay();
        Task RequestLeaderboard(Action<LeaderboardData> onResult, Action onFail);
        Task EnterRanked(Action<Lobby> onMatched, Action onReady, Action<string> onRequeued, Action<string> onFail);
        Task ExitRanked();
        bool IsSearchingQuickPlay();
        int GetSearchingCount();
        int GetStandardSearchingCount();
        int GetWideSearchingCount();
        bool IsSearchWide();
        bool IsQuickPlayStarting();

        Task UpdatePlayer(Player player, Action onSucess, Action onFail);
        void PublishInputDelay();
        Task UpdateLobbySettings(int maxPlayers, GameData gameData, ICollection<CustomMod> mods);
        bool CanEditLobbySettings();
        void RequestRollcallReconcile();
        void PublishCustomSkin(int archerIndex, int archerAltIndex);
        Task LeaveLobby(Action onSuccess, Action onFail);
        void ResetPeer();
        bool IsLobbyReady();
        bool IsLobbyFull();
        bool CanHostStart();
        bool IsWaitingForHostStart();
        bool IsCheckingConnections();
        void RequestStart();
        void ResetLobby();
        void QueueSpectatorNotice(string text);
        void ShowMatchStartWarnings(Monocle.Scene scene);
        void ShowPendingSpectatorNoticeIfAny();
        bool IsSpectator();
        string GetRoomPeerId();
        string GetRelayUrl();
        int GetLocalSeat();
        void ReconcileRollcallIfPending();
        void RunOnGameThread(Action action);
        void DrainGameThreadActions();
        void ApplyTeamsToMatchSettings();
        void RestoreArchersFromLobbyIfNeeded();
        void NotifyMatchEnded(int winnerSeat, IList<int> scores);
        IEnumerable<Models.WebSocket.EndGameStatus> GetEndGameStatus();
        Task RematchChoice();
        Task ArcherSelectChoice();
        Task SeriesContinueChoice();
        Task PickSeriesMap(int mapId);
        bool IsSeriesPicker();
    }
}
