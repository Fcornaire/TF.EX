namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
{
    public class DodgeSlide
    {
        public bool IsDodgeSliding { get; set; }
        public bool WasDodgeSliding { get; set; }

        public DodgeSlide(bool current, bool was)
        {
            IsDodgeSliding = current;
            WasDodgeSliding = was;
        }
    }
}
