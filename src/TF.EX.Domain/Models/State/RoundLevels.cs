namespace TF.EX.Domain.Models.State
{
    public class RoundLevels
    {
        public IEnumerable<string> Nexts = Enumerable.Empty<string>();

        public string Last { get; set; } = string.Empty;

    }
}
