using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Domain.Ports.TF
{
    public interface IInputService
    {
        void UpdateCurrent(IEnumerable<Input> inputs);
        void UpdatePolledInput(int seat, InputState input, RightStick rightStick = default);
        void ResetPolledInput();
        Input GetCurrentInput(int characterIndex);
        Input GetPolledInput();
        List<Input> GetCurrentInputs();

        int GetLocalPlayerInputIndex();
        bool IsLocalSeat(int seat);
        void PushExtraLocalInputs();
        void ResetCurrentInput();
        void EnsureRemoteController(int playerCount);
        void EnsureRemoteControllers(IEnumerable<int> seats);
        bool EnsureLocalControllerSeats();
        void EnsureEveryControllerSlot();

        void EnsureFakeControllers();

        void RemoveFakeControllers();

        int GetInputIndex(PlayerInput input);
        void ObserveLocalDevice(bool shouldCheckStartAndBack);
        void RebindLocalInput();
        void DisableAllControllers();
        void EnableAllControllers();

        void DisableAllControllerExceptLocal();
        bool IsInputLocked();
        PlayerInput GetLocalInput();
        bool HasUsableLocalInput();
        void ClaimLocalInput(PlayerInput input);
        IReadOnlyList<PlayerInput> GetGuestInputs();
        int GetLocalPlayerCount();
        bool TryAddGuestInput(PlayerInput input);
        void RemoveGuestInput(PlayerInput input);
        void ClearGuestInputs();
        void KeepOnlyLocalInputInMenus();
        IEnumerable<PlayerInput> GetJoinableDevices();
    }
}
