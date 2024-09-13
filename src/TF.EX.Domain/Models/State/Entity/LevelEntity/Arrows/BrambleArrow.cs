using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class BrambleArrow : Arrow
    {
        [Key(23)]
        public bool CanDie { get; set; }
        [Key(24)]
        public bool IsUsed { get; set; }
        [Key(25)]
        public int CoroutineTimer { get; set; }

    }
}
