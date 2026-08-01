using MessagePack;

namespace TF.State.Domain.Models.Entity.HUD
{
    [MessagePackObject]
    public class VersusStart
    {
        [Key(0)]
        public int CoroutineState { get; set; } = 0;
        [Key(1)]
        public int TweenState { get; set; } = 0;
    }
}
