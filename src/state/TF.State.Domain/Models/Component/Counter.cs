using MessagePack;

namespace TF.State.Domain.Models.Component
{
    //TODO: refacto other classes to use this one
    [MessagePackObject]
    public class Counter
    {
        [Key(0)]
        public float CounterValue { get; set; }

    }
}
