using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class PrismArrow : Arrow
    {
        [Key(23)]
        public Sprite<int> NormalSprite { get; set; }

        [Key(24)]
        public Sprite<int> BuriedSprite { get; set; }
    }
}
