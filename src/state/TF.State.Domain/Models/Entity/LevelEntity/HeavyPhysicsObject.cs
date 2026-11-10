using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class HeavyPhysicsObject
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public Vector2f PositionCounter { get; set; }

        [Key(3)]
        public Vector2f Speed { get; set; }

        [Key(4)]
        public float Spin { get; set; }

        [Key(5)]
        public float Rotation { get; set; }

        [Key(6)]
        public Alarm FadeAlarm { get; set; }

        [Key(7)]
        public bool IsCollidable { get; set; }

        [Key(8)]
        public bool Pending { get; set; }

        [Key(9)]
        public Tween FadeTween { get; set; }
    }
}
