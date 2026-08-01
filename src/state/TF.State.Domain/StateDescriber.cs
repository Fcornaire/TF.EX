using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using MessagePack;
using TF.State.Domain.Models;

namespace TF.State.Domain
{
    public static class StateDescriber
    {
        private const int MaxDepth = 8;
        private const int MaxItems = 64;

        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public;

        public static string Describe(byte[] state, int maxDepth = MaxDepth)
        {
            if (state == null)
            {
                return "";
            }

            GameState gameState;

            try
            {
                gameState = MessagePackSerializer.Deserialize<GameState>(state, StateSerialization.Options);
            }
            catch (Exception e)
            {
                return $"Could not decode state ({state.Length} bytes): {e.Message}";
            }

            if (gameState == null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.AppendLine($"===== State | frame={gameState.Frame} | {state.Length} bytes =====");

            Append(sb, gameState, "", maxDepth <= 0 ? MaxDepth : maxDepth, 0);

            return sb.ToString();
        }

        private static void Append(StringBuilder sb, object value, string indent, int maxDepth, int depth)
        {
            foreach (var member in Members(value.GetType()))
            {
                object memberValue;

                try
                {
                    memberValue = Read(member, value);
                }
                catch (Exception e)
                {
                    sb.AppendLine($"{indent}{member.Name} = <threw {e.GetType().Name}>");
                    continue;
                }

                AppendMember(sb, member.Name, memberValue, indent, maxDepth, depth);
            }
        }

        private static void AppendMember(StringBuilder sb, string name, object value, string indent,
                                         int maxDepth, int depth)
        {
            if (IsScalar(value))
            {
                sb.AppendLine($"{indent}{name} = {Scalar(value)}");
                return;
            }

            if (depth >= maxDepth)
            {
                sb.AppendLine($"{indent}{name} = <...>");
                return;
            }

            if (value is IEnumerable items)
            {
                AppendEnumerable(sb, name, items, indent, maxDepth, depth);
                return;
            }

            sb.AppendLine($"{indent}{name}:");
            Append(sb, value, indent + "  ", maxDepth, depth + 1);
        }

        private static void AppendEnumerable(StringBuilder sb, string name, IEnumerable items, string indent,
                                             int maxDepth, int depth)
        {
            var listed = 0;
            var body = new StringBuilder();

            foreach (var item in items)
            {
                if (listed == MaxItems)
                {
                    body.AppendLine($"{indent}  <{Remaining(items) - MaxItems} more>");
                    break;
                }

                AppendMember(body, $"[{listed}]", item, indent + "  ", maxDepth, depth + 1);
                listed++;
            }

            if (listed == 0)
            {
                sb.AppendLine($"{indent}{name} = []");
                return;
            }

            sb.AppendLine($"{indent}{name}:");
            sb.Append(body);
        }

        private static int Remaining(IEnumerable items)
        {
            if (items is ICollection collection)
            {
                return collection.Count;
            }

            var count = 0;

            foreach (var _ in items)
            {
                count++;
            }

            return count;
        }

        private static MemberInfo[] Members(Type type)
            => type.GetProperties(MemberFlags)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .Cast<MemberInfo>()
                .Concat(type.GetFields(MemberFlags))
                .OrderBy(member => member.Name, StringComparer.Ordinal)
                .ToArray();

        private static object Read(MemberInfo member, object owner)
            => member is PropertyInfo property
                ? property.GetValue(owner)
                : ((FieldInfo)member).GetValue(owner);

        private static bool IsScalar(object value)
            => value == null
               || value is string
               || value is decimal
               || value.GetType().IsPrimitive
               || value.GetType().IsEnum;

        private static string Scalar(object value)
            => value switch
            {
                null => "null",
                string text => $"\"{text}\"",
                IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString(),
            };
    }
}
