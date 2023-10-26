using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class Player
    {
        [Key(0)]
        public bool IsCollidable { get; set; }

        [Key(1)]
        public bool IsDead { get; set; }

        [Key(2)]
        public Vector2f Position { get; set; }

        [Key(3)]
        public Vector2f PositionCounter { get; set; }

        [Key(4)]
        public int Facing { get; set; }

        [Key(5)]
        public PlayerArrowsInventory ArrowsInventory { get; set; }

        [Key(6)]
        public float WallStickMax { get; set; }

        [Key(7)]
        public Vector2f Speed { get; set; }

        [Key(8)]
        public float FlapGravity { get; set; }

        [Key(9)]
        public bool CanHyper { get; set; }

        [Key(10)]
        public PlayerState State { get; set; }

        [Key(11)]
        public float JumpBufferCounter { get; set; }

        [Key(12)]
        public float DodgeEndCounter { get; set; }

        [Key(13)]
        public float DodgeStallCounter { get; set; }

        [Key(14)]
        public float JumpGraceCounter { get; set; }

        [Key(15)]
        public float DodgeCatchCounter { get; set; }

        [Key(16)]
        public float DyingCounter { get; set; }

        [Key(17)]
        public float FlapBounceCounter { get; set; }

        [Key(18)]
        public Counter WingsFireCounter { get; set; }

        [Key(19)]
        public FireControl FireControl { get; set; }

        [Key(20)]
        public DodgeSlide DodgeSlide { get; set; }

        [Key(21)]
        public bool DodgeCooldown { get; set; }

        [Key(22)]
        public Scheduler Scheduler { get; set; }

        [Key(23)]
        public Hitbox Hitbox { get; set; }

        [Key(24)]
        public int AutoMove { get; set; }

        [Key(25)]
        public bool Aiming { get; set; }

        [Key(26)]
        public bool CanVarJump { get; set; }

        [Key(27)]
        public bool IsOnGround { get; set; }

        [Key(28)]
        public float DuckSlipCounter { get; set; } //Not a 'Counter'

        [Key(29)]
        public bool IsShieldVisible { get; set; }

        [Key(30)]
        public bool IsWingsVisible { get; set; }

        [Key(31)]
        public Flash Flash { get; set; }

        [Key(32)]
        public double DeathArrowDepth { get; set; }

        [Key(33)]
        public int Index { get; set; }

        [Key(34)]
        public bool ShouldDrawSelf { get; set; }

        [Key(35)]
        public bool ShouldStartAimingDown { get; set; }

        [Key(36)]
        public int GraceLedgeDir { get; set; }

        [Key(37)]
        public double ActualDepth { get; set; }

        [Key(38)]
        public bool MarkedForRemoval { get; set; }

        [Key(39)]
        public double LastPlatformDepth { get; set; }

        [Key(40)]
        public PlayerAnimations Animations { get; set; }

        [Key(41)]
        public float LastAimDir { get; set; }

        [Key(42)]
        public int Cling { get; set; }

        [Key(43)]
        public bool HasSpeedBoots { get; set; }

        [Key(44)]
        public bool IsInvisible { get; set; }

        [Key(45)]
        public bool ShouldAutoBounce { get; set; }

    }
}
