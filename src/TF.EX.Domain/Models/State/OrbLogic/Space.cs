using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.OrbLogic
{
    [MessagePackObject]
    public class Space
    {
        [Key(0)]
        public Counter SpaceCounter { get; set; }

        [Key(1)]
        public Vector2f TargetSpaceSpeed { get; set; }

        [Key(2)]
        public Vector2f SpaceSpeed { get; set; }

        [Key(3)]
        public Vector2f ScreenOffsetStart { get; set; }
        [Key(4)]
        public Vector2f ScreenOffsetEnd { get; set; }

        [Key(5)]
        public float SpaceTweenTimer { get; set; }

        public static Space Default => new Space
        {
            SpaceCounter = new Counter
            {
                CounterValue = -1
            },
            TargetSpaceSpeed = new Vector2f { X = -1, Y = -1 },
            SpaceSpeed = new Vector2f { X = -1, Y = -1 },
            SpaceTweenTimer = -1,
            ScreenOffsetStart = new Vector2f { X = 0, Y = 0 },
            ScreenOffsetEnd = new Vector2f { X = 0, Y = 0 }
        };

    }
}
