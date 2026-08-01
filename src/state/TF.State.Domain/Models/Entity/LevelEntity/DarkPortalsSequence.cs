using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class DarkPortalsSequence
    {
        [Key(0)]
        public int Phase { get; set; }

        [Key(1)]
        public float Counter { get; set; }
    }
}
