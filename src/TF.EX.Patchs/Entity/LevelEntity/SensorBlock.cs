using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.TowerFallExtensions.Entity.LevelEntity;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.SensorBlock))]
    internal class SensorBlockPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool SensorBlock_Update(TowerFall.SensorBlock __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (!netplayManager.IsInit())
            {
                return true;
            }

            if (netplayManager.IsUpdating())
            {
                return false;
            }

            var sequenceCounter = SensorBlockOwnedState.GetSequenceCounter(__instance);
            if (sequenceCounter <= SensorBlockOwnedState.Idle)
            {
                if (__instance.CanSense)
                {
                    TryDetect(__instance);
                }

                return false;
            }

            var previous = sequenceCounter;
            sequenceCounter += TowerFall.TFGame.TimeMult;

            if (previous < SensorBlockOwnedState.SlamStart && sequenceCounter >= SensorBlockOwnedState.SlamStart)
            {
                TowerFall.Sounds.env_crusherSlam.Play(__instance.X);
            }

            if (previous < SensorBlockOwnedState.RiseStart && sequenceCounter >= SensorBlockOwnedState.RiseStart)
            {
                TowerFall.Sounds.env_crusherRise.Play(__instance.X);
            }

            if (sequenceCounter >= SensorBlockOwnedState.Duration)
            {
                __instance.SquisherIndex = -1;
                SensorBlockOwnedState.SetSequenceCounter(__instance, SensorBlockOwnedState.Idle);
                SensorBlockOwnedState.Apply(__instance, SensorBlockOwnedState.Duration);
            }
            else
            {
                SensorBlockOwnedState.SetSequenceCounter(__instance, sequenceCounter);
                SensorBlockOwnedState.Apply(__instance, sequenceCounter);
            }

            return false;
        }

        private static void TryDetect(TowerFall.SensorBlock block)
        {
            var detector = DynamicData.For(block).Get<Rectangle>("detector");

            foreach (TowerFall.Player player in block.Level.Players)
            {
                if (!player.Flashing && player.Speed != Vector2.Zero && player.CollideCheck(detector))
                {
                    StartSequence(block, player.PlayerIndex);
                    return;
                }
            }

            foreach (TowerFall.Arrow arrow in block.Level[GameTags.Arrow])
            {
                if (arrow.Speed != Vector2.Zero && arrow.PlayerIndex != -1 && arrow.CollideCheck(detector))
                {
                    StartSequence(block, arrow.PlayerIndex);
                    return;
                }
            }

            foreach (TowerFall.PlayerGhost ghost in block.Level[GameTags.PlayerGhost])
            {
                if (ghost.Speed != Vector2.Zero && ghost.CollideCheck(detector))
                {
                    StartSequence(block, ghost.PlayerIndex);
                    return;
                }
            }
        }

        private static void StartSequence(TowerFall.SensorBlock block, int squisherIndex)
        {
            block.SquisherIndex = squisherIndex;
            SensorBlockOwnedState.SetSequenceCounter(block, 0f);
            TowerFall.Sounds.env_crusherDetect.Play(block.X);
        }
    }
}
