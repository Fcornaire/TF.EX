using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Domain.Services.TF
{
    public class InputService(IGameContext context) : IInputService
    {
        private static bool _useTestInputs = true;
        private static bool _useLocalInputs = true;
        private readonly Input[] _extraLocalInputs = new Input[Constants.MAX_SEATS];


        public int GetInputIndex(PlayerInput input)
        {
            var idx = TFGame.PlayerInputs
                .Select((x, i) => new { Value = x, Index = i })
                .FirstOrDefault(x => x.Value == input)?.Index;

            return idx ?? -1;
        }

        public void ObserveLocalDevice(bool shouldCheckStartAndBack)
        {
            var guests = context.GetGuestInputs();

            foreach (var input in TFGame.PlayerInputs)
            {
                if (input == null || !input.Attached || !HasMenuActivity(input, shouldCheckStartAndBack) || guests.Any(guest => IsSameDevice(guest, input)))
                {
                    continue;
                }

                context.SetLocalInput(input);
                return;
            }
        }

        public void RebindLocalInput()
        {
            var previous = context.GetLocalInput();

            if (previous != null)
            {
                var match = TFGame.PlayerInputs.FirstOrDefault(input => IsSameDevice(input, previous))
                    ?? TFGame.PlayerInputs.FirstOrDefault(input => input != null && input.Attached);

                if (match != null && !ReferenceEquals(match, previous))
                {
                    context.SetLocalInput(match);
                }
            }

            RebindGuestInputs();
        }

        private void RebindGuestInputs()
        {
            context.SetGuestInputs([.. context.GetGuestInputs().Select(guest => TFGame.PlayerInputs.FirstOrDefault(input => IsSameDevice(input, guest)) ?? guest)]);
        }

        public PlayerInput GetLocalInput()
        {
            return context.GetLocalInput();
        }

        public bool HasUsableLocalInput()
        {
            var owner = context.GetLocalInput();

            return owner != null && owner.Attached;
        }

        public void ClaimLocalInput(PlayerInput input)
        {
            context.SetLocalInput(input);
        }

        public IReadOnlyList<PlayerInput> GetGuestInputs()
        {
            return context.GetGuestInputs();
        }

        public int GetLocalPlayerCount()
        {
            return 1 + context.GetGuestInputs().Count;
        }

        public bool TryAddGuestInput(PlayerInput input)
        {
            var guests = context.GetGuestInputs();

            if (input == null
                || 1 + guests.Count >= Constants.MAX_LOCAL_PLAYERS
                || IsSameDevice(input, context.GetLocalInput())
                || guests.Any(guest => IsSameDevice(guest, input)))
            {
                return false;
            }

            context.SetGuestInputs([.. guests, input]);

            return true;
        }

        public void RemoveGuestInput(PlayerInput input)
        {
            context.SetGuestInputs([.. context.GetGuestInputs().Where(guest => !IsSameDevice(guest, input))]);
        }

        public void ClearGuestInputs()
        {
            context.SetGuestInputs([]);
        }

        public void KeepOnlyLocalInputInMenus()
        {
            var owner = context.GetLocalInput();

            if (!HasUsableLocalInput())
            {
                return;
            }

            var ownerIndex = Array.IndexOf(MenuInput.MenuInputs, owner);

            if (ownerIndex < 0)
            {
                ownerIndex = Array.FindIndex(MenuInput.MenuInputs, input => IsSameDevice(input, owner));
            }

            for (int i = 0; i < MenuInput.MenuInputs.Length; i++)
            {
                if (i != ownerIndex)
                {
                    MenuInput.MenuInputs[i] = null;
                }
            }

            if (ownerIndex < 0 && MenuInput.MenuInputs.Length > 0)
            {
                MenuInput.MenuInputs[0] = owner;
            }
        }

        public IEnumerable<PlayerInput> GetJoinableDevices()
        {
            var owner = context.GetLocalInput();
            var guests = context.GetGuestInputs();
            var devices = new List<PlayerInput>();

            foreach (var input in TFGame.PlayerInputs.Concat(MenuInput.MenuInputs))
            {
                if (input == null
                    || !input.Attached
                    || input is FakeController
                    || IsSameDevice(input, owner)
                    || guests.Any(guest => IsSameDevice(guest, input))
                    || devices.Any(device => IsSameDevice(device, input)))
                {
                    continue;
                }

                devices.Add(input);
            }

            return devices;
        }

        public static bool IsSameDevice(PlayerInput input, PlayerInput other)
        {
            if (input == null || other == null)
            {
                return false;
            }

            if (ReferenceEquals(input, other))
            {
                return true;
            }

            if (input is XGamepadInput pad && other is XGamepadInput otherPad)
            {
                return ReferenceEquals(pad.XGamepad, otherPad.XGamepad);
            }

            return input is KeyboardInput && other is KeyboardInput && input.ID == other.ID;
        }

        private static bool HasMenuActivity(PlayerInput input, bool shouldCheckStartAndBack)
        {
            return input.MenuConfirmCheck || shouldCheckStartAndBack && (input.MenuStartCheck || input.MenuBackCheck)
                || input.MenuUpCheck || input.MenuDownCheck
                || input.MenuLeftCheck || input.MenuRightCheck
                || input.MenuAltCheck || input.MenuAlt2Check;
        }

        public bool EnsureLocalControllerSeats()
        {
            var hasChanged = false;

            foreach (var (seat, device) in GetLocalDevicePlacements())
            {
                hasChanged |= PlaceLocalDevice(seat, device);
            }

            return hasChanged;
        }

        private List<(int Seat, PlayerInput Device)> GetLocalDevicePlacements()
        {
            var placements = new List<(int Seat, PlayerInput Device)>
            {
                (context.GetLocalPlayerIndex(), context.GetLocalInput())
            };

            var guests = context.GetGuestInputs();

            foreach (var player in ServiceCollections.ResolveMatchmakingService().GetLocalPlayers().Where(player => player.IsGuest))
            {
                var device = guests.ElementAtOrDefault(player.LocalIndex - 1);

                if (device != null)
                {
                    placements.Add((player.Seat, device));
                }
            }

            return placements;
        }

        private static bool PlaceLocalDevice(int seat, PlayerInput device)
        {
            if (device == null || seat < 0 || seat >= TFGame.PlayerInputs.Length)
            {
                return false;
            }

            if (TFGame.PlayerInputs[seat] == device)
            {
                SetLocalMenuInput(seat, device);
                return false;
            }

            var displaced = TFGame.PlayerInputs[seat];

            for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
            {
                if (TFGame.PlayerInputs[i] == device)
                {
                    TFGame.PlayerInputs[i] = displaced;
                }
            }

            TFGame.PlayerInputs[seat] = device;
            SetLocalMenuInput(seat, device);

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
            foreach (var seat in seats)
            {
                if (context.IsLocalSeat(seat) || seat < 0 || seat >= TFGame.PlayerInputs.Length)
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
            return context.GetCurrentInput(characterIndex);
        }

        public List<Input> GetCurrentInputs()
        {
            return context.GetCurrentInputs();
        }

        public int GetLocalPlayerInputIndex()
        {
            return context.GetLocalPlayerIndex();
        }

        public bool IsLocalSeat(int seat)
        {
            return context.IsLocalSeat(seat);
        }

        public void PushExtraLocalInputs()
        {
            var seats = context.GetSessionSeats();

            if (seats.Count < 2 || !_useLocalInputs)
            {
                return;
            }

            Array.Clear(_extraLocalInputs, 0, _extraLocalInputs.Length);

            for (int i = 1; i < seats.Count; i++)
            {
                var seat = seats[i];

                if (seat >= 0 && seat < _extraLocalInputs.Length)
                {
                    _extraLocalInputs[seat] = context.GetPolledInput(seat);
                }
            }

            try
            {
                Externals.GGRSFFI.netplay_set_local_inputs(_extraLocalInputs, _extraLocalInputs.Length);
            }
            catch (EntryPointNotFoundException)
            {
                _useLocalInputs = false;
            }
        }

        public Input GetPolledInput()
        {
            if (InputScripter.Enabled)
            {
                var seatInputs = InputScripter.GetAllInputs(Externals.GGRSFFI.netplay_current_frame());

                PushTestInputs(seatInputs);

                return seatInputs[0];
            }

            return context.GetPolledInput(context.GetLocalPlayerIndex());
        }

        private void PushTestInputs(Input[] seatInputs)
        {
            if (!_useTestInputs)
            {
                return;
            }

            try
            {
                Externals.GGRSFFI.netplay_set_test_inputs(seatInputs, seatInputs.Length);
            }
            catch (EntryPointNotFoundException)
            {
                _useTestInputs = false;
            }
        }

        public void ResetCurrentInput()
        {
            context.UpdateCurrentInputs(Enumerable.Range(0, TFGame.Players.Length).Select(_ => new Input()));
        }

        public void ResetPolledInput()
        {
            context.ResetPolledInputs();
        }

        public void UpdateCurrent(IEnumerable<Input> inputs)
        {
            context.UpdateCurrentInputs(inputs);
        }


        public void UpdatePolledInput(int seat, InputState input, RightStick rightStick = default)
        {
            var polled = context.GetPolledInput(seat);

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

            context.UpdatePolledInput(seat, newInput);
        }

        public void DisableAllControllers()
        {
            context.SetInputLocked(true);

            if (!IsInNetplayLobby())
            {
                var playerCount = ServiceCollections.ResolveWiderSetModApi() != null ? 8 : 4;

                TFGame.PlayerInputs = new PlayerInput[playerCount];
                MenuInput.MenuInputs = new PlayerInput[5];
            }
        }

        public void EnableAllControllers()
        {
            context.SetInputLocked(false);

            if (IsInNetplayLobby() && TFGame.PlayerInputs.Any(input => input != null))
            {
                return;
            }

            PlayerInput.AssignInputs();
            MenuInput.UpdateInputs();
        }

        public bool IsInputLocked()
        {
            return context.IsInputLocked();
        }

        private static bool IsInNetplayLobby()
        {
            return !ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty;
        }

        public void DisableAllControllerExceptLocal()
        {
            var playerCount = ServiceCollections.ResolveWiderSetModApi() != null ? 8 : 4;

            var previous = context.GetLocalInput();

            var localInput = TFGame.PlayerInputs.FirstOrDefault(input => IsSameDevice(input, previous))
                ?? TFGame.PlayerInputs.FirstOrDefault(input => input != null);

            context.SetLocalInput(localInput);
            RebindGuestInputs();

            var placements = GetLocalDevicePlacements();

            for (int i = 0; i < playerCount; i++)
            {
                TFGame.PlayerInputs[i] = null;

                if (i < MenuInput.MenuInputs.Length)
                {
                    MenuInput.MenuInputs[i] = null;
                }
            }

            foreach (var (seat, device) in placements)
            {
                if (seat >= 0 && seat < TFGame.PlayerInputs.Length)
                {
                    TFGame.PlayerInputs[seat] = device;
                }

                if (seat >= 0 && seat < MenuInput.MenuInputs.Length)
                {
                    MenuInput.MenuInputs[seat] = device;
                }
            }
        }
    }
}
