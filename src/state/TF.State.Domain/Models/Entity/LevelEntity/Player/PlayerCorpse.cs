using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class PlayerCorpse
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public Vector2f PositionCounter { get; set; }

        [Key(2)]
        public int PlayerIndex { get; set; }

        [Key(3)]
        public int KillerIndex { get; set; }

        [Key(4)]
        public Vector2f Speed { get; set; }

        [Key(5)]
        public Facing Facing { get; set; }

        [Key(6)]
        public float FallSpriteCounter { get; set; }

        [Key(7)]
        public bool Pinned { get; set; }

        [Key(8)]
        public ArrowCushion ArrowCushion { get; set; }

        [Key(9)]
        public double ActualDepth { get; set; }

        [Key(10)]
        public bool AgainstWall { get; set; }

        [Key(11)]
        public float ExplodingCounter { get; set; }

        [Key(12)]
        public PlayerArrowsInventory Arrows { get; set; }

        [Key(13)]
        public int DropDir { get; set; }

        [Key(14)]
        public float DropArrowAlarmFramesLeft { get; set; }

        [Key(15)]
        public bool Reviving { get; set; }

        [Key(16)]
        public bool Revived { get; set; }

        [Key(17)]
        public int Ledge { get; set; }

        [Key(18)]
        public bool IgnoreJumpThrus { get; set; }

        [Key(19)]
        public Vector2f Squished { get; set; }

        [Key(20)]
        public float SquishedCounter { get; set; }

        [Key(21)]
        public float GhostSpawnCounter { get; set; }

        [Key(22)]
        public bool PrismHit { get; set; }

        [Key(23)]
        public bool PrismFall { get; set; }

        [Key(24)]
        public float PrismTicks { get; set; }

        [Key(25)]
        public bool HasBrambles { get; set; }

        [Key(26)]
        public float BrambleTicks { get; set; }

        [Key(27)]
        public bool BrambleCollidable { get; set; }

        [Key(28)]
        public bool BramblesVisible { get; set; }

        [Key(29)]
        public bool NotCollidable { get; set; }

        [Key(30)]
        public bool NotPushable { get; set; }

        [Key(31)]
        public float DodgeTooLateCounter { get; set; }

        [Key(32)]
        public int Depth { get; set; }
    }

    [MessagePackObject]
    public class ArrowCushionData
    {
        [Key(0)]
        public double ActualDepth;

        [Key(1)]
        public Vector2f Offset;

        [Key(2)]
        public float Rotation;
    }
}
