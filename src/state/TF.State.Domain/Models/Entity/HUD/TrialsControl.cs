using MessagePack;

namespace TF.State.Domain.Models.Entity.HUD
{
    [MessagePackObject]
    public class TrialsControl
    {
        [Key(0)]
        public long Time { get; set; }

        [Key(1)]
        public bool Started { get; set; }

        [Key(2)]
        public bool CanEnd { get; set; }

        [Key(3)]
        public int Drawing { get; set; }
    }
}
