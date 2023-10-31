using Microsoft.Xna.Framework;

namespace TF.EX.Patchs.Entity
{
    public class TreasureSpawnerPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.TreasureSpawner.GetChestSpawnsForLevel += TreasureSpawner_GetChestSpawnsForLevel;
        }

        public void Unload()
        {
            On.TowerFall.TreasureSpawner.GetChestSpawnsForLevel -= TreasureSpawner_GetChestSpawnsForLevel;
        }

        private List<TowerFall.TreasureChest> TreasureSpawner_GetChestSpawnsForLevel(On.TowerFall.TreasureSpawner.orig_GetChestSpawnsForLevel orig, TowerFall.TreasureSpawner self, List<Vector2> chestPositions, List<Vector2> bigChestPositions)
        {
            Calc.CalcPatch.RegisterRng();
            Calc.CalcPatch.RegisterShuffle(chestPositions);
            var res = orig(self, chestPositions, new List<Vector2>()); //TODO: re enable big chests
            Calc.CalcPatch.UnregisterRng();

            //if (res.Count > 0) //Useful for test only
            //{
            //    foreach (var c in res.ToArray())
            //    {
            //        var dynPickup = MonoMod.Utils.DynamicData.For(c);
            //        dynPickup.Set("pickups", new List<TowerFall.Pickups> { TowerFall.Pickups.LavaOrb });
            //    }
            //}

            return res;
        }
    }
}
