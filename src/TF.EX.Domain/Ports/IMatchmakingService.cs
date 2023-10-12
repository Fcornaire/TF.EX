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

        Task RetrieveLobbies(Action onSuccess);

        IEnumerable<Lobby> GetLobbies();

        Lobby GetOwnLobby();

        void UpdateOwnLobby(Lobby lobby);
        Task CreateLobby(Action onSuccess, Action onFail);
        Task JoinLobby(string roomId, Action onSucess, Action onFail);

        Task UpdatePlayer(Player player);
        string GetRoomChatPeerId();
        Task LeaveLobby(Action onSuccess, Action onFail);
        void ResetPeer();
        bool IsLobbyReady();
    }
}
