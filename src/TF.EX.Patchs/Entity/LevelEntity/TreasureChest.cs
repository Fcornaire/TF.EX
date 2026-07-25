using HarmonyLib;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.TowerFallExtensions;
using TF.EX.TowerFallExtensions.Entity;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TreasureChest))]
    public class TreasureChestPatch
    {
        private const float RESPAWN_DELAY = 90f;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Microsoft.Xna.Framework.Vector2), typeof(TreasureChest.Types), typeof(TreasureChest.AppearModes), typeof(Pickups[]), typeof(int)])]
        public static void TreasureChest_ctor(TreasureChest __instance)
        {
            DynamicData.For(__instance).Set("bottomlessCounter", -1f);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Added")]
        public static void TreasureChest_Added_Postfix(TreasureChest __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (!netplayManager.IsInit())
            {
                return;
            }

            var dyn = DynamicData.For(__instance);
            var queued = dyn.Get<TreasurePreview>("preview");

            if (queued == null)
            {
                return;
            }

            foreach (var layer in __instance.Level.Layers.Values)
            {
                DynamicData.For(layer).Get<List<Monocle.Entity>>("toAdd")?.Remove(queued);
            }

            dyn.Set("preview", null);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(TreasureChest.OpenChest))]
        public static void TreasureChest_OpenChest_Postfix(TreasureChest __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var dyn = DynamicData.For(__instance);

            if (!netplayManager.IsInit() || dyn.Get<TreasureChest.Types>("type") != TreasureChest.Types.Bottomless)
            {
                return;
            }

            var actualDepth = dyn.Get<double>("actualDepth");

            foreach (var layer in __instance.Level.Layers.Values)
            {
                var toAdd = DynamicData.For(layer).Get<List<Monocle.Entity>>("toAdd");
                if (toAdd == null)
                {
                    continue;
                }

                foreach (var pending in toAdd)
                {
                    if (pending is Pickup pickup && DynamicData.For(pickup).Get<double>("bottomlessChestDepth") == -1.0)
                    {
                        DynamicData.For(pickup).Set("bottomlessChestDepth", actualDepth);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void TreasureChest_Update_Postfix(TreasureChest __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (!netplayManager.IsInit())
            {
                return;
            }

            var dyn = DynamicData.For(__instance);

            UpdatePreview(__instance, dyn);

            if (dyn.Get<TreasureChest.Types>("type") != TreasureChest.Types.Bottomless)
            {
                return;
            }

            __instance.DeleteAllComponents<Coroutine>();

            if (__instance.State != TreasureChest.States.Opened && __instance.State != TreasureChest.States.Opening)
            {
                return;
            }

            var actualDepth = dyn.Get<double>("actualDepth");

            if (HasLivePickup(__instance, actualDepth))
            {
                dyn.Set("bottomlessCounter", -1f);
                return;
            }

            var counter = dyn.Get<float>("bottomlessCounter");

            if (counter < 0f)
            {
                dyn.Set("bottomlessCounter", 0f);
                return;
            }

            counter += Monocle.Engine.TimeMult;
            dyn.Set("bottomlessCounter", counter);

            if (counter < RESPAWN_DELAY)
            {
                return;
            }

            SpawnNextPickup(__instance, actualDepth);
            dyn.Set("bottomlessCounter", -1f);
        }

        private static void UpdatePreview(TreasureChest self, DynamicData dyn)
        {
            if (!(bool)self.Level.Session.MatchSettings.Variants.ShowTreasureSpawns)
            {
                return;
            }

            var actualDepth = dyn.Get<double>("actualDepth");
            var preview = FindPreview(self.Level, actualDepth);

            if (preview != null)
            {
                preview.Visible = self.State == TreasureChest.States.WaitingToAppear;
                return;
            }

            if (self.State != TreasureChest.States.WaitingToAppear)
            {
                return;
            }

            preview = new TreasurePreview(self.Position, dyn.Get<TreasureChest.Types>("type") == TreasureChest.Types.Large);
            DynamicData.For(preview).Set("previewChestDepth", actualDepth);
            self.Level.Add(preview);
        }

        private static TreasurePreview FindPreview(Level level, double actualDepth)
        {
            foreach (var preview in level.GetAll<TreasurePreview>())
            {
                if (DynamicData.For(preview).Get<double>("previewChestDepth") == actualDepth)
                {
                    return preview;
                }
            }

            foreach (var layer in level.Layers.Values)
            {
                var toAdd = DynamicData.For(layer).Get<List<Monocle.Entity>>("toAdd");
                if (toAdd == null)
                {
                    continue;
                }

                foreach (var pending in toAdd)
                {
                    if (pending is TreasurePreview preview && DynamicData.For(preview).Get<double>("previewChestDepth") == actualDepth)
                    {
                        return preview;
                    }
                }
            }

            return null;
        }

        private static bool HasLivePickup(TreasureChest self, double actualDepth)
        {
            foreach (var pickup in self.Level.GetAll<Pickup>())
            {
                if (pickup.Scene != null
                    && !pickup.MarkedForRemoval
                    && DynamicData.For(pickup).Get<double>("bottomlessChestDepth") == actualDepth)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SpawnNextPickup(TreasureChest self, double actualDepth)
        {
            Calc.CalcPatch.RegisterRng();
            var treasure = self.Level.Session.TreasureSpawner.GetTreasureSpawn();
            Calc.CalcPatch.UnregisterRng();

            var target = Pickup.GetTargetPositionFromChest(self.Level, self.Position - Microsoft.Xna.Framework.Vector2.UnitY);
            var pickup = Pickup.CreatePickup(self.Position, target, treasure, -1);

            DynamicData.For(pickup).Set("bottomlessChestDepth", actualDepth);
            pickup.StartTimeout();
            self.Level.Add(pickup);

            Sounds.sfx_bottomlessTreasure.Play();
        }
    }
}
