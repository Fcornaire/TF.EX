using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Domain.Services.TF
{
    public class InputService : IInputService
    {
        private readonly IGameContext _context;

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

        /// <summary>
        /// Used to add controller for remote players
        /// </summary>
        public void EnsureRemoteController() //TODO: Handle more than 2 players
        {
            if (TFGame.PlayerInputs[1] is null)
            {
                TFGame.PlayerInputs[1] = new KeyboardInput();
            }
        }

        public void EnsureFakeControllers()
        {
            if (TFGame.PlayerInputs[1] is null)
            {
                TFGame.PlayerInputs[1] = new FakeController();
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
            return _context.GetPolledInput();
        }

        public int GetRemotePlayerInputIndex()
        {
            return _context.GetRemotePlayerIndex();
        }

        public void ResetCurrentInput()
        {
            var defaultInputs = new List<Input>
            {
                new Input(),
                new Input(),
            };

            _context.UpdateCurrentInputs(defaultInputs);
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
            TFGame.PlayerInputs = new PlayerInput[4];
            MenuInput.MenuInputs = new PlayerInput[5];
        }

        public void EnableAllControllers()
        {
            PlayerInput.AssignInputs();
            MenuInput.UpdateInputs();
        }

        public void DisableAllControllerExceptLocal()
        {
            for (int i = 1; i < 4; i++)
            {
                TFGame.PlayerInputs[i] = null;
                MenuInput.MenuInputs[i] = null;
            };
        }
    }
}
