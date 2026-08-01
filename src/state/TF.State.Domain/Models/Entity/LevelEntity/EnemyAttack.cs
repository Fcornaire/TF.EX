using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class EnemyAttack
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public double EnemyActualDepth { get; set; }

        [Key(2)]
        public Vector2f Offset { get; set; }

        [Key(3)]
        public float Width { get; set; }

        [Key(4)]
        public float Height { get; set; }

        [Key(5)]
        public float Timer { get; set; }
    }
}
