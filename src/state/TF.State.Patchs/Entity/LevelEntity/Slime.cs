using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Slime))]
    public class SlimePatch
    {
        private const int ST_AIR = 1;
        private const int ST_JUMP = 2;

        private const int JUMP_PHASE_CROUCH = 0;
        private const int JUMP_PHASE_SQUASH = 1;
        private const int JUMP_PHASE_LAUNCH = 2;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Facing), typeof(Slime.SlimeColors)])]
        public static void Slime_ctor(Slime __instance)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Add("enemyStateCounter", 0f);
            dyn.Add("enemyStatePhase", 0);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Slime_Update_Postfix(Slime __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dyn = DynamicData.For(__instance);
            dyn.Get<Coroutine>("coroutine")?.Cancel();

            if (dyn.Get<int>("currentState") != ST_JUMP)
            {
                return;
            }

            var counter = dyn.Get<float>("enemyStateCounter") + Monocle.Engine.TimeMult;
            dyn.Set("enemyStateCounter", counter);

            var sprite = dyn.Get<Monocle.Sprite<string>>("sprite");

            switch (dyn.Get<int>("enemyStatePhase"))
            {
                case JUMP_PHASE_CROUCH:
                    if (counter >= 5f)
                    {
                        dyn.Set("enemyStatePhase", JUMP_PHASE_SQUASH);
                        dyn.Set("enemyStateCounter", 0f);
                    }
                    break;

                case JUMP_PHASE_SQUASH:
                    sprite.Scale.Y = Monocle.Calc.Approach(sprite.Scale.Y, 0.6f, 0.1f * Monocle.Engine.TimeMult);
                    if (sprite.Scale.Y <= 0.61f)
                    {
                        dyn.Set("enemyStatePhase", JUMP_PHASE_LAUNCH);
                        dyn.Set("enemyStateCounter", 0f);
                    }
                    break;

                case JUMP_PHASE_LAUNCH:
                    if (counter >= 5f)
                    {
                        Sounds.en_slimeJump.Play(__instance.X);
                        sprite.Scale = new Vector2(0.5f, 1.2f);
                        __instance.Speed.X = dyn.Get<float>("jumpHSpeed") * (float)__instance.Facing;
                        __instance.Speed.Y = dyn.Get<float>("jumpVSpeed");
                        __instance.State = ST_AIR;
                    }
                    break;
            }
        }
    }
}
