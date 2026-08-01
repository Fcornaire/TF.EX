using HarmonyLib;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.TowerFallExtensions.Entity.LevelEntity;

using TF.State.Domain.Context;
namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.Icicle))]
    internal class IciclePatch
    {
        private const float FallDelay = 15f;

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void Icicle_Update(TowerFall.Icicle __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            if (StateFlags.IsRestoring)
            {
                return;
            }

            float fallCounter = IcicleOwnedState.GetFallCounter(__instance);
            if (fallCounter <= IcicleOwnedState.NotShaking)
            {
                return;
            }

            fallCounter -= TowerFall.TFGame.TimeMult;
            if (fallCounter <= 0f)
            {
                var dyn = DynamicData.For(__instance);
                dyn.Set("falling", true);
                dyn.Set("canFall", false);
                IcicleOwnedState.SetFallCounter(__instance, IcicleOwnedState.NotShaking);
            }
            else
            {
                IcicleOwnedState.SetFallCounter(__instance, fallCounter);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnArrowHit")]
        public static bool Icicle_OnArrowHit(TowerFall.Icicle __instance, TowerFall.Arrow arrow, ref bool __result)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            var dyn = DynamicData.For(__instance);
            if (dyn.Get<bool>("canFall"))
            {
                arrow.EnterFallMode();
                dyn.Set("cannotHitArrow", arrow);
                StartFalling(__instance, arrow.PlayerIndex);
            }
            else if (dyn.Get<bool>("falling") && arrow != dyn.Get<TowerFall.Arrow>("cannotHitArrow"))
            {
                arrow.EnterFallMode();
                dyn.Invoke("Explode");
            }

            __result = true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnPlayerCollide")]
        public static bool Icicle_OnPlayerCollide(TowerFall.Icicle __instance, TowerFall.Player player)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            var dyn = DynamicData.For(__instance);
            if (dyn.Get<bool>("canFall"))
            {
                dyn.Set("cannotHit", player);
                StartFalling(__instance, player.PlayerIndex);
            }
            else if (dyn.Get<bool>("falling")
                && player != dyn.Get<TowerFall.LevelEntity>("cannotHit")
                && __instance.Bottom - dyn.Get<float>("vSpeed") < player.Y - player.Speed.Y - 2f
                && player.CanHurt)
            {
                player.Hurt(TowerFall.DeathCause.FallingObject, __instance.Position, dyn.Get<int>("ownerIndex"));
                dyn.Invoke("Explode");
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnPlayerGhostCollide")]
        public static bool Icicle_OnPlayerGhostCollide(TowerFall.Icicle __instance, TowerFall.PlayerGhost ghost)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            var dyn = DynamicData.For(__instance);
            if (dyn.Get<bool>("canFall"))
            {
                dyn.Set("cannotHit", ghost);
                StartFalling(__instance, ghost.PlayerIndex);
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnExplodePush")]
        public static bool Icicle_OnExplodePush(TowerFall.Icicle __instance, TowerFall.Explosion explosion, Microsoft.Xna.Framework.Vector2 normal)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            var dyn = DynamicData.For(__instance);
            if (dyn.Get<bool>("canFall"))
            {
                StartFalling(__instance, explosion.PlayerIndex);
            }
            else if (dyn.Get<bool>("falling"))
            {
                dyn.Invoke("Explode");
            }

            return false;
        }

        private static void StartFalling(TowerFall.Icicle icicle, int ownerIndex)
        {
            var dyn = DynamicData.For(icicle);
            dyn.Set("ownerIndex", ownerIndex);
            dyn.Set("canFall", false);
            IcicleOwnedState.SetFallCounter(icicle, FallDelay);

            if (ownerIndex != -1)
            {
                TowerFall.TFGame.PlayerInputs[ownerIndex].Rumble(0.5f, 10);
            }
            TowerFall.Sounds.sfx_icicleTouch.Play(icicle.X);
        }
    }
}
