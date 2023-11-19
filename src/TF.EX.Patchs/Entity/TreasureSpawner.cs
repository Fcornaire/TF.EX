using Microsoft.Xna.Framework;

namespace TF.EX.Patchs.Entity
{
    public class TreasureSpawnerPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.TreasureSpawner.GetChestSpawnsForLevel += TreasureSpawner_GetChestSpawnsForLevel;
            On.TowerFall.TreasureSpawner.ctor_Session_Int32Array_float_bool += TreasureSpawner_ctor_Session_Int32Array_float_bool;
        }

        public void Unload()
        {
            On.TowerFall.TreasureSpawner.GetChestSpawnsForLevel -= TreasureSpawner_GetChestSpawnsForLevel;
            On.TowerFall.TreasureSpawner.ctor_Session_Int32Array_float_bool -= TreasureSpawner_ctor_Session_Int32Array_float_bool;
        }

        private void TreasureSpawner_ctor_Session_Int32Array_float_bool(On.TowerFall.TreasureSpawner.orig_ctor_Session_Int32Array_float_bool orig, TowerFall.TreasureSpawner self, TowerFall.Session session, int[] mask, float arrowChance, bool arrowShuffle)
        {
            orig(self, session, mask, arrowChance, arrowShuffle);

            var dynSpawner = MonoMod.Utils.DynamicData.For(self);
            dynSpawner.Set("Random", Monocle.Calc.Random);
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
