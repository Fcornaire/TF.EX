namespace TF.EX.Domain.Models.State
{
    public class RoundLogic
    {
        public bool WasFinalKill { get; set; } = false;
        public ICollection<double> SpotlightDephts { get; set; } = new List<double>();

        public long Time { get; set; } = 0;
    }
}
