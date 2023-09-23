namespace TF.EX.Domain.Models.State
{
    public struct MatchStats
    {
        public bool Won;

        public bool GotWin;

        public int PointsFromGoal;

        public uint SpamShots;

        public uint ArrowsShot;

        public uint ArrowsCollected;

        public uint ArrowsCaught;

        public uint OwnArrowsCaught;

        public uint ShieldsBroken;

        public uint TreasuresTaken;

        public uint Dodges;

        public uint Jumps;

        public uint CrownSpawns;

        public uint FailedShots;

        public uint ExplosionsSurvived;

        public uint DeathsDuringDodgeCooldown;

        public uint HatsShotOff;

        public uint LedgeGrabbedKills;

        public uint WinnerKills;

        public uint BombMultiKill;

        public uint ScreenWraps;

        public uint ArrowScreenWraps;

        public uint KillsWhileDead;

        public uint CrownsPickedUp;

        public uint HatsPickedUp;

        public uint ArrowsStolen;

        public uint RoundSweeps;

        public uint FurthestBehind;

        public uint MostBouncesLaserKill;

        public uint DrillHits;

        public uint DodgesTooLate;

        public uint GracePeriodCatches;

        public uint SelfLaserKills;

        public long FastestKill;

        public uint MostBoltTurns;

        public uint DodgeStomps;

        public uint TeamArrowCatches;

        public uint KillsAsGhost;

        public uint GhostKills;

        public uint SurvivedWithNoKills;

        public uint Revives;

        public uint TriggerBombKills;

        public uint TriggerBombsLost;

        public uint DroppedArrowKills;

        public uint HyperArrowKills;

        public uint HyperDeaths;

        public uint HyperSelfKills;

        public uint HyperStomps;

        public uint BombsDefused;

        public uint EnemyInPrismKills;

        public uint SelfInPrismKills;

        public uint EnemyInTeamPrismKills;

        public uint BombTrapKills;

        public uint CrushOthersKills;

        public uint KillsDuringMiasma;

        public uint DuckDances;

        public float LongestShot;

        public float FastFallFrames;

        public float DuckFrames;

        public float LedgeFrames;

        public float FramesAlive;

        public KillStats Kills;

        public KillStats Deaths;
    }
}
