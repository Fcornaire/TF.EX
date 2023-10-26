using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class Explosion
    {
        [Key(0)]
        public Vector2f Position { get; set; }
        [Key(1)]
        public double ActualDepth { get; set; }
        [Key(2)]
        public IEnumerable<Counter> Counters { get; set; }

        [Key(3)]
        public Alarm Alarm { get; set; }

        [Key(4)]
        public bool IsSuper { get; set; }

        [Key(5)]
        public int PlayerIndex { get; set; }

        [Key(6)]
        public uint Kills { get; set; }

        [Key(7)]
        public bool TriggerBomb { get; set; }

        [Key(8)]
        public bool BombTrap { get; set; }
    }
}
