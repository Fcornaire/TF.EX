using TowerFall;

namespace TF.EX.Domain.Ports.TF
{
    public interface IInputService
    {
        void UpdateCurrent(IEnumerable<InputState> inputs);
        void UpdatePolledInput(InputState input);
        void ResetPolledInput();
        InputState GetCurrentInput(int characterIndex);
        InputState GetPolledInput();
        List<InputState> GetCurrentInputs();

        int GetLocalPlayerInputIndex();
        int GetRemotePlayerInputIndex();
        void ResetCurrentInput();
        void EnsureRemoteController();

        int GetInputIndex(PlayerInput input);
        void DisableAllController();
        void EnableAllController();
    }
}
