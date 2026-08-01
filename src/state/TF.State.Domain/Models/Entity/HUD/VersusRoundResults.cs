using MessagePack;

namespace TF.State.Domain.Models.Entity.HUD
{
    [MessagePackObject]
    public class VersusRoundResults
    {
        [Key(0)]
        public int CoroutineState { get; set; } = 0;
    }
}
