using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Arrows;
using TF.EX.Domain.Models.State.Player;

namespace TF.EX.Domain.Extensions
{
    public static class PlayerArrowsInventoryExtensions
    {
        public static PlayerArrowsInventory ToModel(this List<TowerFall.ArrowTypes> playerArrows)
        {
            var normal = playerArrows.Where(arrow => arrow.ToModel().Equals(ArrowTypes.Normal)).Count();

            return new PlayerArrowsInventory
            {
                Normal = normal,
            };

        }

        public static void Update(this List<TowerFall.ArrowTypes> playerArrows, PlayerArrowsInventory toLoad)
        {
            playerArrows.Clear();

            for (int i = 0; i < toLoad.Normal; i++)
            {
                playerArrows.Add(TowerFall.ArrowTypes.Normal);
            }
        }
    }
}
