using MessagePack;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;

namespace TF.EX.Domain.Models.State.Component
{
    [MessagePackObject]
    public class ArrowCushion
    {
        [Key(0)]
        public Vector2f Offset { get; set; }

        [Key(1)]
        public float Rotation { get; set; }

        [Key(2)]
        public bool LockOffset { get; set; }

        [Key(3)]
        public bool LockDirection { get; set; }

        [Key(4)]
        public List<ArrowCushionData> ArrowCushionDatas { get; set; }

        [Key(5)]
        public double EntityActualDepth { get; set; }
    }
}
