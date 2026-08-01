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

        Task UpdatePlayer(Player player, Action onSucess, Action onFail);
        Task LeaveLobby(Action onSuccess, Action onFail);
        void ResetPeer();
        bool IsLobbyReady();
        bool IsLobbyFull();
        bool CanHostStart();
        bool IsWaitingForHostStart();
        void RequestStart();
        void ResetLobby();
        bool IsSpectator();
        string GetRoomPeerId();
        int GetLocalSeat();
        void ReconcileRollcallIfPending();
        void ApplyTeamsToMatchSettings();
        Task RematchChoice();
        Task ArcherSelectChoice();
    }
}
