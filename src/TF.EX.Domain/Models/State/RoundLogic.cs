namespace TF.EX.Domain.Models.State
{
    public class RoundLogic
    {
        public bool WasFinalKill { get; set; } = false;
        public ICollection<double> SpotlightDephts { get; set; } = new List<double>();

        public long Time { get; set; } = 0;

        public EventLog.EventLog EventLogs { get; set; } = new EventLog.EventLog();

        public RoundLevels RoundLevels { get; set; } = new RoundLevels();
    }
}
