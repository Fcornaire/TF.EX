using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class ChaliceGhost
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public Vector2f Speed { get; set; }

        [Key(3)]
        public float Lerp { get; set; }

        [Key(4)]
        public int OwnerIndex { get; set; }

        [Key(5)]
        public int Team { get; set; }

        [Key(6)]
        public int TargetIndex { get; set; }

        [Key(7)]
        public bool CanFindTarget { get; set; }

        [Key(8)]
        public bool Dead { get; set; }

        [Key(9)]
        public bool Spawned { get; set; }

        [Key(10)]
        public bool IsCollidable { get; set; }

        [Key(11)]
        public Sprite<string> Sprite { get; set; }

        [Key(12)]
        public Wiggler Wiggler { get; set; }

        [Key(13)]
        public int Phase { get; set; }

        [Key(14)]
        public float PhaseCounter { get; set; }

        [Key(15)]
        public float AttackCooldown { get; set; }
    }
}
