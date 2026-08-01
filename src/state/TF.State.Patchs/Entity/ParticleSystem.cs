using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.Patchs.Calc;

using TF.State.Domain.Context;
namespace TF.State.Patchs.Entity
{
    [HarmonyPatch(typeof(ParticleSystem))]
    internal class ParticleSystemPatch
    {
        private static Random _random = new Random();

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool ParticleSystem_Update(ParticleSystem __instance)
        {

            if (StateFlags.IsRollbackFrame)
            {
                return false;
            }

            return true;
        }

        //Same as original, but with custom random since calc is being used deterministically
        [HarmonyPrefix]
        [HarmonyPatch("Emit", [typeof(Monocle.ParticleType), typeof(int), typeof(Vector2), typeof(Vector2), typeof(float)])]
        public static bool ParticleSystem_Emit_ParticleType_int_Vector2_Vector2_float(
            Monocle.ParticleSystem __instance,
            Monocle.ParticleType type,
            int amount,
            Vector2 position,
            Vector2 positionRange,
            float direction)
        {

            if (StateFlags.IsRollbackFrame)
            {
                return false;
            }

            CalcPatch.IgnoreToRegisterRng();
            for (int i = 0; i < amount; i++)
            {
                __instance.Emit(type, _random.Range(position - positionRange, positionRange * 2f), direction);
            }
            CalcPatch.UnignoreToRegisterRng();
            return false;
        }

        //Same as original, but with custom random since calc is being used deterministically
        [HarmonyPrefix]
        [HarmonyPatch("Emit", [typeof(Monocle.ParticleType), typeof(int), typeof(Vector2), typeof(Vector2)])]
        public static bool ParticleSystem_Emit_ParticleType_int_Vector2_Vector2(
            Monocle.ParticleSystem __instance,
            Monocle.ParticleType type,
            int amount,
            Vector2 position,
            Vector2 positionRange)
        {

            if (StateFlags.IsRollbackFrame)
            {
                return false;
            }

            CalcPatch.IgnoreToRegisterRng();
            for (int i = 0; i < amount; i++)
            {
                __instance.Emit(type, _random.Range(position - positionRange, positionRange * 2f));
            }
            CalcPatch.UnignoreToRegisterRng();
            return false;
        }
    }
}
