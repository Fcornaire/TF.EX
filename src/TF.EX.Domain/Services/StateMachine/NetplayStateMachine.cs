using TF.EX.Domain.Externals;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Domain.Services.StateMachine
{
    public abstract class NetplayStateMachine : INetplayStateMachine
    {
        protected Netplay1V1State _state;
        protected readonly IMatchmakingService _matchmakingService;
        protected bool _hasLocalPlayerChoosed = false;

        public NetplayStateMachine(IMatchmakingService matchmakingService)
        {
            _state = Netplay1V1State.None;
            _matchmakingService = matchmakingService;
        }

        public bool CanStart()
        {
            return _state == Netplay1V1State.Start;
        }

        public abstract string GetClipped();

        public bool IsInitialized()
        {
            return _state != Netplay1V1State.None;
        }

        public bool IsWaitingForUserAction()
        {
            return _state == Netplay1V1State.WaitingForRemotePlayerCode
                || _state == Netplay1V1State.WaitingForRemotePlayerChoice;
        }

        public void Reset()
        {
            _state = Netplay1V1State.None;
            _hasLocalPlayerChoosed = false;
        }

        public abstract void Update();

        public virtual void UpdateText(string text)
        {
            //Nothing to do, used in direct mode
        }

        protected void EnsureLocalPlayerChoiceWasSent()
        {
            if (!_hasLocalPlayerChoosed && TFGame.Players[0])
            {
                var archer_alt = $"Archer : {TFGame.Characters[0]}-{TFGame.AltSelect[0]}";
                MatchboxClientFFI.send_message(archer_alt, _matchmakingService.GetOpponentPeerId().ToString());
                _hasLocalPlayerChoosed = true;
            }
        }
    }
}
