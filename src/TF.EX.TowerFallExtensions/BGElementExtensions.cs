using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;

namespace TF.EX.TowerFallExtensions
{
    public static class BGElementExtensions
    {
        public static BackgroundElement ToModel(this TowerFall.Background.ScrollLayer scrollBgElement, int index)
        {
            return new BackgroundElement
            {
                index = index,
                Position = scrollBgElement.Image.Position.ToModel()
            };
        }
    }
}
