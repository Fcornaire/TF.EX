using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class BombArrow : Arrow
    {
        [Key(21)]
        public bool CanExplode { get; set; }

        [Key(22)]
        public Alarm ExplodeAlarm { get; set; }

        [Key(23)]
        public Sprite<int> NormalSprite { get; set; }

        [Key(24)]
        public Sprite<int> BuriedSprite { get; set; }

    }
}
