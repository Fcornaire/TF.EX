namespace TF.EX.Domain.Models.State.LevelEntity.Chest
{
    public class Chest
    {
        public int CurrentAnimId { get; set; }
        public bool IsCollidable { get; set; }
        public float AppearCounter { get; set; }
        public PickupState Pickups { get; set; }
        public Vector2f Position { get; set; }
        public Vector2f PositionCounter { get; set; }
        public float VSpeed { get; set; }
        public ChestState State { get; set; }
        public float AppearTimer { get; set; }
        public bool IsLightVisible { get; set; }
        public float OpeningTimer { get; set; }
        public double ActualDepth { get; set; }

        public static Chest Empty()
        {
            return new Chest
            {
                CurrentAnimId = 0,
                Position = new Vector2f { x = -1, y = -1 },
                AppearCounter = 0,
                PositionCounter = new Vector2f { x = -1, y = -1 },
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
