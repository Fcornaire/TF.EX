using static TowerFall.Lava;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public struct Lava
    {
        public LavaSide side;
        public bool is_collidable;
        public Vector2f position;
        public float percent;
        public float sine_counter;

    }
}
