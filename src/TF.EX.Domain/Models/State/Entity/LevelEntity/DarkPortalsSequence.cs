using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
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
