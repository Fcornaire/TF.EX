using MessagePack;

namespace TF.EX.Domain.Models.State.EventLog
{
    [MessagePackObject]
    public class LosePoint
    {
        [Key(0)]
        public int ScoreIndex { get; set; }
    }
}
