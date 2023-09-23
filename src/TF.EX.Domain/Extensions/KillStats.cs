using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Extensions
{
    public static class KillStatsExtensions
    {
        public static TowerFall.KillStats ToTF(this KillStats killStats)
        {
            return new TowerFall.KillStats
            {
                Kills = killStats.Kills,
                TeamKills = killStats.TeamKills,
                SelfKills = killStats.SelfKills,
                Environment = killStats.Environment,
                Green = killStats.Green,
                Blue = killStats.Blue,
                Pink = killStats.Pink,
                Orange = killStats.Orange,
                White = killStats.White,
                Yellow = killStats.Yellow,
                Cyan = killStats.Cyan,
                Purple = killStats.Purple,
                Red = killStats.Red,
                ArrowKills = killStats.ArrowKills,
                ShockKills = killStats.ShockKills,
                ExplosionKills = killStats.ExplosionKills,
                BramblesKills = killStats.BramblesKills,
                JumpedOnKills = killStats.JumpedOnKills,
                LavaKills = killStats.LavaKills,
                SpikeBallKills = killStats.SpikeBallKills,
                OrbKills = killStats.OrbKills,
                SquishKills = killStats.SquishKills,
                CursedBowKills = killStats.CursedBowKills,
                MiasmaKills = killStats.MiasmaKills,
                EnemyKills = killStats.EnemyKills,
                ChaliceKills = killStats.ChaliceKills
            };
        }

        public static KillStats ToDomain(this TowerFall.KillStats killStats)
        {
            return new KillStats
            {
                Kills = killStats.Kills,
                TeamKills = killStats.TeamKills,
                SelfKills = killStats.SelfKills,
                Environment = killStats.Environment,
                Green = killStats.Green,
                Blue = killStats.Blue,
                Pink = killStats.Pink,
                Orange = killStats.Orange,
                White = killStats.White,
                Yellow = killStats.Yellow,
                Cyan = killStats.Cyan,
                Purple = killStats.Purple,
                Red = killStats.Red,
                ArrowKills = killStats.ArrowKills,
                ShockKills = killStats.ShockKills,
                ExplosionKills = killStats.ExplosionKills,
                BramblesKills = killStats.BramblesKills,
                JumpedOnKills = killStats.JumpedOnKills,
                LavaKills = killStats.LavaKills,
                SpikeBallKills = killStats.SpikeBallKills,
                OrbKills = killStats.OrbKills,
                SquishKills = killStats.SquishKills,
                CursedBowKills = killStats.CursedBowKills,
                MiasmaKills = killStats.MiasmaKills,
                EnemyKills = killStats.EnemyKills,
                ChaliceKills = killStats.ChaliceKills
            };
        }

    }
}
