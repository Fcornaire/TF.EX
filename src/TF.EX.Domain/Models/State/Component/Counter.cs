using MessagePack;

namespace TF.EX.Domain.Models.State.Component
{
    //TODO: refacto other classes to use this one
    [MessagePackObject]
    public class Counter
    {
        [Key(0)]
        public float CounterValue { get; set; }

    }
}
