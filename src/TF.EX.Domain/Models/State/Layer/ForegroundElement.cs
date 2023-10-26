using MessagePack;

namespace TF.EX.Domain.Models.State.Layer
{
    [MessagePackObject]
    public class ForegroundElement
    {
        [Key(0)]
        public int index { get; set; }

        [Key(1)]
        public float counter { get; set; }
    }
}
