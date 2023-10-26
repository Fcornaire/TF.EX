using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class Lava
    {
        [Key(0)]
        public LavaSide Side { get; set; }
        [Key(1)]
        public bool IsCollidable { get; set; }
        [Key(2)]
        public Vector2f Position { get; set; }
        [Key(3)]
        public float Percent { get; set; }
        [Key(4)]
        public float SineCounter { get; set; }
    }
}
