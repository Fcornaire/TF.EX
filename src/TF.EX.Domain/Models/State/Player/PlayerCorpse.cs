using System.Collections.Generic;
using TowerFall;

namespace TF.EX.Domain.Models.State.Player
{
    public class PlayerCorpse
    {
        public Vector2f Position { get; set; }
        public Vector2f PositionCounter { get; set; }
        public int PlayerIndex { get; set; }
        public int KillerIndex { get; set; }
        public Vector2f Speed { get; set; }

        public Facing Facing { get; set; }
        public float FallSpriteCounter { get; set; }

        public bool Pinned { get; set; }

        public ArrowCushion ArrowCushion { get; set; }

        public double ActualDepth { get; set; }
    }

    public class ArrowCushion
    {
        public Vector2f Offset { get; set; }
        public float Rotation { get; set; }
        public bool LockOffset { get; set; }
        public bool LockDirection { get; set; }
        public List<ArrowCushionData> ArrowCushionDatas { get; set; }
    }

    public class ArrowCushionData
    {
        public double ActualDepth;

        public Vector2f Offset;

        public float Rotation;
    }
}
