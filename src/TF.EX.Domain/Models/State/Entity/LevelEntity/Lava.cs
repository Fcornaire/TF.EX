namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public class Lava
    {
        public LavaSide Side { get; set; }
        public bool IsCollidable { get; set; }
        public Vector2f Position { get; set; }
        public float Percent { get; set; }
        public float SineCounter { get; set; }
    }
}
