using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.HUD
{
    [MessagePackObject]
    public class VersusRoundResults
    {
        [Key(0)]
        public int CoroutineState { get; set; } = 0;
    }
}
