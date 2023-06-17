using TF.EX.Domain.Ports;

namespace TF.EX.Domain.Services.StateMachine
{
    public class DefaultNetplayStateMachine : INetplayStateMachine
    {

        public bool CanStart()
        {
            return false;
        }

        public string GetClipped()
        {
            throw new NotImplementedException();
        }

        public bool IsInitialized()
        {
            return false;
        }

        public bool IsWaitingForUserAction()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {

        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        public void UpdateText(string text)
        {
            throw new NotImplementedException();
        }
    }
}
