using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MonoMod.Utils;
using Level = TowerFall.Level;

namespace TF.EX.TowerFallExtensions
{

    public static class EntityDumper
    {
        private const int MaxFieldRecursionDepth = 4;

        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static string Dump(Level level)
        {
            var sb = new StringBuilder();

            var frame = DynamicData.For(level).Get<float>("FrameCounter");
            sb.AppendLine($"===== Entity dump | level={ResolveLevelName(level)} | frame={frame} =====");

            var topLevel = new HashSet<Monocle.Entity>(level.Layers.SelectMany(l => l.Value.Entities));

            sb.AppendLine();
            sb.AppendLine("--- Totals by type (all layers) ---");
            foreach (var group in topLevel
                .GroupBy(e => e.GetType().FullName)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key))
            {
                sb.AppendLine($"  x{group.Count(),-4} {group.Key}");
            }

            foreach (var layer in level.Layers.OrderBy(l => l.Key))
            {
                var entities = layer.Value.Entities;
                sb.AppendLine();
                sb.AppendLine($"--- Layer key={layer.Key} index={layer.Value.Index} | {entities.Count} entities ---");

                foreach (var group in entities.GroupBy(e => e.GetType().FullName).OrderBy(g => g.Key))
                {
                    sb.AppendLine($"  {group.Key}  (x{group.Count()})");
                    foreach (var entity in group)
                    {
                        var dyn = DynamicData.For(entity);
                        double actualDepth = dyn.Get<double>("actualDepth");
                        string tags = entity.Tags != null ? string.Join(",", entity.Tags) : "";
                        sb.AppendLine($"      depth={entity.Depth} actualDepth={actualDepth} pos=({entity.X},{entity.Y}) tags=[{tags}]");
                        AppendComponents(sb, entity, "        ");
                        AppendChildEntities(sb, entity, "        ", topLevel, new HashSet<Monocle.Entity>(), 0);
                    }
                }
            }

            return sb.ToString();
        }

        private static string ResolveLevelName(Level level)
        {
            try
            {
                var name = level.Session?.MatchSettings?.LevelSystem?.Theme?.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
            catch
            {
                // Fall through to the scene type name.
            }

            return level.GetType().Name;
        }

        private static void AppendComponents(StringBuilder sb, Monocle.Entity entity, string indent)
        {
            if (entity.Components == null)
            {
                return;
            }

            var grouped = entity.Components
                .GroupBy(c => c.GetType())
                .OrderBy(g => g.Key.Name)
                .Select(g => g.Count() > 1 ? $"{g.Key.Name} x{g.Count()}" : g.Key.Name)
                .ToList();

            if (grouped.Count > 0)
            {
                sb.AppendLine($"{indent}components: {string.Join(", ", grouped)}");
            }
        }

        private static void AppendChildEntities(StringBuilder sb, Monocle.Entity entity, string indent, HashSet<Monocle.Entity> topLevel, HashSet<Monocle.Entity> visited, int depth)
        {
            if (depth >= MaxFieldRecursionDepth)
            {
                return;
            }

            foreach (var field in GetAllFields(entity.GetType()))
            {
                if (!typeof(Monocle.Entity).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                if (field.GetValue(entity) is not Monocle.Entity child
                    || ReferenceEquals(child, entity)
                    || topLevel.Contains(child) 
                    || !visited.Add(child)) 
                {
                    continue;
                }

                sb.AppendLine($"{indent}-> field '{field.Name}': {child.GetType().FullName} pos=({child.X},{child.Y})");
                AppendComponents(sb, child, indent + "    ");
                AppendChildEntities(sb, child, indent + "    ", topLevel, visited, depth + 1);
            }
        }

        private static IEnumerable<FieldInfo> GetAllFields(Type type)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(FieldFlags | BindingFlags.DeclaredOnly))
                {
                    yield return field;
                }
            }
        }
    }
}
