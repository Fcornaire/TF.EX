using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Ports
{
    public interface IMatchmakingService
    {
        int GetPingTo(Player player);
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
        bool IsSearchingQuickPlay();
        int GetSearchingCount();
        bool IsQuickPlayStarting();

        Task UpdatePlayer(Player player, Action onSucess, Action onFail);
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
        void RequestStart();
        void ResetLobby();
        void QueueSpectatorNotice(string text);
        void ShowPendingSpectatorNoticeIfAny();
        bool IsSpectator();
        string GetRoomPeerId();
        int GetLocalSeat();
        void ReconcileRollcallIfPending();
        void RunOnGameThread(Action action);
        void DrainGameThreadActions();
        void ApplyTeamsToMatchSettings();
        void RestoreArchersFromLobbyIfNeeded();
        void NotifyMatchEnded(int winnerSeat);
        IEnumerable<Models.WebSocket.EndGameStatus> GetEndGameStatus();
        Task RematchChoice();
        Task ArcherSelectChoice();
    }
}
