using MessagePack;

namespace TF.EX.Domain.Models.State.EventLog
{
    [MessagePackObject]
    public class EventLog
    {
        [Key(0)]
        public ICollection<GainPoint> GainPoints { get; set; } = new List<GainPoint>();
        [Key(1)]
        public ICollection<LosePoint> LosePoints { get; set; } = new List<LosePoint>();
        [Key(2)]
        public ICollection<CrownChange> CrownChanges { get; set; } = new List<CrownChange>();
    }
}
