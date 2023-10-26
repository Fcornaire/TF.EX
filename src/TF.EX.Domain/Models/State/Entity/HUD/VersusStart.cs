using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.HUD
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
