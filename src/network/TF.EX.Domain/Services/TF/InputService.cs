using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Domain.Services.TF
{
    public class InputService : IInputService
    {
        private readonly IGameContext _context;
        private static bool _supportsTestInputs = true;
        private static bool? _supportsLocalPlayers;

        //Other players on this device: the devices not used by the local player, and the one each extra seat plays with
        private static readonly List<PlayerInput> _spareInputs = [];
        private static readonly Dictionary<int, PlayerInput> _localPlayersInputs = [];
        private static readonly List<PlayerInput> _joiningInputs = [];
        private static readonly HashSet<int> _orphanSeats = [];
        private static DateTime _joiningSince;
        private static int _lateJoinResponses;
        private const int LOCAL_PLAYER_JOIN_TIMEOUT_SECONDS = 5;
        private const int DEVICE_SCAN_INTERVAL_FRAMES = 30;
        private static int _deviceScanFrames;

        public InputService(IGameContext context)
        {
            _context = context;
        }

        public int GetInputIndex(PlayerInput input)
        {
            var idx = TFGame.PlayerInputs
                .Select((x, i) => new { Value = x, Index = i })
                .FirstOrDefault(x => x.Value == input)?.Index;

            return idx ?? -1;
        }

        public void ObserveLocalDevice()
        {
            foreach (var input in TFGame.PlayerInputs)
            {
                if (input == null || !input.Attached || !HasMenuActivity(input))
                {
                    continue;
                }

                _context.SetLocalInput(input);
                return;
            }
        }

        public void RebindLocalInput()
        {
            var previous = _context.GetLocalInput();

            if (previous == null)
            {
                return;
            }

            var match = TFGame.PlayerInputs.FirstOrDefault(input => input != null && input.ID == previous.ID)
                ?? TFGame.PlayerInputs.FirstOrDefault(input => input != null && input.Attached);

            if (match != null && !ReferenceEquals(match, previous))
            {
                _context.SetLocalInput(match);
            }
        }

        private static bool HasMenuActivity(PlayerInput input)
        {
            return input.MenuConfirmCheck || input.MenuBackCheck || input.MenuStartCheck
                || input.MenuUpCheck || input.MenuDownCheck
                || input.MenuLeftCheck || input.MenuRightCheck
                || input.MenuAltCheck || input.MenuAlt2Check;
        }

        /// <summary>
        /// Used to add controller for remote players
        /// </summary>
        public bool EnsureLocalControllerSeat()
        {
            var localSeat = GetLocalInputIndex();
            var localInput = _context.GetLocalInput();

            if (localInput == null || localSeat < 0 || localSeat >= TFGame.PlayerInputs.Length)
            {
                return false;
            }

            if (TFGame.PlayerInputs[localSeat] == localInput)
            {
                SetLocalMenuInput(localSeat, localInput);
                return false;
            }

            var displaced = TFGame.PlayerInputs[localSeat];

            for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
            {
                if (TFGame.PlayerInputs[i] == localInput)
                {
                    TFGame.PlayerInputs[i] = displaced;
                }
            }

            TFGame.PlayerInputs[localSeat] = localInput;
            SetLocalMenuInput(localSeat, localInput);

            return true;
        }

        private static void SetLocalMenuInput(int localSeat, PlayerInput localInput)
        {
            if (localSeat >= MenuInput.MenuInputs.Length || MenuInput.MenuInputs[localSeat] == localInput)
            {
                return;
            }

            for (int i = 0; i < MenuInput.MenuInputs.Length; i++)
            {
                if (MenuInput.MenuInputs[i] == localInput)
                {
                    MenuInput.MenuInputs[i] = null;
                }
            }

            MenuInput.MenuInputs[localSeat] = localInput;
        }

        public void EnsureEveryControllerSlot()
        {
            for (int seat = 0; seat < TFGame.PlayerInputs.Length; seat++)
            {
                TFGame.PlayerInputs[seat] ??= new FakeController();
            }
        }

        public void EnsureRemoteController(int playerCount)
        {
            EnsureRemoteControllers(Enumerable.Range(0, playerCount));
        }

        public void EnsureRemoteControllers(IEnumerable<int> seats)
        {
            var localSeat = _context.GetLocalPlayerIndex();

            foreach (var seat in seats)
            {
                if (seat == localSeat || seat < 0 || seat >= TFGame.PlayerInputs.Length)
                {
                    continue;
                }

                TFGame.PlayerInputs[seat] ??= new KeyboardInput();
            }
        }

        public void EnsureFakeControllers()
        {
            for (int seat = 0; seat < TFGame.Players.Length && seat < TFGame.PlayerInputs.Length; seat++)
            {
                if (TFGame.Players[seat])
                {
                    TFGame.PlayerInputs[seat] ??= new FakeController();
                }
            }
        }

        public void RemoveFakeControllers()
        {
            for (int seat = 0; seat < TFGame.PlayerInputs.Length; seat++)
            {
                if (TFGame.PlayerInputs[seat] is FakeController)
                {
                    TFGame.PlayerInputs[seat] = null;
                }
            }

            for (int i = 0; i < MenuInput.MenuInputs.Length; i++)
            {
                if (MenuInput.MenuInputs[i] is FakeController)
                {
                    MenuInput.MenuInputs[i] = null;
                }
            }
        }

        public Input GetCurrentInput(int characterIndex)
        {
            return _context.GetCurrentInput(characterIndex);
        }

        public List<Input> GetCurrentInputs()
        {
            return _context.GetCurrentInputs();
        }

        public int GetLocalPlayerInputIndex()
        {
            return _context.GetLocalPlayerIndex();
        }

        public bool IsLocalPlayerIndex(int index)
        {
            return index >= 0 && _context.GetLocalPlayerIndexes().Contains(index);
        }

        public bool IsLocalInput(PlayerInput input)
        {
            return IsLocalPlayerIndex(GetInputIndex(input)) || _spareInputs.Contains(input);
        }

        public bool IsSpareInput(PlayerInput input)
        {
            return _spareInputs.Contains(input) && !_localPlayersInputs.ContainsValue(input);
        }

        public Input GetPolledInput()
        {
            if (InputScripter.Enabled)
            {
                var seatInputs = InputScripter.GetAllInputs(Externals.GGRSFFI.netplay_current_frame());

                PushTestInputs(seatInputs);

                return seatInputs[0];
            }

            return _context.GetPolledInput(GetLocalPlayerInputIndex());
        }

        /// <summary>
        /// Gives GGRS the inputs of the other players on this device (nothing to do with a single local player)
        /// </summary>
        public void PushLocalPlayersInputs(bool neutral)
        {
            var indexes = _context.GetLocalPlayerIndexes();

            if (indexes.Count <= 1 || InputScripter.Enabled)
            {
                return;
            }

            var others = indexes.Skip(1).ToList();

            var inputs = new Input[others.Max() + 1];

            foreach (var index in others)
            {
                inputs[index] = neutral ? new Input() : _context.GetPolledInput(index);
            }

            Externals.GGRSFFI.netplay_set_local_inputs(inputs, inputs.Length);
        }

        private static bool IsSameDevice(PlayerInput a, PlayerInput b)
        {
            if (a == null || b == null || a.GetType() != b.GetType())
            {
                return false;
            }

            return a is XGamepadInput pad ? pad.XGamepadIndex == ((XGamepadInput)b).XGamepadIndex : a.ID == b.ID;
        }

        /// <summary>
        /// Rollcall: a spare device pressing confirm asks the server for another seat, which it then plays
        /// </summary>
        public void UpdateLocalPlayers()
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var lobby = matchmakingService.GetOwnLobby();
            var mainSeat = matchmakingService.GetLocalSeat();
            var seats = matchmakingService.GetLocalSeats().Where(seat => seat != mainSeat).ToList();

            foreach (var seat in _localPlayersInputs.Keys.Where(seat => !seats.Contains(seat)).ToList())
            {
                if (seat < TFGame.PlayerInputs.Length && TFGame.PlayerInputs[seat] == _localPlayersInputs[seat])
                {
                    TFGame.PlayerInputs[seat] = null;
                }

                _localPlayersInputs.Remove(seat);
            }

            foreach (var seat in seats.Where(seat => !_localPlayersInputs.ContainsKey(seat) && _joiningInputs.Count > 0))
            {
                _localPlayersInputs[seat] = _joiningInputs[0];
                _joiningInputs.RemoveAt(0);
            }

            PlaceLocalPlayersInputs();

            //A seat the server added after the device that asked for it gave up: give it back
            _orphanSeats.IntersectWith(seats);

            foreach (var seat in seats.Where(seat => !lobby.InGame && !_localPlayersInputs.ContainsKey(seat) && _joiningInputs.Count == 0 && _orphanSeats.Add(seat)))
            {
                Task.Run(() => matchmakingService.RemoveLocalPlayer(seat));
            }

            if (DateTime.UtcNow - _joiningSince > TimeSpan.FromSeconds(LOCAL_PLAYER_JOIN_TIMEOUT_SECONDS))
            {
                _lateJoinResponses += _joiningInputs.Count;
                _joiningInputs.Clear();
            }

            if (_joiningInputs.Count > 0
                || lobby.IsEmpty
                || lobby.InGame
                || lobby.IsQuickPlay
                || lobby.IsRanked
                || lobby.IsSeriesInProgress
                || lobby.Players.Count >= lobby.MaxPlayers
                || matchmakingService.IsSpectator()
                || IsInputLocked())
            {
                return;
            }

            ScanNewDevices(_context.GetLocalInput());

            var joining = _spareInputs.FirstOrDefault(input => !_localPlayersInputs.ContainsValue(input) && input.Attached && input.MenuConfirm);

            //A device mirrored by another (Steam Input, DS4Windows) presses along with the local player's: not a new player
            if (joining == null
                || _context.GetLocalInput()?.MenuConfirmCheck == true
                || _localPlayersInputs.Values.Any(input => input.MenuConfirmCheck)
                || !SupportsLocalPlayers())
            {
                return;
            }

            _joiningInputs.Add(joining);
            _joiningSince = DateTime.UtcNow;
            Sounds.ui_click.Play();

            Task.Run(matchmakingService.AddLocalPlayer);
        }

        public void HandleRemoveLocalPlayerRefused()
        {
            //Asked again on the next update if a seat is still without a device
            _orphanSeats.Clear();
        }

        public void HandleAddLocalPlayerResponse(bool success)
        {
            //Answers come in order: the ones for joins that already timed out come first
            if (_lateJoinResponses > 0)
            {
                _lateJoinResponses--;
                return;
            }

            if (success || _joiningInputs.Count == 0)
            {
                return;
            }

            _joiningInputs.RemoveAt(0);
            Sounds.ui_invalid.Play();
        }

        /// <summary>
        /// A device attached after the lobby opened (a gamepad plugged in, Joy-Cons split apart) becomes a spare too. The lobby's
        /// slots aren't touched: the attached devices are listed by the game's own AssignInputs into a scratch array, and the slots
        /// and menu inputs are put back as they were.
        /// </summary>
        private void ScanNewDevices(PlayerInput localInput)
        {
            if (localInput == null || ++_deviceScanFrames < DEVICE_SCAN_INTERVAL_FRAMES)
            {
                return;
            }

            _deviceScanFrames = 0;

            var playerInputs = TFGame.PlayerInputs;
            var savedPlayers = (PlayerInput[])playerInputs.Clone();
            var menuInputs = MenuInput.MenuInputs;
            var savedMenus = (PlayerInput[])menuInputs.Clone();

            try
            {
                TFGame.PlayerInputs = new PlayerInput[playerInputs.Length];
                PlayerInput.AssignInputs();

                foreach (var input in TFGame.PlayerInputs.Where(input => input != null && input is not FakeController))
                {
                    if (!IsSameDevice(input, localInput)
                        && !_spareInputs.Any(spare => IsSameDevice(spare, input))
                        && !_localPlayersInputs.Values.Any(local => IsSameDevice(local, input)))
                    {
                        _spareInputs.Add(input);
                    }
                }
            }
            finally
            {
                Array.Copy(savedPlayers, playerInputs, savedPlayers.Length);
                TFGame.PlayerInputs = playerInputs;
                Array.Copy(savedMenus, menuInputs, savedMenus.Length);
                MenuInput.MenuInputs = menuInputs;
            }
        }

        private void RefreshSpareInputs(PlayerInput localInput)
        {
            _spareInputs.Clear();

            foreach (var input in TFGame.PlayerInputs.Where(input => input != null && input is not FakeController))
            {
                if (!IsSameDevice(input, localInput) && !_spareInputs.Any(spare => IsSameDevice(spare, input)))
                {
                    _spareInputs.Add(input);
                }
            }
        }

        /// <summary>
        /// Where the local player's device goes: its seat (rollcall), or the handle of its seat (match), which isn't
        /// GGRS's first local handle when another player of this device sits before it
        /// </summary>
        private int GetLocalInputIndex()
        {
            var localIndex = _context.GetLocalPlayerIndex();

            if (_localPlayersInputs.Count == 0 || TFGame.Instance?.Scene is MainMenu)
            {
                return localIndex;
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var index = GetSessionSeats().IndexOf(matchmakingService.GetLocalSeat());

            return index >= 0 ? index : localIndex;
        }

        private static List<int> GetSessionSeats()
        {
            return [.. ServiceCollections.ResolveMatchmakingService().GetOwnLobby().SessionPlayers
                .Select(player => player.Seat)
                .OrderBy(seat => seat)];
        }

        /// <summary>
        /// Puts each extra local player's device at its seat (rollcall) or at its handle, the rank of its seat (match)
        /// </summary>
        public void PlaceLocalPlayersInputs()
        {
            if (_localPlayersInputs.Count == 0)
            {
                return;
            }

            if (ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty)
            {
                _localPlayersInputs.Clear();
                return;
            }

            EnsureLocalControllerSeat();

            var seats = GetSessionSeats();

            foreach (var (seat, input) in _localPlayersInputs)
            {
                var index = TFGame.Instance.Scene is MainMenu ? seat : seats.IndexOf(seat);

                if (index < 0 || index >= TFGame.PlayerInputs.Length || TFGame.PlayerInputs[index] == input)
                {
                    continue;
                }

                for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
                {
                    if (TFGame.PlayerInputs[i] == input)
                    {
                        TFGame.PlayerInputs[i] = null;
                    }
                }

                TFGame.PlayerInputs[index] = input;
            }
        }

        public bool SupportsLocalPlayers()
        {
            if (_supportsLocalPlayers == null)
            {
                try
                {
                    Externals.GGRSFFI.netplay_set_local_inputs([], 0);
                    _supportsLocalPlayers = true;
                }
                catch (EntryPointNotFoundException)
                {
                    _supportsLocalPlayers = false;
                }
            }

            return _supportsLocalPlayers.Value;
        }

        private void PushTestInputs(Input[] seatInputs)
        {
            if (!_supportsTestInputs)
            {
                return;
            }

            try
            {
                Externals.GGRSFFI.netplay_set_test_inputs(seatInputs, seatInputs.Length);
            }
            catch (EntryPointNotFoundException)
            {
                _supportsTestInputs = false;
            }
        }

        public void ResetCurrentInput()
        {
            _context.UpdateCurrentInputs(Enumerable.Range(0, TFGame.Players.Length).Select(_ => new Input()));
        }

        public void ResetPolledInput()
        {
            _context.ResetPolledInputs();
        }

        public void UpdateCurrent(IEnumerable<Input> inputs)
        {
            _context.UpdateCurrentInputs(inputs);
        }


        public void UpdatePolledInput(InputState input, RightStick rightStick = default, int index = -1)
        {
            index = index < 0 ? GetLocalPlayerInputIndex() : index;
            var polled = _context.GetPolledInput(index);

            var newInput = new Input
            {
                move_x = input.MoveX,
                move_y = input.MoveY,
                aim_axis = input.AimAxis.ToModel(),
                jump_check = input.JumpCheck ? input.JumpCheck.ToInt() : polled.jump_check,
                alt_shoot_check = input.AltShootCheck ? input.AltShootCheck.ToInt() : polled.alt_shoot_check,
                alt_shoot_pressed = input.AltShootPressed ? input.AltShootPressed.ToInt() : polled.alt_shoot_pressed,
                arrow_pressed = input.ArrowsPressed ? input.ArrowsPressed.ToInt() : polled.arrow_pressed,
                dodge_check = input.DodgeCheck ? input.DodgeCheck.ToInt() : polled.dodge_check,
                dodge_pressed = input.DodgePressed ? input.DodgePressed.ToInt() : polled.dodge_pressed,
                jump_pressed = input.JumpPressed ? input.JumpPressed.ToInt() : polled.jump_pressed,
                shoot_check = input.ShootCheck ? input.ShootCheck.ToInt() : polled.shoot_check,
                shoot_pressed = input.ShootPressed ? input.ShootPressed.ToInt() : polled.shoot_pressed,
                aim_right_axis = rightStick.AimAxis.ToModel(),
            };

            _context.UpdatePolledInput(index, newInput);
        }

        public void DisableAllControllers()
        {
            _context.SetInputLocked(true);

            if (!IsInNetplayLobby())
            {
                var playerCount = ServiceCollections.ResolveWiderSetModApi() != null ? 8 : 4;

                TFGame.PlayerInputs = new PlayerInput[playerCount];
                MenuInput.MenuInputs = new PlayerInput[5];
            }
        }

        public void EnableAllControllers()
        {
            _context.SetInputLocked(false);

            if (IsInNetplayLobby() && TFGame.PlayerInputs.Any(input => input != null))
            {
                return;
            }

            PlayerInput.AssignInputs();
            MenuInput.UpdateInputs();
        }

        public bool IsInputLocked()
        {
            return _context.IsInputLocked();
        }

        private static bool IsInNetplayLobby()
        {
            return !ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty;
        }

        public void DisableAllControllerExceptLocal()
        {
            var playerCount = ServiceCollections.ResolveWiderSetModApi() != null ? 8 : 4;
            var localSeat = _context.GetLocalPlayerIndex();

            var previous = _context.GetLocalInput();

            var localInput = previous == null
                ? null
                : TFGame.PlayerInputs.FirstOrDefault(input => input != null && input.ID == previous.ID);

            localInput ??= TFGame.PlayerInputs.FirstOrDefault(input => input != null);

            _context.SetLocalInput(localInput);

            RefreshSpareInputs(localInput);
            _localPlayersInputs.Clear();
            _joiningInputs.Clear();
            _lateJoinResponses = 0;

            for (int i = 0; i < playerCount; i++)
            {
                TFGame.PlayerInputs[i] = null;

                if (i < MenuInput.MenuInputs.Length)
                {
                    MenuInput.MenuInputs[i] = null;
                }
            }

            if (localSeat >= 0 && localSeat < TFGame.PlayerInputs.Length)
            {
                TFGame.PlayerInputs[localSeat] = localInput;
            }

            if (localSeat >= 0 && localSeat < MenuInput.MenuInputs.Length)
            {
                MenuInput.MenuInputs[localSeat] = localInput;
            }
        }
    }
}
