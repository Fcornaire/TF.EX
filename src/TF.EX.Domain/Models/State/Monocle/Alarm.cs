namespace TF.EX.Domain.Models.State.Monocle
{
    //TODO: refacto other classes to use this one
    public class Alarm
    {
        public int Duration { get; set; }
        public float FramesLeft { get; set; }

        public bool Active { get; set; }
    }
}
