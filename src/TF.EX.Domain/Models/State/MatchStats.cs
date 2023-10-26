using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]

    public struct MatchStats
    {
        [Key(0)]
        public bool Won { get; set; }
        [Key(1)]
        public bool GotWin { get; set; }
        [Key(2)]
        public int PointsFromGoal { get; set; }
        [Key(3)]
        public uint SpamShots { get; set; }
        [Key(4)]
        public uint ArrowsShot { get; set; }
        [Key(5)]
        public uint ArrowsCollected { get; set; }
        [Key(6)]
        public uint ArrowsCaught { get; set; }
        [Key(7)]
        public uint OwnArrowsCaught { get; set; }
        [Key(8)]
        public uint ShieldsBroken { get; set; }
        [Key(9)]
        public uint TreasuresTaken { get; set; }
        [Key(10)]
        public uint Dodges { get; set; }
        [Key(11)]
        public uint Jumps { get; set; }
        [Key(12)]
        public uint CrownSpawns { get; set; }
        [Key(13)]
        public uint FailedShots { get; set; }
        [Key(14)]
        public uint ExplosionsSurvived { get; set; }
        [Key(15)]
        public uint DeathsDuringDodgeCooldown { get; set; }
        [Key(16)]
        public uint HatsShotOff { get; set; }
        [Key(17)]
        public uint LedgeGrabbedKills { get; set; }
        [Key(18)]
        public uint WinnerKills { get; set; }
        [Key(19)]
        public uint BombMultiKill { get; set; }
        [Key(20)]
        public uint ScreenWraps { get; set; }
        [Key(21)]
        public uint ArrowScreenWraps { get; set; }
        [Key(22)]
        public uint KillsWhileDead { get; set; }
        [Key(23)]
        public uint CrownsPickedUp { get; set; }
        [Key(24)]
        public uint HatsPickedUp { get; set; }
        [Key(25)]
        public uint ArrowsStolen { get; set; }
        [Key(26)]
        public uint RoundSweeps { get; set; }
        [Key(27)]
        public uint FurthestBehind { get; set; }
        [Key(28)]
        public uint MostBouncesLaserKill { get; set; }
        [Key(29)]
        public uint DrillHits { get; set; }
        [Key(30)]
        public uint DodgesTooLate { get; set; }
        [Key(31)]
        public uint GracePeriodCatches { get; set; }
        [Key(32)]
        public uint SelfLaserKills { get; set; }
        [Key(33)]
        public long FastestKill { get; set; }
        [Key(34)]
        public uint MostBoltTurns { get; set; }
        [Key(35)]
        public uint DodgeStomps { get; set; }
        [Key(36)]
        public uint TeamArrowCatches { get; set; }
        [Key(37)]
        public uint KillsAsGhost { get; set; }
        [Key(38)]
        public uint GhostKills { get; set; }
        [Key(39)]
        public uint SurvivedWithNoKills { get; set; }
        [Key(40)]
        public uint Revives { get; set; }
        [Key(41)]
        public uint TriggerBombKills { get; set; }
        [Key(42)]
        public uint TriggerBombsLost { get; set; }
        [Key(43)]
        public uint DroppedArrowKills { get; set; }
        [Key(44)]
        public uint HyperArrowKills { get; set; }
        [Key(45)]
        public uint HyperDeaths { get; set; }
        [Key(46)]
        public uint HyperSelfKills { get; set; }
        [Key(47)]
        public uint HyperStomps { get; set; }
        [Key(48)]
        public uint BombsDefused { get; set; }
        [Key(49)]
        public uint EnemyInPrismKills { get; set; }
        [Key(50)]
        public uint SelfInPrismKills { get; set; }
        [Key(51)]
        public uint EnemyInTeamPrismKills { get; set; }
        [Key(52)]
        public uint BombTrapKills { get; set; }
        [Key(53)]
        public uint CrushOthersKills { get; set; }
        [Key(54)]
        public uint KillsDuringMiasma { get; set; }
        [Key(55)]
        public uint DuckDances { get; set; }
        [Key(56)]
        public float LongestShot { get; set; }
        [Key(57)]
        public float FastFallFrames { get; set; }
        [Key(58)]
        public float DuckFrames { get; set; }
        [Key(59)]
        public float LedgeFrames { get; set; }
        [Key(60)]
        public float FramesAlive { get; set; }
        [Key(61)]
        public KillStats Kills { get; set; }
        [Key(62)]
        public KillStats Deaths { get; set; }
    }
}
