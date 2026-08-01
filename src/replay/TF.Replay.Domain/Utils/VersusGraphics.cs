using System.Runtime.InteropServices;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain.Utils
{
    public class VersusGraphics
    {
        public static List<Image> GetVersusGraphics(int levelID) => GetGraphics(GameData.VersusTowers[levelID].Theme);

        public static List<Image> GetGraphics(TowerTheme towerTheme)
        {
            Image image = new Image(MapButton.GetBlockTexture(towerTheme.TowerType));
            image.CenterOrigin();
            Image image2 = new Image(towerTheme.Icon);
            image2.CenterOrigin();
            image2.Color = MapButton.GetTint(towerTheme.TowerType);
            int num = 2;
            List<Image> list = new List<Image>(num);
            CollectionsMarshal.SetCount(list, num);
            Span<Image> span = CollectionsMarshal.AsSpan(list);
            int num2 = 0;
            span[num2] = image;
            num2++;
            span[num2] = image2;
            return list;
        }
    }
}
