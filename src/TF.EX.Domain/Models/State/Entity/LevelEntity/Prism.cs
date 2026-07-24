using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class Prism
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public float Counter { get; set; }

        [Key(3)]
        public bool Finished { get; set; }

        [Key(4)]
        public bool StartedShaking { get; set; }

        [Key(5)]
        public int OwnerIndex { get; set; }

        [Key(6)]
        public int EncasedPlayerIndex { get; set; }

        [Key(7)]
        public bool IsCollidable { get; set; }

        [Key(8)]
        public bool OnlyCollidableSwitch { get; set; }

        [Key(9)]
        public int OnlyPlayerIndex { get; set; }
    }
}
