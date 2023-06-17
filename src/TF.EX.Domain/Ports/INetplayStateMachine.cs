namespace TF.EX.Domain.Ports
{
    public enum Netplay1V1State : int
    {
        None = 0,
        WaitingForRemotePlayerCode = 1,
        WaitingForRemotePlayerChoice = 2,
        Start = 3,
        Finished = 4,
        Error = 5
    }

    public interface INetplayStateMachine
    {
        void Update();

        bool IsWaitingForUserAction();

        bool CanStart();

        bool IsInitialized();

        void UpdateText(string text);

        string GetClipped();

        void Reset();
    }
}
