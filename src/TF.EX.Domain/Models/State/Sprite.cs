using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class Sprite<T>
    {
        [Key(0)]
        public T CurrentAnimID { get; set; }

        [Key(1)]
        public int CurrentFrame { get; set; }

        [Key(2)]
        public int AnimationFrame { get; set; }

        [Key(3)]
        public float Timer { get; set; }

        [Key(4)]
        public bool Finished { get; set; }

        [Key(5)]
        public bool Playing { get; set; }

        [Key(6)]
        public Vector2f Scale { get; set; }

        [Key(7)]
        public float Rate { get; set; }

        [Key(8)]
        public float Rotation { get; set; }
    }
}
