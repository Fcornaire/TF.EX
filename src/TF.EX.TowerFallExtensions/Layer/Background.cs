using MonoMod.Utils;
using TowerFall;

namespace TF.EX.TowerFallExtensions.Layer
{
    public static class BackgroundExtensions
    {
        public static List<Background.BGElement> GetBGElements(this Background background)
        {
            var dynBacground = DynamicData.For(background);
            return dynBacground.Get<List<Background.BGElement>>("elements");
        }
    }
}
