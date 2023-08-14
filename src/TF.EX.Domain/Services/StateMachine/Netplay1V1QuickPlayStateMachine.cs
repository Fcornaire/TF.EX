using Monocle;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Domain.Services.StateMachine
{
    public class Netplay1V1QuickPlayStateMachine : NetplayStateMachine
    {
        public Netplay1V1QuickPlayStateMachine(IMatchmakingService matchmakingService) : base(matchmakingService)
        {
        }

        public override string GetClipped()
        {
            return string.Empty;
        }

        public override void Update()
        {
            switch (_state)
            {
                case Netplay1V1State.None:
                    HandleNone();
                    break;
                case Netplay1V1State.WaitingForRemotePlayerChoice:
                    HandleRemotePlayerCHoice();
                    break;
                case Netplay1V1State.Start:
                case Netplay1V1State.Finished:
                case Netplay1V1State.Error:
                case Netplay1V1State.WaitingForRemotePlayerCode:
                default:
                    break;
            }
        }

        private void HandleNone()
        {
            Engine.Instance.Commands.Open = true;
            _state = Netplay1V1State.WaitingForRemotePlayerChoice;
        }

        private void HandleRemotePlayerCHoice()
        {
            Engine.Instance.Commands.Clear();
            Engine.Instance.Commands.Log("Waiting for your opponent choice...");

            EnsureLocalPlayerChoiceWasSent();

            if (HasBothPlayerChoosed())
            {
                _state = Netplay1V1State.Start;
            }
        }

        private bool HasBothPlayerChoosed()
        {
            return _matchmakingService.HasOpponentChoosed() && TFGame.Players[0] && TFGame.Players[1];
        }
    }
}
