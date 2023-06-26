using TowerFall;

namespace TF.EX.TowerFallExtensions.Scene
{
    public static class MainMenuExtensions
    {
        public static Monocle.Layer GetMainLayer(this MainMenu level)
        {
            return level.Layers.FirstOrDefault(l => l.Value.Index == 0).Value;
        }
    }
}
