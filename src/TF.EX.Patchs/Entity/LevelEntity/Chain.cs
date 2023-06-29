using Microsoft.Xna.Framework;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    internal class ChainPatch : IHookable
    {
        private readonly IRngService _rngService;

        public ChainPatch(IRngService rngService)
        {
            _rngService = rngService;
        }

        public void Load()
        {
            On.TowerFall.Chain.ctor_Vector2_int += Chain_ctor_Vector2_int;
        }

        public void Unload()
        {
            On.TowerFall.Chain.ctor_Vector2_int -= Chain_ctor_Vector2_int;
        }

        private void Chain_ctor_Vector2_int(On.TowerFall.Chain.orig_ctor_Vector2_int orig, TowerFall.Chain self, Vector2 position, int height)
        {
            _rngService.Get().ResetRandom();
            orig(self, position, height);
            _rngService.AddGen(RngGenType.Integer);
        }
    }
}
