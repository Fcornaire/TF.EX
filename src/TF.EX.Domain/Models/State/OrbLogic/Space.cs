using TF.EX.Domain.Models.State.Monocle;

namespace TF.EX.Domain.Models.State.OrbLogic
{
    public class Space
    {
        public Counter SpaceCounter { get; set; }

        public Vector2f TargetSpaceSpeed { get; set; }

        public Vector2f SpaceSpeed { get; set; }

        public Vector2f ScreenOffsetStart { get; set; }
        public Vector2f ScreenOffsetEnd { get; set; }

        public float SpaceTweenTimer { get; set; }

        public static Space Default => new Space
        {
            SpaceCounter = new Counter
            {
                CounterValue = -1
            },
            TargetSpaceSpeed = new Vector2f(-1, -1),
            SpaceSpeed = new Vector2f(-1, -1),
            SpaceTweenTimer = -1,
            ScreenOffsetStart = new Vector2f(0, 0),
            ScreenOffsetEnd = new Vector2f(0, 0)
        };

    }
}
