using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class TeamReviver
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public double ActualDepth { get; set; }

        [Key(2)]
        public double CorpseActualDepth { get; set; }

        [Key(3)]
        public int Mode { get; set; }

        [Key(4)]
        public bool IsReviving { get; set; }

        [Key(5)]
        public float ReviveCounter { get; set; }

        [Key(6)]
        public bool CanRevive { get; set; }

        [Key(7)]
        public int Reviver { get; set; }

        [Key(8)]
        public bool Finished { get; set; }

        [Key(9)]
        public bool LevitateCorpse { get; set; }

        [Key(10)]
        public bool PlayerCanRevive { get; set; }

        [Key(11)]
        public bool AutoRevive { get; set; }

        [Key(12)]
        public bool IsRevivingHitbox { get; set; }

        [Key(13)]
        public Component.Alarm CanReviveAlarm { get; set; }

        [Key(14)]
        public Vector2f TargetPosition { get; set; }

        [Key(15)]
        public Component.SineWave Sine { get; set; }

        [Key(16)]
        public float ReviveSequenceCounter { get; set; }
    }
}
