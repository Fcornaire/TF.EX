namespace TF.EX.Domain.Models.State
{
    public class Flash
    {
        public bool IsFlashing { get; set; }
        public float FlashCounter { get; set; }
        public float FlashInterval { get; set; }


        public Flash(bool is_flashing, float flash_counter, float flash_interval)
        {
            this.IsFlashing = is_flashing;
            this.FlashCounter = flash_counter;
            this.FlashInterval = flash_interval;
        }
    }
}
