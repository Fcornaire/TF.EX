using MessagePack;

namespace TF.EX.Domain.Models.State.EventLog
{
    [MessagePackObject]
    public class CrownChange
    {
        [Key(0)]
        public bool[] PlayerWithCrown { get; set; }
    }
}
