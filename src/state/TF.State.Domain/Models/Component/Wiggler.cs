using MessagePack;

namespace TF.State.Domain.Models.Component
{
    [MessagePackObject]
    public class Wiggler
    {
        [Key(0)]
        public float Counter { get; set; }

        [Key(1)]
        public float SineCounter { get; set; }

        [Key(2)]
        public float Value { get; set; }

        [Key(3)]
        public bool Active { get; set; }
    }
}
