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
            var localSeat = _context.GetLocalPlayerIndex();
            var localInput = _context.GetLocalInput();

            if (localInput == null || localSeat < 0 || localSeat >= TFGame.PlayerInputs.Length)
            {
                return false;
            }

            if (TFGame.PlayerInputs[localSeat] == localInput)
            {
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

            for (int i = 0; i < MenuInput.MenuInputs.Length; i++)
            {
                if (MenuInput.MenuInputs[i] == localInput)
                {
                    MenuInput.MenuInputs[i] = null;
                }
            }

            if (localSeat < MenuInput.MenuInputs.Length)
            {
                MenuInput.MenuInputs[localSeat] = localInput;
            }

            return true;
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

        public Input GetPolledInput()
        {
            if (InputScripter.Enabled)
            {
                var seatInputs = InputScripter.GetAllInputs(Externals.GGRSFFI.netplay_current_frame());

                PushTestInputs(seatInputs);

                return seatInputs[0];
            }

            return _context.GetPolledInput();
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
            _context.UpdatePolledInput(new Input());
        }

        public void UpdateCurrent(IEnumerable<Input> inputs)
        {
            _context.UpdateCurrentInputs(inputs);
        }


        public void UpdatePolledInput(InputState input, RightStick rightStick = default)
        {
            var polled = _context.GetPolledInput();

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

            _context.UpdatePolledInput(newInput);
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
            var localMenuInput = MenuInput.MenuInputs.FirstOrDefault(input => input != null);

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
                MenuInput.MenuInputs[localSeat] = localMenuInput;
            }
        }
    }
}
