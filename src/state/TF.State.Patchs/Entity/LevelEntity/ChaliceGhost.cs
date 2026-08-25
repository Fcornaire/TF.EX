using HarmonyLib;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Context;
using TF.State.TowerFallExtensions.Entity;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(ChaliceGhost))]
    public class ChaliceGhostPatch
    {
        public const int PHASE_WAIT_SPAWNED = 0;
        public const int PHASE_WAIT_COLLIDABLE = 1;
        public const int PHASE_WAIT_CHASE = 2;
        public const int PHASE_CHASING = 3;
        public const int PHASE_DONE = 4;

        private const float COLLIDABLE_DELAY = 20f;
        private const float CHASE_DELAY = 90f;
        private const float CHASE_RAMP = 480f;
        private const float ATTACK_COOLDOWN = 30f;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(int), typeof(Chalice)])]
        public static void ChaliceGhost_ctor(ChaliceGhost __instance)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Add("chaliceGhostPhase", PHASE_WAIT_SPAWNED);
            dyn.Add("chaliceGhostCounter", 0f);
            dyn.Add("chaliceGhostAttackCooldown", 0f);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnPlayerCollide")]
        public static void ChaliceGhost_OnPlayerCollide_Postfix(ChaliceGhost __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            if (!__instance.Components.Any(component => component is Alarm))
            {
                return;
            }

            __instance.DeleteAllComponents<Alarm>();
            DynamicData.For(__instance).Set("chaliceGhostAttackCooldown", ATTACK_COOLDOWN);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void ChaliceGhost_Update_Postfix(ChaliceGhost __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            __instance.DeleteAllComponents<Coroutine>();

            var dyn = DynamicData.For(__instance);

            UpdateAttackCooldown(dyn);

            var phase = dyn.Get<int>("chaliceGhostPhase");
            var counter = dyn.Get<float>("chaliceGhostCounter");

            switch (phase)
            {
                case PHASE_WAIT_SPAWNED:
                    if (dyn.Get<bool>("spawned"))
                    {
                        dyn.Get<Wiggler>("wiggler").Start();
                        phase = PHASE_WAIT_COLLIDABLE;
                        counter = 0f;
                    }
                    break;

                case PHASE_WAIT_COLLIDABLE:
                    counter += Engine.TimeMult;
                    if (counter >= COLLIDABLE_DELAY)
                    {
                        __instance.Collidable = true;
                        dyn.Set("canFindTarget", true);
                        phase = PHASE_WAIT_CHASE;
                        counter = 0f;
                    }
                    break;

                case PHASE_WAIT_CHASE:
                    counter += Engine.TimeMult;
                    if (counter >= CHASE_DELAY)
                    {
                        phase = PHASE_CHASING;
                        counter = 0f;
                    }
                    break;

                case PHASE_CHASING:
                    counter += Engine.TimeMult;
                    dyn.Set("lerp", System.Math.Min(counter / CHASE_RAMP, 1f));
                    if (counter >= CHASE_RAMP)
                    {
                        phase = PHASE_DONE;
                    }
                    break;
            }

            dyn.Set("chaliceGhostPhase", phase);
            dyn.Set("chaliceGhostCounter", counter);
        }

        private static void UpdateAttackCooldown(DynamicData dyn)
        {
            var cooldown = dyn.Get<float>("chaliceGhostAttackCooldown");
            if (cooldown <= 0f)
            {
                return;
            }

            cooldown -= Engine.TimeMult;
            if (cooldown <= 0f)
            {
                cooldown = 0f;
                dyn.Set("canFindTarget", true);
            }

            dyn.Set("chaliceGhostAttackCooldown", cooldown);
        }
    }
}
