using MessagePack;

namespace TF.State.Domain.Models.Component
{
    [MessagePackObject]
    public class FireControl
    {
        [Key(0)]
        public Counter Counter { get; set; }
    }
}
