using MessagePack;

namespace TF.State.Domain.Models.EventLog
{
    [MessagePackObject]
    public class GainPoint
    {
        [Key(0)]
        public int ScoreIndex { get; set; }
    }
}
