using Monocle;

namespace TF.EX.TowerFallExtensions
{
    public static class LayerExtensions
    {
        public static bool IsGameplayLayer(this Layer layer)
        {
            return layer.Index == 0;
        }
    }
}
