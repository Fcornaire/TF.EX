using MessagePack;

namespace TF.EX.Domain.Models.State.Layer
{

    [MessagePackObject]
    public class Layer
    {
        [Key(0)]
        public IEnumerable<BGElement> BackgroundElements { get; set; } = Enumerable.Empty<BGElement>();
        [Key(1)]
        public IEnumerable<BGElement> ForegroundElements { get; set; } = Enumerable.Empty<BGElement>();

        [Key(2)]
        public float LightingLayerSine { get; set; }
        [Key(3)]
        public Dictionary<int, double> GameplayLayerActualDepthLookup { get; set; } = new Dictionary<int, double>();

    }
}
