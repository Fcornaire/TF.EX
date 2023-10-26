using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.HUD
{
    [MessagePackObject]
    public class HUD
    {
        [Key(0)]
        public VersusStart VersusStart { get; set; } = new VersusStart();

        [Key(1)]
        public VersusRoundResults VersusRoundResults { get; set; } = new VersusRoundResults();
    }
}
