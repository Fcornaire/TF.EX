using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class RoundLogic
    {
        [Key(0)]
        public bool WasFinalKill { get; set; } = false;
        [Key(1)]
        public ICollection<double> SpotlightDephts { get; set; } = new List<double>();

        [Key(2)]
        public long Time { get; set; } = 0;

        [Key(3)]
        public EventLog.EventLog EventLogs { get; set; } = new EventLog.EventLog();

        [Key(4)]
        public RoundLevels RoundLevels { get; set; } = new RoundLevels();
    }
}
