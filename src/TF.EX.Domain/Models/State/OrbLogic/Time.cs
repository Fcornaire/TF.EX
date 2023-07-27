namespace TF.EX.Domain.Models.State.OrbLogic
{
    public class Time : IOrbLogic
    {
        public Counter Counter { get; set; }
        public float EngineTimeRate { get; set; }
        public float EngineTimeMult { get; set; }
        public float GameRateTarget { get; set; }
        public bool GameRateEased { get; set; }

        public static Time Default => new Time
        {
            Counter = Counter.Default,
            EngineTimeRate = 1f,
            EngineTimeMult = 1f,
            GameRateTarget = Constants.INITIAL_GAME_RATE_TARGET,
            GameRateEased = false
        };

        public bool IsDefault() => Counter.Start == Counter.Default.Start && Counter.End == Counter.Default.End;
    }
}
