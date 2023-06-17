namespace TF.EX.Domain.Ports
{
    public interface IMatchmakingService
    {
        int GetPingToOpponent();
        Guid GetOpponentPeerId();
        bool HasOpponentChoosed();
        bool IsConnectedToServer();
        bool ConnectToServerAndListen();

        void DisconnectFromServer();
        void RegisterForDirect();

        string GetDirectCode();
        Task<bool> SendOpponentCode(string text);
        void RegisterForQuickPlay();
        bool HasRegisteredForQuickPlay();
        bool HasFoundOpponentForQuickPlay();
        void CancelQuickPlay();
        void AcceptOpponentInQuickPlay();
        bool HasAcceptedOpponentForQuickPlay();
        bool HasOpponentDeclined();

        int GetTotalAvailablePlayersInQuickPlayQueue();
    }
}
