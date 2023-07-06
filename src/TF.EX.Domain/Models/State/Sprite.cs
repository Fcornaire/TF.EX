namespace TF.EX.Domain.Models.State
{
    public class Sprite<T>
    {
        public T CurrentAnimID { get; set; }

        public int CurrentFrame { get; set; }

        public int AnimationFrame { get; set; }

        public float Timer { get; set; }

        public bool Finished { get; set; }

        public bool Playing { get; set; }
    }
}
