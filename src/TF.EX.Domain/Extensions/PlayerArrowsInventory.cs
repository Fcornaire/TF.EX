using MonoMod.Utils;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;
using TowerFall;

namespace TF.EX.Domain.Extensions
{
    public static class PlayerArrowsInventoryExtensions
    {
        public static PlayerArrowsInventory ToModel(this ArrowList arrowList)
        {
            var dynArrowList = DynamicData.For(arrowList);
            List<TowerFall.ArrowTypes> sortSet = dynArrowList.Get<List<TowerFall.ArrowTypes>>("sortSet");

            return new PlayerArrowsInventory
            {
                Arrows = arrowList.Arrows.Select(arrow => arrow.ToModel()),
                SortSet = sortSet.Select(arrow => arrow.ToModel())
            };

        }

        public static void ToLoad(this ArrowList arrowList, PlayerArrowsInventory toLoad)
        {
            arrowList.Arrows.Clear();

            foreach (var arrow in toLoad.Arrows)
            {
                arrowList.Arrows.Add(arrow.ToTFModel());
            }

            var dynArrowList = DynamicData.For(arrowList);
            List<TowerFall.ArrowTypes> sortSet = dynArrowList.Get<List<TowerFall.ArrowTypes>>("sortSet");
            sortSet.Clear();

            foreach (var arrow in toLoad.SortSet)
            {
                sortSet.Add(arrow.ToTFModel());
            }
        }
    }
}
