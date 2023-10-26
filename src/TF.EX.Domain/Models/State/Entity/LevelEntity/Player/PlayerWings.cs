using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class PlayerWings
    {
        [Key(0)]
        public Sprite<string> Wings { get; set; }
        [Key(1)]
        public bool IsGaining { get; set; }
        [Key(2)]
        public float SpriteScaleTweenTimer { get; set; }
    }
}
