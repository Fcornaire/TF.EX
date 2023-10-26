using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class KillStats
    {
        [Key(0)]
        public ulong Kills { get; set; }

        [Key(1)]
        public ulong TeamKills { get; set; }

        [Key(2)]
        public ulong SelfKills { get; set; }

        [Key(3)]
        public ulong Environment { get; set; }

        [Key(4)]
        public ulong Green { get; set; }

        [Key(5)]
        public ulong Blue { get; set; }

        [Key(6)]
        public ulong Pink { get; set; }

        [Key(7)]
        public ulong Orange { get; set; }

        [Key(8)]
        public ulong White { get; set; }

        [Key(9)]
        public ulong Yellow { get; set; }

        [Key(10)]
        public ulong Cyan { get; set; }

        [Key(11)]
        public ulong Purple { get; set; }

        [Key(12)]
        public ulong Red { get; set; }

        [Key(13)]
        public ulong ArrowKills { get; set; }

        [Key(14)]
        public ulong ShockKills { get; set; }

        [Key(15)]
        public ulong ExplosionKills { get; set; }

        [Key(16)]
        public ulong BramblesKills { get; set; }

        [Key(17)]
        public ulong JumpedOnKills { get; set; }

        [Key(18)]
        public ulong LavaKills { get; set; }

        [Key(19)]
        public ulong SpikeBallKills { get; set; }

        [Key(20)]
        public ulong OrbKills { get; set; }

        [Key(21)]
        public ulong SquishKills { get; set; }

        [Key(22)]
        public ulong CursedBowKills { get; set; }

        [Key(23)]
        public ulong MiasmaKills { get; set; }

        [Key(24)]
        public ulong EnemyKills { get; set; }

        [Key(25)]
        public ulong ChaliceKills { get; set; }
    }
}
