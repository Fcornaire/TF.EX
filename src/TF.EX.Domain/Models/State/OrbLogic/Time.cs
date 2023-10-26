using MessagePack;

namespace TF.EX.Domain.Models.State.OrbLogic
{
    [MessagePackObject]
    public class Time : IOrbLogic
    {
        [Key(0)]
        public CounterOrb Counter { get; set; }
        [Key(1)]
        public float EngineTimeRate { get; set; }
        [Key(2)]
        public float EngineTimeMult { get; set; }
        [Key(3)]
        public float GameRateTarget { get; set; }
        [Key(4)]
        public bool GameRateEased { get; set; }

        public static Time Default => new Time
        {
            Counter = CounterOrb.Default,
            EngineTimeRate = 1f,
            EngineTimeMult = 1f,
            GameRateTarget = Constants.INITIAL_GAME_RATE_TARGET,
            GameRateEased = false
        };

        public bool IsDefault() => Counter.Start == CounterOrb.Default.Start && Counter.End == CounterOrb.Default.End;
    }
}
