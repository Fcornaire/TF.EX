using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class PlayerState
    {
        [Key(0)]
        public PlayerStates CurrentState { get; set; }

        [Key(1)]
        public PlayerStates PreviousState { get; set; }
    }
}
