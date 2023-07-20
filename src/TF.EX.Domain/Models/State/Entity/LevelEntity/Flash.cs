namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public class Flash
    {
        public bool IsFlashing { get; set; }
        public float FlashCounter { get; set; }
        public float FlashInterval { get; set; }


        public Flash(bool is_flashing, float flash_counter, float flash_interval)
        {
            IsFlashing = is_flashing;
            FlashCounter = flash_counter;
            FlashInterval = flash_interval;
        }
    }
}
