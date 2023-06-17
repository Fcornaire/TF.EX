namespace TF.EX.Domain.Models.State
{
    public class PlayerState
    {
        public PlayerStates CurrentState { get; set; }
        public PlayerStates PreviousState { get; set; }

        public PlayerState(PlayerStates current, PlayerStates previous)
        {
            CurrentState = current;
            PreviousState = previous;
        }

    }
}
