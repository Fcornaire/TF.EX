using TowerFall;

namespace TF.EX.TowerFallExtensions.Layer
{
    public static class LayerExtensions
    {
        public static bool IsGameplayLayer(this Monocle.Layer layer)
        {
            return layer.Index == Level.GAMEPLAY_LAYER;
        }

        public static bool IsMenuLayer(this Monocle.Layer layer)
        {
            return layer.Index == MainMenu.MENU_ITEMS_LAYER;
        }

        public static bool IsHUDLayer(this Monocle.Layer layer)
        {
            return layer.Index == Level.HUD_LAYER;
        }
    }
}
