using TF.EX.Domain.Models.State.Monocle;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public class Explosion
    {
        public Vector2f Position { get; set; }
        public double ActualDepth { get; set; }
        public IEnumerable<Counter> Counters { get; set; }


        public Alarm Alarm { get; set; }

        public bool IsSuper { get; set; }

        public int PlayerIndex { get; set; }

        public uint Kills { get; set; }

        public bool TriggerBomb { get; set; }

        public bool BombTrap { get; set; }
    }
}
