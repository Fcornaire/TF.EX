using TowerFall;

namespace TF.Replay.Domain
{
    internal static class ReplayInputGate
    {
        public static void SetInputEnabled(bool enabled)
        {
            if (enabled)
            {
                PlayerInput.AssignInputs();
                MenuInput.UpdateInputs();
                return;
            }

            TFGame.PlayerInputs = new PlayerInput[TFGame.PlayerInputs?.Length ?? 4];
            MenuInput.MenuInputs = new PlayerInput[5];
        }
    }
}
