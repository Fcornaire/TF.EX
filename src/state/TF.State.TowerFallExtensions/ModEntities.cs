using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Models;
using TowerFall;

namespace TF.State.TowerFallExtensions
{
    /// <summary>
    /// Mods' entities in the rollback state (ModExports.RegisterEntityStateEvents), carried in the mods' data: one entry per mod's
    /// registration, each entity as one base64 blob: its depth (8 bytes, little-endian bits) then its own bytes.
    /// </summary>
    public static class ModEntitiesExtensions
    {
        private const string KeyPrefix = "entities:";

        public static void AddModEntitiesState(this GameState gameState, Level level)
        {
            foreach (var events in ServiceCollections.ResolveAPIManager().GetEntityStateEvents())
            {
                var entries = LiveEntities(level, events.Owns)
                    .Select(entity => Convert.ToBase64String(WithDepth(ActualDepth(entity), events.OnSaveState(entity) ?? [])))
                    .ToList();

                // none, nothing in the state (as for a player without the mod)
                if (entries.Count > 0)
                {
                    gameState.AdditionnalData[KeyPrefix + events.Key] = string.Join(";", entries);
                }
            }
        }

        public static void LoadModEntitiesState(this GameState gameState, Level level)
        {
            foreach (var events in ServiceCollections.ResolveAPIManager().GetEntityStateEvents())
            {
                // absent: none were saved (the mod's own entities made after that frame go)
                gameState.AdditionnalData.TryGetValue(KeyPrefix + events.Key, out var saved);
                saved ??= "";

                var live = LiveEntities(level, events.Owns);

                foreach (var entry in saved.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var blob = Convert.FromBase64String(entry);
                    var actualDepth = DepthOf(blob);
                    var bytes = blob.Skip(8).ToArray();

                    var entity = live.FirstOrDefault(candidate => ActualDepth(candidate) == actualDepth);

                    if (entity == null && events.Create != null)
                    {
                        entity = events.Create();
                        level.Spawn(entity, actualDepth);
                    }

                    if (entity != null)
                    {
                        live.Remove(entity);
                        events.OnLoadState(entity, bytes);
                    }
                }

                if (events.Create != null)
                {
                    foreach (var extra in live)
                    {
                        level.RemoveEntity(extra);
                    }
                }
            }
        }

        private static List<Monocle.Entity> LiveEntities(Level level, Func<Monocle.Entity, bool> owns)
        {
            var layer = level.GetGameplayLayer();
            var toAdd = DynamicData.For(layer).Get<List<Monocle.Entity>>("toAdd") ?? [];

            return layer.Entities.Concat(toAdd)
                .Where(entity => owns(entity) && !entity.MarkedForRemoval)
                .Distinct()
                .OrderBy(ActualDepth)
                .ToList();
        }

        private static byte[] WithDepth(double actualDepth, byte[] bytes)
        {
            var bits = BitConverter.DoubleToInt64Bits(actualDepth);
            var blob = new byte[8 + bytes.Length];
            for (int i = 0; i < 8; i++)
            {
                blob[i] = (byte)(bits >> (i * 8));
            }
            Array.Copy(bytes, 0, blob, 8, bytes.Length);
            return blob;
        }

        private static double DepthOf(byte[] blob)
        {
            long bits = 0;
            for (int i = 0; i < 8 && i < blob.Length; i++)
            {
                bits |= (long)blob[i] << (i * 8);
            }
            return BitConverter.Int64BitsToDouble(bits);
        }

        private static double ActualDepth(Monocle.Entity entity)
        {
            return DynamicData.For(entity).Get<double>("actualDepth");
        }
    }
}
