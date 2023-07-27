namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public class Orb
    {
        public Vector2f Position { get; set; }
        public Vector2f PositionCounter { get; set; }
        public float VSpeed { get; set; }
        public float SineCounter { get; set; }
        public double ActualDepth { get; set; }
        public bool IsCollidable { get; set; }
        public bool IsFalling { get; set; }

        public int OwnerIndex { get; set; }
    }
}
