namespace TF.EX.Domain.Models.State.Monocle
{
    public class Alarm
    {
        public int Duration { get; set; }
        public float FramesLeft { get; set; }

        public bool Active { get; set; }
    }
}
