using MessagePack;

namespace TF.EX.Domain.Models.State.Component
{
    //TODO: refacto other classes to use this one
    [MessagePackObject]
    public class Alarm
    {
        [Key(0)]
        public int Duration { get; set; }
        [Key(1)]
        public float FramesLeft { get; set; }

        [Key(2)]
        public bool Active { get; set; }
    }
}
