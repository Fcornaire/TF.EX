using MessagePack;

namespace TF.EX.Domain.Models.State.Component
{
    [MessagePackObject]
    public class FireControl
    {
        [Key(0)]
        public Counter Counter { get; set; }
    }
}
