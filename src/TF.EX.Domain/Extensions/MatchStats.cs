using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Extensions
{
    public static class MatchStatsExtensions
    {
        public static TowerFall.MatchStats ToTF(this MatchStats matchStats)
        {
            return new TowerFall.MatchStats
            {
                Won = matchStats.Won,
                GotWin = matchStats.GotWin,

                PointsFromGoal = matchStats.PointsFromGoal,

                SpamShots = matchStats.SpamShots,

                ArrowsShot = matchStats.ArrowsShot,

                ArrowsCollected = matchStats.ArrowsCollected,

                ArrowsCaught = matchStats.ArrowsCaught,

                OwnArrowsCaught = matchStats.OwnArrowsCaught,

                ShieldsBroken = matchStats.ShieldsBroken,

                TreasuresTaken = matchStats.TreasuresTaken,

                Dodges = matchStats.Dodges,

                Jumps = matchStats.Jumps,

                CrownSpawns = matchStats.CrownSpawns,

                FailedShots = matchStats.FailedShots,

                ExplosionsSurvived = matchStats.ExplosionsSurvived,

                DeathsDuringDodgeCooldown = matchStats.DeathsDuringDodgeCooldown,

                HatsShotOff = matchStats.HatsShotOff,

                LedgeGrabbedKills = matchStats.LedgeGrabbedKills,

                WinnerKills = matchStats.WinnerKills,

                BombMultiKill = matchStats.BombMultiKill,

                ScreenWraps = matchStats.ScreenWraps,

                ArrowScreenWraps = matchStats.ArrowScreenWraps,

                KillsWhileDead = matchStats.KillsWhileDead,

                CrownsPickedUp = matchStats.CrownsPickedUp,

                HatsPickedUp = matchStats.HatsPickedUp,

                ArrowsStolen = matchStats.ArrowsStolen,

                RoundSweeps = matchStats.RoundSweeps,

                FurthestBehind = matchStats.FurthestBehind,

                MostBouncesLaserKill = matchStats.MostBouncesLaserKill,

                DrillHits = matchStats.DrillHits,

                DodgesTooLate = matchStats.DodgesTooLate,

                GracePeriodCatches = matchStats.GracePeriodCatches,

                SelfLaserKills = matchStats.SelfLaserKills,

                FastestKill = matchStats.FastestKill,

                MostBoltTurns = matchStats.MostBoltTurns,

                DodgeStomps = matchStats.DodgeStomps,

                TeamArrowCatches = matchStats.TeamArrowCatches,

                KillsAsGhost = matchStats.KillsAsGhost,

                GhostKills = matchStats.GhostKills,

                SurvivedWithNoKills = matchStats.SurvivedWithNoKills,

                Revives = matchStats.Revives,

                TriggerBombKills = matchStats.TriggerBombKills,

                TriggerBombsLost = matchStats.TriggerBombsLost,

                DroppedArrowKills = matchStats.DroppedArrowKills,

                HyperArrowKills = matchStats.HyperArrowKills,

                HyperDeaths = matchStats.HyperDeaths,

                HyperSelfKills = matchStats.HyperSelfKills,

                HyperStomps = matchStats.HyperStomps,

                BombsDefused = matchStats.BombsDefused,

                EnemyInPrismKills = matchStats.EnemyInPrismKills,

                SelfInPrismKills = matchStats.SelfInPrismKills,

                EnemyInTeamPrismKills = matchStats.EnemyInTeamPrismKills,

                BombTrapKills = matchStats.BombTrapKills,

                CrushOthersKills = matchStats.CrushOthersKills,

                KillsDuringMiasma = matchStats.KillsDuringMiasma,

                DuckDances = matchStats.DuckDances,

                LongestShot = matchStats.LongestShot,

                FastFallFrames = matchStats.FastFallFrames,

                DuckFrames = matchStats.DuckFrames,

                LedgeFrames = matchStats.LedgeFrames,

                FramesAlive = matchStats.FramesAlive,

                Kills = matchStats.Kills.ToTF(),

                Deaths = matchStats.Deaths.ToTF()
            };
        }

        //Extension method to convert a towerfall MatchStats object to a MatchStats object
        public static MatchStats ToDomain(this TowerFall.MatchStats matchStats)
        {
            return new MatchStats
            {
                Won = matchStats.Won,
                GotWin = matchStats.GotWin,

                PointsFromGoal = matchStats.PointsFromGoal,

                SpamShots = matchStats.SpamShots,

                ArrowsShot = matchStats.ArrowsShot,

                ArrowsCollected = matchStats.ArrowsCollected,

                ArrowsCaught = matchStats.ArrowsCaught,

                OwnArrowsCaught = matchStats.OwnArrowsCaught,

                ShieldsBroken = matchStats.ShieldsBroken,

                TreasuresTaken = matchStats.TreasuresTaken,

                Dodges = matchStats.Dodges,

                Jumps = matchStats.Jumps,

                CrownSpawns = matchStats.CrownSpawns,

                FailedShots = matchStats.FailedShots,

                ExplosionsSurvived = matchStats.ExplosionsSurvived,

                DeathsDuringDodgeCooldown = matchStats.DeathsDuringDodgeCooldown,

                HatsShotOff = matchStats.HatsShotOff,

                LedgeGrabbedKills = matchStats.LedgeGrabbedKills,

                WinnerKills = matchStats.WinnerKills,

                BombMultiKill = matchStats.BombMultiKill,

                ScreenWraps = matchStats.ScreenWraps,

                ArrowScreenWraps = matchStats.ArrowScreenWraps,

                KillsWhileDead = matchStats.KillsWhileDead,

                CrownsPickedUp = matchStats.CrownsPickedUp,

                HatsPickedUp = matchStats.HatsPickedUp,

                ArrowsStolen = matchStats.ArrowsStolen,

                RoundSweeps = matchStats.RoundSweeps,

                FurthestBehind = matchStats.FurthestBehind,

                MostBouncesLaserKill = matchStats.MostBouncesLaserKill,

                DrillHits = matchStats.DrillHits,

                DodgesTooLate = matchStats.DodgesTooLate,

                GracePeriodCatches = matchStats.GracePeriodCatches,

                SelfLaserKills = matchStats.SelfLaserKills,

                FastestKill = matchStats.FastestKill,

                MostBoltTurns = matchStats.MostBoltTurns,

                DodgeStomps = matchStats.DodgeStomps,

                TeamArrowCatches = matchStats.TeamArrowCatches,

                KillsAsGhost = matchStats.KillsAsGhost,

                GhostKills = matchStats.GhostKills,

                SurvivedWithNoKills = matchStats.SurvivedWithNoKills,

                Revives = matchStats.Revives,

                TriggerBombKills = matchStats.TriggerBombKills,

                TriggerBombsLost = matchStats.TriggerBombsLost,

                DroppedArrowKills = matchStats.DroppedArrowKills,

                HyperArrowKills = matchStats.HyperArrowKills,

                HyperDeaths = matchStats.HyperDeaths,

                HyperSelfKills = matchStats.HyperSelfKills,

                HyperStomps = matchStats.HyperStomps,

                BombsDefused = matchStats.BombsDefused,

                EnemyInPrismKills = matchStats.EnemyInPrismKills,

                SelfInPrismKills = matchStats.SelfInPrismKills,

                EnemyInTeamPrismKills = matchStats.EnemyInTeamPrismKills,

                BombTrapKills = matchStats.BombTrapKills,

                CrushOthersKills = matchStats.CrushOthersKills,

                KillsDuringMiasma = matchStats.KillsDuringMiasma,

                DuckDances = matchStats.DuckDances,

                LongestShot = matchStats.LongestShot,

                FastFallFrames = matchStats.FastFallFrames,

                DuckFrames = matchStats.DuckFrames,

                LedgeFrames = matchStats.LedgeFrames,

                FramesAlive = matchStats.FramesAlive,

                Kills = matchStats.Kills.ToDomain(),

                Deaths = matchStats.Deaths.ToDomain()
            };
        }
    }
}
