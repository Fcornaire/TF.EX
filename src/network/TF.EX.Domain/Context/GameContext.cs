using System.Collections.Concurrent;
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
        void UpdatePolledInput(int seat, Input input);
        Input GetPolledInput(int seat);
        void ResetPolledInputs();
        Input GetCurrentInput(int characterIndex);
        List<Input> GetCurrentInputs();

        int GetLocalPlayerIndex();
        void SetLocalSeat(int seat);
        TowerFall.PlayerInput GetLocalInput();
        void SetLocalInput(TowerFall.PlayerInput input);
        void SetInputLocked(bool locked);
        bool IsInputLocked();
        void ResetPlayersIndex();
        bool IsLocalSeat(int seat);
        IReadOnlyList<int> GetSessionSeats();
        void SetSessionSeats(int[] seats);
        IReadOnlyList<TowerFall.PlayerInput> GetGuestInputs();
        void SetGuestInputs(IReadOnlyList<TowerFall.PlayerInput> inputs);

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
        private readonly Input[] PolledInputs = new Input[Constants.MAX_SEATS];

        private readonly ConcurrentDictionary<int, Player> ArcherSelections = new();

        private int _localPlayerIndex = -1;
        private TowerFall.PlayerInput _localInput;
        private int[] _sessionSeats = [];
        private IReadOnlyList<TowerFall.PlayerInput> _guestInputs = [];
        private DateTime _inputLockedUntil = DateTime.MinValue;
        private int[] SessionSeats => Interlocked.CompareExchange(ref _sessionSeats, null, null);


        public GameContext()
        {
            CurrentInputs = new AttributeManager<Input>(EmptyInput, MAX_PLAYERS);
        }

        private Input EmptyInput() { return new Input(); }

        public void UpdateCurrentInputs(IEnumerable<Input> inputs)
        {
            CurrentInputs.Update(inputs);
        }

        public void UpdatePolledInput(int seat, Input input)
        {
            if (seat >= 0 && seat < PolledInputs.Length)
            {
                PolledInputs[seat] = input;
            }
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

        public Input GetPolledInput(int seat)
        {
            return seat >= 0 && seat < PolledInputs.Length ? PolledInputs[seat] : new Input();
        }

        public void ResetPolledInputs()
        {
            Array.Clear(PolledInputs, 0, PolledInputs.Length);
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

        public bool IsLocalSeat(int seat)
        {
            if (seat < 0)
            {
                return false;
            }

            var sessionSeats = SessionSeats;

            if (sessionSeats.Length > 1 && TowerFall.TFGame.Instance?.Scene is not TowerFall.MainMenu)
            {
                return Array.IndexOf(sessionSeats, seat) >= 0;
            }

            if (seat == GetLocalPlayerIndex())
            {
                return true;
            }

            if (TowerFall.TFGame.Instance?.Scene is not TowerFall.MainMenu)
            {
                return false;
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            return !matchmakingService.IsSpectator() && matchmakingService.GetLocalPlayers().Any(player => player.Seat == seat);
        }

        public IReadOnlyList<int> GetSessionSeats()
        {
            return SessionSeats;
        }

        public void SetSessionSeats(int[] seats)
        {
            Interlocked.Exchange(ref _sessionSeats, seats);
        }

        public IReadOnlyList<TowerFall.PlayerInput> GetGuestInputs()
        {
            return Interlocked.CompareExchange(ref _guestInputs, null, null);
        }

        public void SetGuestInputs(IReadOnlyList<TowerFall.PlayerInput> inputs)
        {
            Interlocked.Exchange(ref _guestInputs, inputs);
        }

        public IEnumerable<(int, string)> GetArchers()
        {
            return ArcherSelections
                .ToArray()
                .Select(kvp => (kvp.Key, $"{kvp.Value.ArcherIndex}-{kvp.Value.ArcherAltIndex}"))
                .ToList();
        }

        public void AddArcher(int index, Player player)
        {
            ArcherSelections[index] = player;
        }

        public void ResetArcherSelections()
        {
            ArcherSelections.Clear();
        }

        public void RemoveArcher(int playerIndex)
        {
            ArcherSelections.TryRemove(playerIndex, out _);
        }

        public IEnumerable<(int, Player)> GetPlayers()
        {
            return ArcherSelections.ToArray().Select(kvp => (kvp.Key, kvp.Value)).ToList();
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _sessionSeats, []);
            ResetPlayersIndex();
            ResetArcherSelections();

            StateApi.Current.ResetMatch();
        }
    }
}
