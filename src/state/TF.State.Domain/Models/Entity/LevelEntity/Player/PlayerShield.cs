using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class PlayerShield
    {
        [Key(0)]
        public Sprite<int> Shield { get; set; }
        [Key(1)]
        public float SineCounter { get; set; }
    }
}
