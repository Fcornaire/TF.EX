using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class Ice
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public float X { get; set; }

        [Key(2)]
        public float Width { get; set; }

        [Key(3)]
        public bool IsCollidable { get; set; }

        [Key(4)]
        public bool IsActive { get; set; }

        [Key(5)]
        public bool HasTiles { get; set; }

        [Key(6)]
        public float OldTilesAlpha { get; set; }

        [Key(7)]
        public int OldTilesXOffset { get; set; }
    }
}
