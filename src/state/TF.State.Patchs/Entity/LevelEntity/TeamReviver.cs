using HarmonyLib;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.TowerFallExtensions.Entity;
using TowerFall;

using TF.State.Domain.Context;
namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TeamReviver))]
    public class TeamReviverPatch
    {
        private const string SEQUENCE_COUNTER = "reviveSequenceCounter";

        private const float FINISH_REVIVING = 5f;
        private const float REMOVE_CORPSE = 6f;
        private const float PLAY_SFX = 21f;
        private const float UNFREEZE = 36f;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(PlayerCorpse), typeof(TeamReviver.Modes)])]
        public static void TeamReviver_ctor(TeamReviver __instance)
        {
            DynamicData.For(__instance).Set(SEQUENCE_COUNTER, -1f);
        }

        [HarmonyPostfix]
        [HarmonyPatch("ReviveUpdate")]
        public static void TeamReviver_ReviveUpdate_Postfix(TeamReviver __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            __instance.DeleteAllComponents<Coroutine>();

            var dyn = DynamicData.For(__instance);
            var counter = dyn.Get<float>(SEQUENCE_COUNTER);

            if (!__instance.Finished)
            {
                return;
            }

            if (counter < 0f)
            {
                dyn.Set(SEQUENCE_COUNTER, 0f);
                DespawnGhost(__instance);
                return;
            }

            var previous = counter;
            counter += Monocle.Engine.TimeMult;
            dyn.Set(SEQUENCE_COUNTER, counter);

            if (Reached(previous, counter, FINISH_REVIVING))
            {
                FinishReviving(__instance, dyn);
                return;
            }

            if (Reached(previous, counter, REMOVE_CORPSE))
            {
                __instance.Level.Remove(__instance.Corpse);
                dyn.Set("targetLightAlpha", 0f);
                return;
            }

            if (Reached(previous, counter, PLAY_SFX))
            {
                var reviving = __instance.Level.GetPlayer(__instance.Corpse.PlayerIndex);
                reviving?.ArcherData.SFX.Revive.Play(__instance.X);
                return;
            }

            if (Reached(previous, counter, UNFREEZE))
            {
                __instance.Level.GetPlayer(__instance.Corpse.PlayerIndex)?.Unfreeze();
                __instance.RemoveSelf();
            }
        }

        private static void DespawnGhost(TeamReviver self)
        {
            foreach (PlayerGhost ghost in self.Level[Monocle.GameTags.PlayerGhost])
            {
                if (ghost.PlayerIndex == self.Corpse.PlayerIndex)
                {
                    ghost.Despawn(self.Corpse);
                    break;
                }
            }
        }

        private static void FinishReviving(TeamReviver self, DynamicData dyn)
        {
            var player = dyn.Invoke("FinishReviving") as Player;

            if (player != null)
            {
                return;
            }

            dyn.Set(SEQUENCE_COUNTER, -1f);
            dyn.Set("levitateCorpse", false);
            dyn.Set("Finished", false);
            dyn.Set("reviving", false);
            self.Corpse.Reviving = false;
            self.Corpse.Revived = false;

            if (self.Level.Session.MatchSettings.Mode == Modes.TeamDeathmatch)
            {
                (self.Level.Session.RoundLogic as TeamDeathmatchRoundLogic).CheckForWin();
            }
        }

        private static bool Reached(float previous, float counter, float threshold)
        {
            return previous < threshold && counter >= threshold;
        }
    }
}
