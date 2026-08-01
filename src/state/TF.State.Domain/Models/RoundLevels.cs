using MessagePack;

namespace TF.State.Domain.Models
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
