namespace TF.EX.Domain.Models.State.Layer
{
    public class Layer
    {
        public IEnumerable<BackgroundElement> BackgroundElements { get; set; } = Enumerable.Empty<BackgroundElement>();
        public IEnumerable<ForegroundElement> ForegroundElements { get; set; } = Enumerable.Empty<ForegroundElement>();

        public float LightingLayerSine { get; set; }
        public Dictionary<int, double> GameplayLayerActualDepthLookup { get; set; } = new Dictionary<int, double>();

    }
}
