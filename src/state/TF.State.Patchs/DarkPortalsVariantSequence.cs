using HarmonyLib;
using Monocle;
using MonoMod.Utils;
using TF.State.Patchs.Calc;
using TF.State.TowerFallExtensions;
using TF.State.TowerFallExtensions.Entity;
using TowerFall;

namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(DarkPortalsVariantSequence))]
    public class DarkPortalsVariantSequencePatch
    {
        public const int PHASE_WAIT_ROUND_START = 0;
        public const int PHASE_WAIT_APPEAR = 1;
        public const int PHASE_PORTAL_APPEARING = 2;
        public const int PHASE_SPAWN_LOOP = 3;
        public const int PHASE_DONE = 4;

        private const float APPEAR_DELAY = 20f;
        private const float SPAWN_INTERVAL = 60f;
        private const int MAX_ENEMIES = 3;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        public static void DarkPortalsVariantSequence_ctor(DarkPortalsVariantSequence __instance)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Add("darkPortalsPhase", PHASE_WAIT_ROUND_START);
            dyn.Add("darkPortalsCounter", 0f);
        }

        public static void Step(Level level)
        {
            if (!level.Session.MatchSettings.Variants.DarkPortals)
            {
                return;
            }

            var sequence = level.GetAll<DarkPortalsVariantSequence>().FirstOrDefault();
            if (sequence == null)
            {
                return;
            }

            sequence.DeleteAllComponents<Monocle.Coroutine>();

            var dyn = DynamicData.For(sequence);
            var phase = dyn.Get<int>("darkPortalsPhase");
            var counter = dyn.Get<float>("darkPortalsCounter");

            switch (phase)
            {
                case PHASE_WAIT_ROUND_START:
                    if (level.Session.RoundLogic.RoundStarted || level.Session.MatchSettings.Mode == Modes.LevelTest)
                    {
                        phase = PHASE_WAIT_APPEAR;
                        counter = 0f;
                    }
                    break;

                case PHASE_WAIT_APPEAR:
                    counter += TFGame.TimeMult;
                    if (counter >= APPEAR_DELAY)
                    {
                        var positions = level.GetXMLPositions("Spawner");
                        if (positions.Count == 0)
                        {
                            phase = PHASE_DONE;
                            break;
                        }

                        CalcPatch.RegisterRng();
                        positions.Shuffle(Monocle.Calc.Random);
                        CalcPatch.UnregisterRng();

                        var portal = new QuestSpawnPortal(positions[0], null);
                        level.Add(portal);
                        portal.Appear();
                        phase = PHASE_PORTAL_APPEARING;
                        counter = 0f;
                    }
                    break;

                case PHASE_PORTAL_APPEARING:
                    counter += TFGame.TimeMult;
                    if (counter >= APPEAR_DELAY)
                    {
                        phase = PHASE_SPAWN_LOOP;
                        counter = SPAWN_INTERVAL;
                    }
                    break;

                case PHASE_SPAWN_LOOP:
                    counter += TFGame.TimeMult;
                    if (counter >= SPAWN_INTERVAL)
                    {
                        counter -= SPAWN_INTERVAL;

                        var portal = level.GetAll<QuestSpawnPortal>().FirstOrDefault();
                        if (portal != null && level[Monocle.GameTags.Enemy].Count + portal.QueuedSpawns < MAX_ENEMIES)
                        {
                            CalcPatch.RegisterRng();
                            var enemy = Monocle.Calc.Random.Choose("Bat", "Slime");
                            CalcPatch.UnregisterRng();

                            portal.SpawnEnemy(enemy);
                        }
                    }
                    break;
            }

            dyn.Set("darkPortalsPhase", phase);
            dyn.Set("darkPortalsCounter", counter);
        }
    }
}
