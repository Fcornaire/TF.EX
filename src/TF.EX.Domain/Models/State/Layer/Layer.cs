namespace TF.EX.Domain.Models.State.Layer
{
    public class Layer
    {
        public List<BackgroundElement> BackgroundElements { get; set; }
        public List<ForegroundElement> ForegroundElements { get; set; }

        public float LightingLayerSine { get; set; }
        public Dictionary<int, double> GameplayLayerActualDepthLookup { get; set; } = new Dictionary<int, double>();

    }
}
