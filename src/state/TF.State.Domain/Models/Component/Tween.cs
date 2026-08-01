using MessagePack;

namespace TF.State.Domain.Models.Component
{
    [MessagePackObject]
    public class Tween
    {
        [Key(0)]
        public int Duration { get; set; }

        [Key(1)]
        public float FramesLeft { get; set; }

        [Key(2)]
        public bool Active { get; set; }
    }
}
