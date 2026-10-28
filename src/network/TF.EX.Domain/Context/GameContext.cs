using TF.EX.Domain.Externals;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Utils;

namespace TF.EX.Domain.Context
{
    public interface IGameContext
    {
        void UpdateCurrentInputs(IEnumerable<Input> inputs);
        void UpdatePolledInput(int index, Input input);
        Input GetPolledInput(int index);
        void ResetPolledInputs();
        Input GetCurrentInput(int characterIndex);
        List<Input> GetCurrentInputs();

        int GetLocalPlayerIndex();
        IReadOnlyList<int> GetLocalPlayerIndexes();
        void SetLocalSeat(int seat);
        TowerFall.PlayerInput GetLocalInput();
        void SetLocalInput(TowerFall.PlayerInput input);
        void SetInputLocked(bool locked);
        bool IsInputLocked();
        void ResetPlayersIndex();

        IEnumerable<(int, string)> GetArchers();
        IEnumerable<(int, Player)> GetPlayers();
        void AddArcher(int index, Player player);
        void ResetArcherSelections();
        void RemoveArcher(int playerIndex);

        void Reset();
    }

    internal class GameContext : IGameContext
    {
        private const int MAX_PLAYERS = 4;
        private const int INPUT_LOCK_TIMEOUT_SECONDS = 5;

        private readonly AttributeManager<Input> CurrentInputs;
        //Per local player index (several players can share this device)
        private readonly Dictionary<int, Input> PolledInputs = new Dictionary<int, Input>();
        //Three threads touch this: game thread,websocket receive thread and the render thread (rollcall)
        private volatile Dictionary<int, Player> ArcherSelections = new Dictionary<int, Player>();

        private int _localPlayerIndex = -1;
        private TowerFall.PlayerInput _localInput;
        private DateTime _inputLockedUntil = DateTime.MinValue;

        public GameContext()
        {
            CurrentInputs = new AttributeManager<Input>(EmptyInput, MAX_PLAYERS);
        }

        private Input EmptyInput() { return new Input(); }

        public void UpdateCurrentInputs(IEnumerable<Input> inputs)
        {
            CurrentInputs.Update(inputs);
        }

        public void UpdatePolledInput(int index, Input input)
        {
            PolledInputs[index] = input;
        }

        public void ResetPolledInputs()
        {
            PolledInputs.Clear();
        }

        public Input GetCurrentInput(int characterIndex)
        {
            if (characterIndex < 0)
            {
                return EmptyInput();
            }

            if (CurrentInputs.Get().Count == 0)
            {
                return EmptyInput();
            }

            if (characterIndex >= CurrentInputs.Get().Count)
            {
                return EmptyInput();
            }

            return CurrentInputs[characterIndex];
        }

        public Input GetPolledInput(int index)
        {
            return PolledInputs.TryGetValue(index, out var input) ? input : EmptyInput();
        }

        public List<Input> GetCurrentInputs()
        {
            return CurrentInputs.Get();
        }

        public int GetLocalPlayerIndex()
        {
            if (_localPlayerIndex != -1 && ServiceCollections.ResolveNetplayManager().IsReplayMode())
            {
                return _localPlayerIndex;
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (TowerFall.TFGame.Instance?.Scene is TowerFall.MainMenu
                && !matchmakingService.GetOwnLobby().IsEmpty
                && !matchmakingService.IsSpectator())
            {
                return matchmakingService.GetLocalSeat();
            }

            var handle = GGRSFFI.netplay_local_player_handle();

            return handle >= 0 ? handle : matchmakingService.GetLocalSeat();
        }

        /// <summary>
        /// Every player index played on this device: the local player first, then the other players sharing this device's RoomPeerId
        /// </summary>
        public IReadOnlyList<int> GetLocalPlayerIndexes()
        {
            var localIndex = GetLocalPlayerIndex();

            if (ServiceCollections.ResolveNetplayManager().IsReplayMode())
            {
                return [localIndex];
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (TowerFall.TFGame.Instance?.Scene is TowerFall.MainMenu
                && !matchmakingService.GetOwnLobby().IsEmpty
                && !matchmakingService.IsSpectator())
            {
                return [localIndex, .. matchmakingService.GetLocalSeats().Where(seat => seat != localIndex)];
            }

            var peerId = matchmakingService.GetRoomPeerId();

            if (string.IsNullOrEmpty(peerId)
                || matchmakingService.GetOwnLobby().SessionPlayers.Count(player => player.RoomPeerId == peerId) <= 1
                || !ServiceCollections.ResolveInputService().SupportsLocalPlayers())
            {
                return [localIndex];
            }

            var count = GGRSFFI.netplay_local_player_handle_count();

            return [localIndex, .. Enumerable.Range(0, count).Select(GGRSFFI.netplay_local_player_handle_at).Where(handle => handle >= 0 && handle != localIndex)];
        }

        public TowerFall.PlayerInput GetLocalInput()
        {
            return _localInput;
        }

        public void SetLocalInput(TowerFall.PlayerInput input)
        {
            _localInput = input;
        }

        public void SetInputLocked(bool locked)
        {
            _inputLockedUntil = locked ? DateTime.UtcNow.AddSeconds(INPUT_LOCK_TIMEOUT_SECONDS) : DateTime.MinValue;
        }

        public bool IsInputLocked()
        {
            return DateTime.UtcNow < _inputLockedUntil;
        }

        public void SetLocalSeat(int seat)
        {
            _localPlayerIndex = seat;
        }

        public void ResetPlayersIndex()
        {
            _localPlayerIndex = -1;
        }

        public IEnumerable<(int, string)> GetArchers()
        {
            return ArcherSelections
                .Select(kvp => (kvp.Key, $"{kvp.Value.ArcherIndex}-{kvp.Value.ArcherAltIndex}"))
                .ToList();
        }

        public void AddArcher(int index, Player player)
        {
            ArcherSelections = new Dictionary<int, Player>(ArcherSelections) { [index] = player };
        }

        public void ResetArcherSelections()
        {
            ArcherSelections = new Dictionary<int, Player>();
        }

        public void RemoveArcher(int playerIndex)
        {
            var current = ArcherSelections;

            if (!current.ContainsKey(playerIndex))
            {
                return;
            }

            var next = new Dictionary<int, Player>(current);

            next.Remove(playerIndex);

            ArcherSelections = next;
        }

        public IEnumerable<(int, Player)> GetPlayers()
        {
            return ArcherSelections.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        }

        public void Reset()
        {
            ResetPlayersIndex();
            ResetArcherSelections();

            StateApi.Current.ResetMatch();
        }
    }
}
