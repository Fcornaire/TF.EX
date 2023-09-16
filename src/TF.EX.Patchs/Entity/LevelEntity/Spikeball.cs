using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    internal class SpikeballPatch : IHookable
    {
        private readonly IRngService _rngService;

        public SpikeballPatch(IRngService rngService)
        {
            _rngService = rngService;
        }

        public void Load()
        {
            On.TowerFall.Spikeball.ctor_Vector2_Vector2_bool += Spikeball_ctor_Vector2_Vector2_bool;
        }

        public void Unload()
        {
            On.TowerFall.Spikeball.ctor_Vector2_Vector2_bool -= Spikeball_ctor_Vector2_Vector2_bool;
        }

        /// <summary>
        /// Patch fot spawning spikeball to use our RNG
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="position"></param>
        /// <param name="pivot"></param>
        /// <param name="exploding"></param>
        private void Spikeball_ctor_Vector2_Vector2_bool(On.TowerFall.Spikeball.orig_ctor_Vector2_Vector2_bool orig, TowerFall.Spikeball self, Vector2 position, Vector2 pivot, bool exploding)
        {
            orig(self, position, pivot, exploding);

            _rngService.Get().ResetRandom();
            var dynSpikeball = DynamicData.For(self);
            dynSpikeball.Set("spinDir", Monocle.Calc.Random.Choose(1, -1));
            _rngService.AddGen(Domain.Models.State.RngGenType.Integer);
        }
    }
}
