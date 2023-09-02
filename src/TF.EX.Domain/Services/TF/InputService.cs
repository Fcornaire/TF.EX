using Microsoft.Extensions.Logging;
using TF.EX.Common.Extensions;
using TF.EX.Domain.Context;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Domain.Services.TF
{
    public class InputService : IInputService
    {
        private readonly IGameContext _context;
        private readonly ILogger _logger;
        private PlayerInput[] _originalPlayersInput;

        public InputService(IGameContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
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

        public InputState GetCurrentInput(int characterIndex)
        {
            return _context.GetCurrentInput(characterIndex);
        }

        public List<InputState> GetCurrentInputs()
        {
            return _context.GetCurrentInputs();
        }

        public int GetLocalPlayerInputIndex()
        {
            return _context.GetLocalPlayerIndex();
        }

        public InputState GetPolledInput()
        {
            return _context.GetPolledInput();
        }

        public int GetRemotePlayerInputIndex()
        {
            return _context.GetRemotePlayerIndex();
        }

        public void ResetCurrentInput()
        {
            var defaultInputs = new List<InputState>
            {
                new InputState(),
                new InputState(),
            };

            _context.UpdateCurrentInputs(defaultInputs);
        }

        public void ResetPolledInput()
        {
            _context.UpdatePolledInput(new InputState());
        }

        public void UpdateCurrent(IEnumerable<InputState> inputs)
        {
            _context.UpdateCurrentInputs(inputs);
        }


        public void UpdatePolledInput(InputState input)
        {
            var polled = _context.GetPolledInput();

            var newInput = new InputState
            {
                MoveX = input.MoveX,
                MoveY = input.MoveY,
                AimAxis = input.AimAxis,
                JumpCheck = input.JumpCheck ? input.JumpCheck : polled.JumpCheck,
                AltShootCheck = input.AltShootCheck ? input.AltShootCheck : polled.AltShootCheck,
                AltShootPressed = input.AltShootPressed ? input.AltShootPressed : polled.AltShootPressed,
                ArrowsPressed = input.ArrowsPressed ? input.ArrowsPressed : polled.ArrowsPressed,
                DodgeCheck = input.DodgeCheck ? input.DodgeCheck : polled.DodgeCheck,
                DodgePressed = input.DodgePressed ? input.DodgePressed : polled.DodgePressed,
                JumpPressed = input.JumpPressed ? input.JumpPressed : polled.JumpPressed,
                ShootCheck = input.ShootCheck ? input.ShootCheck : polled.ShootCheck,
                ShootPressed = input.ShootPressed ? input.ShootPressed : polled.ShootPressed
            };

            _context.UpdatePolledInput(newInput);
        }

        public void DisableAllController()
        {
            _originalPlayersInput = TFGame.PlayerInputs;
            TFGame.PlayerInputs = new PlayerInput[4];
            MenuInput.MenuInputs = new PlayerInput[5];
        }

        public void EnableAllController()
        {
            if (_originalPlayersInput != null)
            {
                TFGame.PlayerInputs = _originalPlayersInput;
                MenuInput.UpdateInputs();
            }
            else
            {
                _logger.LogDebug<InputService>("No players input found");
            }
        }
    }
}
