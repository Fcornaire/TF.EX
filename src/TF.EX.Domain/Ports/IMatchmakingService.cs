using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Ports
{
    public interface IMatchmakingService
    {
        int GetPingToOpponent();
        Guid GetOpponentPeerId();
        bool IsConnectedToServer();
        bool ConnectToServerAndListen();
        void ConnectAndListenToLobby(string roomUrl);

        void DisconnectFromServer();
        void DisconnectFromLobby();

        Task GetLobbies(Action onSuccess, Action onFail);

        void ResetLobbies();

        IEnumerable<Lobby> GetLobbies();

        Lobby GetOwnLobby();

        void UpdateOwnLobby(Lobby lobby);
        Task CreateLobby(Action onSuccess, Action onFail);
        Task JoinLobby(string roomId, Action onSucess, Action onFail);

        Task UpdatePlayer(Player player, Action onSucess, Action onFail);
        string GetRoomChatPeerId();
        Task LeaveLobby(Action onSuccess, Action onFail);
        void ResetPeer();
        bool IsLobbyReady();
        void ResetLobby();
    }
}
