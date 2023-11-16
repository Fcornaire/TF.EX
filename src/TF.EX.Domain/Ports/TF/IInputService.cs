using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.Domain.Ports.TF
{
    public interface IInputService
    {
        void UpdateCurrent(IEnumerable<Input> inputs);
        void UpdatePolledInput(InputState input, RightStick rightStick = default);
        void ResetPolledInput();
        Input GetCurrentInput(int characterIndex);
        Input GetPolledInput();
        List<Input> GetCurrentInputs();

        int GetLocalPlayerInputIndex();
        int GetRemotePlayerInputIndex();
        void ResetCurrentInput();
        void EnsureRemoteController();

        void EnsureFakeControllers();

        int GetInputIndex(PlayerInput input);
        void DisableAllControllers();
        void EnableAllControllers();

        void DisableAllControllerExceptLocal();
    }
}
