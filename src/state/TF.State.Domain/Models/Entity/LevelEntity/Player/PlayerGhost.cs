using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class PlayerGhost
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public int PlayerIndex { get; set; }

        [Key(2)]
        public Vector2f Position { get; set; }

        [Key(3)]
        public Vector2f PositionCounter { get; set; }

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
        public float Sine { get; set; }

        [Key(10)]
        public float LastDir { get; set; }

        [Key(11)]
        public bool HasLastDir { get; set; }

        [Key(12)]
        public float MoveMax { get; set; }

        [Key(13)]
        public float DodgeCooldown { get; set; }

        [Key(14)]
        public float KillDelayCounter { get; set; }

        [Key(15)]
        public int Health { get; set; }

        [Key(16)]
        public bool IsCollidable { get; set; }

        [Key(17)]
        public bool Seek { get; set; }

        [Key(18)]
        public Sprite<string> Sprite { get; set; }

        [Key(19)]
        public double DespawnCorpseActualDepth { get; set; }

        [Key(20)]
        public Vector2f DespawnStart { get; set; }

        [Key(21)]
        public Vector2f PreviousPosition { get; set; }
    }
}
