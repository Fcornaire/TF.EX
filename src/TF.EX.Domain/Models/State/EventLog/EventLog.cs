namespace TF.EX.Domain.Models.State.EventLog
{
    public class EventLog
    {
        public ICollection<GainPoint> GainPoints { get; set; } = new List<GainPoint>();
        public ICollection<LosePoint> LosePoints { get; set; } = new List<LosePoint>();
        public ICollection<CrownChange> CrownChanges { get; set; } = new List<CrownChange>();
    }
}
