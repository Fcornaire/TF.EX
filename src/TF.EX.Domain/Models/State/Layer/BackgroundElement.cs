using MessagePack;

namespace TF.EX.Domain.Models.State.Layer
{
    [MessagePackObject]
    public class BackgroundElement
    {
        [Key(0)]
        public int index { get; set; }
        [Key(1)]
        public Vector2f Position { get; set; }
    }
}
