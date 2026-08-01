using MessagePack;

namespace TF.State.Domain.Models.EventLog
{
    [MessagePackObject]
    public class CrownChange
    {
        [Key(0)]
        public bool[] PlayerWithCrown { get; set; }
    }
}
