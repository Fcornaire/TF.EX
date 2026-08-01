using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class Bat
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public Vector2f PositionCounter { get; set; }

        [Key(3)]
        public Vector2f PreviousPosition { get; set; }

        [Key(4)]
        public Vector2f Speed { get; set; }

        [Key(5)]
        public int Facing { get; set; }

        [Key(6)]
        public int State { get; set; }

        [Key(7)]
        public int SwitchToState { get; set; }

        [Key(8)]
        public float StateCounter { get; set; }

        [Key(9)]
        public int StatePhase { get; set; }

        [Key(10)]
        public int SwoopIndex { get; set; }

        [Key(11)]
        public int Health { get; set; }

        [Key(12)]
        public int KillerIndex { get; set; }

        [Key(13)]
        public bool Dead { get; set; }

        [Key(14)]
        public bool IsCollidable { get; set; }

        [Key(15)]
        public bool Seek { get; set; }

        [Key(16)]
        public int BatType { get; set; }

        [Key(17)]
        public Sprite<string> Sprite { get; set; }

        [Key(18)]
        public float BobSine { get; set; }

        [Key(19)]
        public bool BobSineActive { get; set; }

        [Key(20)]
        public int TargetIndex { get; set; }

        [Key(21)]
        public Vector2f SwoopTargetSpeed { get; set; }

        [Key(22)]
        public float SwoopCooldown { get; set; }

        [Key(23)]
        public bool IgnoreJumpThrus { get; set; }

        [Key(24)]
        public ArrowCushion ArrowCushion { get; set; }
    }
}
