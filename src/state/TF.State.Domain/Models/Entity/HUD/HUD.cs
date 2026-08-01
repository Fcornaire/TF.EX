using MessagePack;

namespace TF.State.Domain.Models.Entity.HUD
{
    [MessagePackObject]
    public class HUD
    {
        [Key(0)]
        public VersusStart VersusStart { get; set; } = new VersusStart();

        [Key(1)]
        public VersusRoundResults VersusRoundResults { get; set; } = new VersusRoundResults();

        [Key(2)]
        public TrialsControl TrialsControl { get; set; }
    }
}
