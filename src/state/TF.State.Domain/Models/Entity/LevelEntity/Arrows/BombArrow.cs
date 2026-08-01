using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class BombArrow : Arrow
    {
        [Key(23)]
        public bool CanExplode { get; set; }

        [Key(24)]
        public Alarm ExplodeAlarm { get; set; }

        [Key(25)]
        public Sprite<int> NormalSprite { get; set; }

        [Key(26)]
        public Sprite<int> BuriedSprite { get; set; }

    }
}
