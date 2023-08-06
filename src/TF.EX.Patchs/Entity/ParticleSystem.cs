using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Patchs.Calc;

namespace TF.EX.Patchs.Entity
{
    internal class ParticleSystemPatch : IHookable
    {
        private readonly Random _random = new Random();

        public void Load()
        {
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2 += ParticleSystem_Emit_ParticleType_int_Vector2_Vector2;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2_float += ParticleSystem_Emit_ParticleType_int_Vector2_Vector2_float;
        }

        public void Unload()
        {
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2 -= ParticleSystem_Emit_ParticleType_int_Vector2_Vector2;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2_float -= ParticleSystem_Emit_ParticleType_int_Vector2_Vector2_float;
        }

        //Same as original, but with custom random since calc is being used deterministically
        private void ParticleSystem_Emit_ParticleType_int_Vector2_Vector2_float(
            On.Monocle.ParticleSystem.orig_Emit_ParticleType_int_Vector2_Vector2_float orig,
            Monocle.ParticleSystem self,
            Monocle.ParticleType type,
            int amount,
            Vector2 position,
            Vector2 positionRange,
            float direction)
        {
            CalcPatch.IgnoreToRegisterRng();
            for (int i = 0; i < amount; i++)
            {
                self.Emit(type, _random.Range(position - positionRange, positionRange * 2f), direction);
            }
            CalcPatch.UnignoreToRegisterRng();
        }

        //Same as original, but with custom random since calc is being used deterministically
        private void ParticleSystem_Emit_ParticleType_int_Vector2_Vector2(
            On.Monocle.ParticleSystem.orig_Emit_ParticleType_int_Vector2_Vector2 orig,
            Monocle.ParticleSystem self,
            Monocle.ParticleType type,
            int amount,
            Vector2 position,
            Vector2 positionRange)
        {
            CalcPatch.IgnoreToRegisterRng();
            for (int i = 0; i < amount; i++)
            {
                self.Emit(type, _random.Range(position - positionRange, positionRange * 2f));
            }
            CalcPatch.UnignoreToRegisterRng();
        }
    }
}
