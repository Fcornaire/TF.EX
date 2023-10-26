using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Chest
{
    [MessagePackObject]
    public class Chest
    {
        [Key(0)]
        public int CurrentAnimId { get; set; }

        [Key(1)]
        public bool IsCollidable { get; set; }

        [Key(2)]
        public float AppearCounter { get; set; }

        [Key(3)]
        public PickupState Pickups { get; set; }

        [Key(4)]
        public Vector2f Position { get; set; }

        [Key(5)]
        public Vector2f PositionCounter { get; set; }

        [Key(6)]
        public float VSpeed { get; set; }

        [Key(7)]
        public ChestState State { get; set; }

        [Key(8)]
        public float AppearTimer { get; set; }

        [Key(9)]
        public bool IsLightVisible { get; set; }

        [Key(10)]
        public float OpeningTimer { get; set; }

        [Key(11)]
        public double ActualDepth { get; set; }

        public static Chest Empty()
        {
            return new Chest
            {
                CurrentAnimId = 0,
                Position = new Vector2f { X = -1, Y = -1 },
                AppearCounter = 0,
                PositionCounter = new Vector2f { X = -1, Y = -1 },
                State = ChestState.WaitingToAppear,
                VSpeed = 0f,
                AppearTimer = -1,
                Pickups = PickupState.Arrows,
                IsCollidable = false,
                IsLightVisible = false,
                OpeningTimer = -1f,
            };
        }
    }
}
