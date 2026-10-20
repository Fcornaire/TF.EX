using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class TriggerArrow : Arrow
    {
        [Key(23)]
        public Sprite<int> NormalSprite { get; set; }

        [Key(24)]
        public Alarm PrimedAlarm { get; set; }

        [Key(25)]
        public bool IsLightVisible { get; set; }
    }
}
