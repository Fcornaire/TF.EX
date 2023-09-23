namespace TF.EX.Domain.Models.State
{
    public class RoundLevels
    {
        public IEnumerable<string> Nexts { get; set; } = Enumerable.Empty<string>();

        public string Last { get; set; } = string.Empty;

    }
}
