using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Chest
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

        [Key(12)]
        public IEnumerable<PickupState> PickupList { get; set; }

        [Key(13)]
        public int Type { get; set; }

        [Key(14)]
        public float BottomlessCounter { get; set; }

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
                PickupList = new List<PickupState>(),
                Type = 0,
                IsCollidable = false,
                IsLightVisible = false,
                OpeningTimer = -1f,
            };
        }
    }
}
