using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class PlayerAnimations
    {
        [Key(0)]
        public Sprite<string> Body { get; set; }

        [Key(1)]
        public Sprite<string> Head { get; set; }

        [Key(2)]
        public Sprite<string> HeadBack { get; set; }

        [Key(3)]
        public Sprite<string> Bow { get; set; }

        [Key(4)]
        public PlayerShield Shield { get; set; }

        [Key(5)]
        public PlayerWings Wings { get; set; }
    }
}
