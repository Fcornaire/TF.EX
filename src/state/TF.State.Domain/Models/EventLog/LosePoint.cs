using MessagePack;

namespace TF.State.Domain.Models.EventLog
{
    [MessagePackObject]
    public class LosePoint
    {
        [Key(0)]
        public int ScoreIndex { get; set; }
    }
}
