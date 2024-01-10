using MessagePack;

namespace TF.EX.Domain.Models.State.Layer
{
    [MessagePackObject]
    public class BGElement
    {
        [Key(0)]
        public int Index { get; set; }
        [Key(1)]
        public Vector2f ScrollLayer_Position { get; set; }
        [Key(2)]
        public float WavyLayer_Counter { get; set; }
        [Key(3)]
        public Sprite<int> FadeLayer_Sprite { get; set; }
        [Key(4)]
        public float FadeLayer_SineCounter { get; set; }
    }
}
