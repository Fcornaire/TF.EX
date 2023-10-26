using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class RoundLevels
    {
        [Key(0)]
        public IEnumerable<string> Nexts { get; set; } = Enumerable.Empty<string>();

        [Key(1)]
        public string Last { get; set; } = string.Empty;

    }
}
