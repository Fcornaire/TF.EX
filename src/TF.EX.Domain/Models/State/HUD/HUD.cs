namespace TF.EX.Domain.Models.State.HUD
{
    public class HUD
    {
        public VersusStart VersusStart { get; set; } = new VersusStart();

        public VersusRoundResults VersusRoundResults { get; set; } = new VersusRoundResults();
    }
}
