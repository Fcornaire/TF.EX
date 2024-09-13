using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class Brambles
    {
        [Key(0)]
        public Vector2f Position { get; set; }
        [Key(1)]
        public Vector2f PositionCounter { get; set; }

        //Hitbox

        [Key(2)]
        public FireControl Fire { get; set; }
        [Key(3)]
        public double ActualDepth { get; set; }
        [Key(4)]
        public double RidingActualDepth { get; set; } = -1;
        [Key(5)]
        public bool HasSoundPlayed { get; set; }
        [Key(6)]
        public Alarm DelayAlarm { get; set; }
        [Key(7)]
        public Alarm DeathAlarm { get; set; }
        [Key(8)]
        public bool HasTweenedOut { get; set; }
        [Key(9)]
        public int OwnerIndex { get; set; }
    }
}
