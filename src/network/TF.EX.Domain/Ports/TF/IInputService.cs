using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Domain.Ports.TF
{
    public interface IInputService
    {
        void UpdateCurrent(IEnumerable<Input> inputs);
        void UpdatePolledInput(InputState input, RightStick rightStick = default, int index = -1);
        void PushLocalPlayersInputs(bool neutral);
        void ResetPolledInput();
        Input GetCurrentInput(int characterIndex);
        Input GetPolledInput();
        List<Input> GetCurrentInputs();

        int GetLocalPlayerInputIndex();
        bool IsLocalPlayerIndex(int index);
        bool IsLocalInput(PlayerInput input);
        bool IsSpareInput(PlayerInput input);
        void UpdateLocalPlayers();
        void PlaceLocalPlayersInputs();
        void ResetCurrentInput();
        void EnsureRemoteController(int playerCount);
        void EnsureRemoteControllers(IEnumerable<int> seats);
        bool EnsureLocalControllerSeat();
        void EnsureEveryControllerSlot();

        void EnsureFakeControllers();

        void RemoveFakeControllers();

        int GetInputIndex(PlayerInput input);
        void ObserveLocalDevice();
        void RebindLocalInput();
        void DisableAllControllers();
        void EnableAllControllers();

        void DisableAllControllerExceptLocal();
        bool IsInputLocked();
    }
}
