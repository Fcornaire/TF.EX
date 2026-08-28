using TF.EX.Domain.Models;
using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Ports
{
    public interface INetplayManager
    {
        void Poll();

        void Init(TowerFall.RoundLogic roundLogic);

        bool ConsumeAbortToVersusOptions();

        bool IsInit();

        void PublishCaptureFlag();

        StatusImpl AdvanceFrame(Input input);

        void UpdateNetplayRequests();

        void SaveGameState(byte[] state);

        void AdvanceGameState();

        byte[] LoadGameState();

        NetworkStats GetNetworkStats();
        IReadOnlyDictionary<int, NetworkStats> GetNetworkStatsPerSeat();

        bool IsTestMode();

        bool IsReplayMode();

        bool IsRollbackFrame();

        void SetIsRollbackFrame(bool isRollbackFrame);

        bool IsUpdating();

        void SetIsUpdating(bool isUpdating);

        bool HaveFramesToReSimulate();

        double GetFramesToReSimulate();

        void UpdateFramesToReSimulate(int frames);

        NetplayMode GetNetplayMode();

        bool CanAdvanceFrame();

        bool HaveRequestToHandle();

        NetplayRequest ConsumeNetplayRequest();

        bool IsSynchronized();

        bool IsDisconnected();

        bool IsFramesAhead();

        void Reset();
        void SetSessionInputDelay(int inputDelay);
        void ClearSessionInputDelay();

        string GetPlayer2Name();

        void UpdatePlayer2Name(string name);
        void SetRoomAndServerMode(string roomUrl, bool isHost);

        int LocalSeat { get; }
        string GetNameForSeat(int seat);
        void SetLocalSeat(int seat);
        bool HasSetMode();
        void SetTestMode(int checkDistance, int numPlayers, int fps = Models.Constants.NETPLAY_FPS);
        int GetSessionFps();
        void SetLocalMode(string addr, ushort localPort, PlayerDraw draw);

        void SetServerMode(string roomUrl);
        void ResetMode();
        void SetReplayMode();

        bool HasFailedInitialConnection();
        ICollection<Models.ArcherSeatInfo> GetArchersInfo();
        bool IsServerMode();
        void SetSpectatorMode(string roomUrl, string hostPeerId);
        bool IsSpectatorMode();
        void AddLateSpectator(string peerId);
        void SetSpectatorCatchup(bool enabled);
        bool IsSpectatorCatchupEnabled();
        void AddSpectators(IEnumerable<Player> spectators);
        void UpdatePlayers(ICollection<Player> players, ICollection<Player> spectators);
        void UpdateNumPlayers(int count);
        int GetNumPlayers();
    }
}
