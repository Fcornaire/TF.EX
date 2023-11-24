using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Background
{
    [MessagePackObject]
    public class BGMushroom
    {
        [Key(0)]
        public Vector2f Position { get; set; }
        [Key(1)]
        public double ActualDepth { get; set; }
        [Key(2)]
        public Sprite<int> Sprite { get; set; }
    }
}
