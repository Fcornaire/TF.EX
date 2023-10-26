using MessagePack;

namespace TF.EX.Domain.Models.State.Layer
{

    [MessagePackObject]
    public class Layer
    {
        [Key(0)]
        public IEnumerable<BackgroundElement> BackgroundElements { get; set; } = Enumerable.Empty<BackgroundElement>();
        [Key(1)]
        public IEnumerable<ForegroundElement> ForegroundElements { get; set; } = Enumerable.Empty<ForegroundElement>();

        [Key(2)]
        public float LightingLayerSine { get; set; }
        [Key(3)]
        public Dictionary<int, double> GameplayLayerActualDepthLookup { get; set; } = new Dictionary<int, double>();

    }
}
