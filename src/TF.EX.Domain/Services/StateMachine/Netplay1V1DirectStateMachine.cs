using Monocle;
using System.Windows.Forms;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Domain.Services.StateMachine
{
    public class Netplay1V1DirectStateMachine : NetplayStateMachine
    {
        private string _text;
        private bool _isDoingTask = false;
        private string _clipped;
        private string _current_code = string.Empty;

        public Netplay1V1DirectStateMachine(IMatchmakingService matchmakingService) : base(matchmakingService)
        {
            _text = string.Empty;
            _isDoingTask = false;
        }

        public override string GetClipped()
        {
            return _clipped;
        }

        public override void Update()
        {
            switch (_state)
            {
                case Netplay1V1State.None:
                    HandleNone();
                    break;
                case Netplay1V1State.WaitingForRemotePlayerCode:
                    HandleWaitPlayerCode();
                    break;
                case Netplay1V1State.WaitingForRemotePlayerChoice:
                    HandleRemotePlayerCHoice();
                    break;
                case Netplay1V1State.Start:
                    _matchmakingService.DisconnectFromServer();
                    break;
                case Netplay1V1State.Finished:
                    break;
                case Netplay1V1State.Error:
                    break;
                default:
                    break;
            }

            var clipped = Clipboard.GetText().Trim();
            if (string.IsNullOrEmpty(_clipped)
                && string.IsNullOrEmpty(_text)
                && !string.IsNullOrEmpty(clipped)
                && clipped != _current_code
                )
            {
                Clipboard.SetData(DataFormats.Text, string.Empty);
                _clipped = clipped;
            }
        }

        private bool HasBothPlayerChoosed()
        {
            return TFGame.Players[0] && TFGame.Players[1];
        }

        public override void UpdateText(string text)
        {
            _text = text;
        }


        private void HandleNone()
        {
            Engine.Instance.Commands.Open = true;

            var code = _matchmakingService.GetDirectCode();

            _current_code = code;
            Clipboard.Clear();
            Clipboard.SetData(DataFormats.Text, code);

            Engine.Instance.Commands.Clear();
            Engine.Instance.Commands.Log("Your direct code is ");
            Engine.Instance.Commands.Log(code);
            Engine.Instance.Commands.Log("It has been copied to your clipboard.");
            Engine.Instance.Commands.Log("Please send it to your opponent.");
            Engine.Instance.Commands.Log("And Enter his code down below.");
            Engine.Instance.Commands.Log("You can copy the opponent code (Ctrl+C) or right click -> copy the opponent code to use the clipboard");
            _state = Netplay1V1State.WaitingForRemotePlayerCode;
        }

        private void HandleWaitPlayerCode()
        {
            if (!string.IsNullOrEmpty(_text))
            {
                if (!_isDoingTask)
                {
                    _isDoingTask = true;

                    Task.Run(async () =>
                    {

                        try
                        {
                            if (_text.Length != Constants.CODE_LENGTH)
                            {
                                Engine.Instance.Commands.Log("The input above is not a valid code, please try again");
                            }
                            else
                            {
                                var hasMatched = await _matchmakingService.SendOpponentCode(_text);

                                if (hasMatched)
                                {
                                    _state = Netplay1V1State.WaitingForRemotePlayerChoice;
                                }
                                else
                                {
                                    Engine.Instance.Commands.Log("Something went wrong...");
                                    _state = Netplay1V1State.Error;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Engine.Instance.Commands.Log("The input above is not a valid code, please try again " + e);
                        }
                        finally
                        {
                            _text = string.Empty;
                            _clipped = string.Empty;
                            _isDoingTask = false;
                        }
                    });
                }
            }
        }

        private void HandleRemotePlayerCHoice()
        {
            Engine.Instance.Commands.Clear();
            Engine.Instance.Commands.Log("Waiting for your opponent choice...");
            Engine.Instance.Commands.Log("(Your direct code is still");
            Engine.Instance.Commands.Log(_current_code);

            EnsureLocalPlayerChoiceWasSent();

            if (_matchmakingService.HasOpponentChoosed() && HasBothPlayerChoosed())
            {
                _state = Netplay1V1State.Start;
            }
        }
    }
}
