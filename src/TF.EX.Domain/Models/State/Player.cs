namespace TF.EX.Domain.Models.State
{
    public class Player
    {
        public bool IsCollidable { get; set; }
        public bool IsDead { get; set; }
        public Vector2f Position { get; set; }
        public Vector2f Position_counter { get; set; }
        public int Facing { get; set; }
        public PlayerArrowsInventory ArrowsInventory { get; set; }
        public float WallStickMax { get; set; }
        public Vector2f Speed { get; set; }
        public float FlapGravity { get; set; }
        public bool CanHyper { get; set; }
        public PlayerState State { get; set; }
        public float JumpBufferCounter { get; set; }
        public float DodgeEndCounter { get; set; }
        public float DodgeStallCounter { get; set; }
        public float JumpGraceCounter { get; set; }
        public float DodgeCatchCounter { get; set; }
        public float DyingCounter { get; set; }
        public float FlapBounceCounter { get; set; }
        public DodgeSlide DodgeSlide { get; set; }
        public bool DodgeCooldown { get; set; }
        public Scheduler Scheduler { get; set; }
        public Hitbox Hitbox { get; set; }
        public int AutoMove { get; set; }
        public bool Aiming { get; set; }
        public bool CanVarJump { get; set; }
        public bool IsOnGround { get; set; }
        public float DuckSlipCounter { get; set; } //Not a 'Counter'
        public bool IsShieldVisible { get; set; }
        public bool IsWingsVisible { get; set; }
        public Flash Flash { get; set; }
        public double DeathArrowDepth { get; set; }
        public int Index { get; set; }
        public bool ShouldDrawSelf { get; set; }
        public bool ShouldStartAimingDown { get; set; }
        public int GraceLedgeDir { get; set; }
        public double ActualDepth { get; set; }
        public bool MarkedForRemoval { get; set; }
        public double LastPlatformDepth { get; set; }
    }
}
