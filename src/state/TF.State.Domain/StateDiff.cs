using DeepEqual.Syntax;
using MessagePack;
using System.Collections;
using System.Text;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models;
using TF.State.Domain.Models.Entity.LevelEntity.Player;

namespace TF.State.Domain
{
    public static class StateDiff
    {
        private static readonly MessagePackSerializerOptions Options = StateSerialization.Options;

        public static bool IsRoundStarted(byte[] state)
        {
            if (state == null)
            {
                return false;
            }

            try
            {
                var gameState = MessagePackSerializer.Deserialize<GameState>(state, Options);

                if (gameState == null)
                {
                    return false;
                }

                return (gameState.Session?.RoundStarted ?? false) || (gameState.Entities?.Hud?.VersusStart?.CoroutineState ?? 0) == 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsRoundResultsShown(byte[] state)
        {
            if (state == null)
            {
                return false;
            }

            try
            {
                var gameState = MessagePackSerializer.Deserialize<GameState>(state, Options);

                return (gameState?.Entities?.Hud?.VersusRoundResults?.CoroutineState ?? 0) > 0;
            }
            catch
            {
                return false;
            }
        }

        public static int GetIntroCoroutineState(byte[] state)
        {
            if (state == null)
            {
                return 0;
            }

            try
            {
                return MessagePackSerializer.Deserialize<GameState>(state, Options)
                    ?.Entities?.Hud?.VersusStart?.CoroutineState ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public static string Compare(byte[] stateA, byte[] stateB)
        {
            if (stateA == null || stateB == null)
            {
                return "";
            }

            var a = MessagePackSerializer.Deserialize<GameState>(stateA, Options);
            var b = MessagePackSerializer.Deserialize<GameState>(stateB, Options);

            var msgBuilder = new StringBuilder();
            var deepEqualDiff = false;

            try
            {
                a.ShouldDeepEqual(b);
            }
            catch (DeepEqualException e)
            {
                deepEqualDiff = true;
                msgBuilder.Append($"Diff at frame {a.Frame} : {e.Message} \n");
            }

            if (!deepEqualDiff && !stateA.AsSpan().SequenceEqual(stateB))
            {
                msgBuilder.Append(LocalizeByteDiff(a.Frame, a, b, stateA.Length, stateB.Length));
            }

            return msgBuilder.ToString();
        }

        public static string[] DescribePlayers(byte[] state)
        {
            if (state == null)
            {
                return new string[0];
            }

            var gs = MessagePackSerializer.Deserialize<GameState>(state, Options);
            var players = gs.Entities?.Players;

            if (players == null)
            {
                return new string[0];
            }

            return players.Select(player => string.Join(";",
                player.Index.ToString(),
                player.Position.ToTFVector().ToString(),
                player.Speed.ToTFVector().ToString(),
                player.State.CurrentState.ToTFModel().ToString())).ToArray();
        }


        public const string KindEqual = "equal";
        public const string KindRenderDerived = "render-derived";
        public const string KindEncodingOnly = "encoding-only";
        public const string KindDivergent = "divergent";

        public static string[] Classify(byte[] liveState, byte[] recordedState)
        {
            var live = MessagePackSerializer.Deserialize<GameState>(liveState, Options);
            var recorded = MessagePackSerializer.Deserialize<GameState>(recordedState, Options);
            var round = recorded.Session?.RoundIndex.ToString() ?? "";

            try
            {
                live.ShouldDeepEqual(recorded);
            }
            catch (DeepEqualException e)
            {
                var detail = e.Message;

                return IsRenderDerived(detail)
                    ? [KindRenderDerived, detail, round]
                    : [KindDivergent, detail, round];
            }

            if (!liveState.AsSpan().SequenceEqual(recordedState))
            {
                return new[] { KindEncodingOnly, LocalizeEncodingDiff(live, recorded, liveState, recordedState), round };
            }

            return new[] { KindEqual, "", round };
        }

        public static bool MatchesWithFrame(byte[] candidate, byte[] liveState, int frame)
        {
            if (candidate == null || liveState == null)
            {
                return false;
            }

            var state = MessagePackSerializer.Deserialize<GameState>(candidate, Options);
            state.Frame = frame;

            return MessagePackSerializer.Serialize(state, Options).AsSpan().SequenceEqual(liveState);
        }

        private static bool IsRenderDerived(string differences)
        {
            var lines = differences.Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("Actual.", StringComparison.Ordinal))
                .ToList();

            return lines.Count > 0 && lines.All(line => line.Contains(".Animations.", StringComparison.Ordinal));
        }

        private static string LocalizeEncodingDiff(GameState live, GameState recorded, byte[] liveBytes, byte[] recordedBytes)
        {
            var report = new StringBuilder();

            foreach (var property in typeof(GameState).GetProperties())
            {
                byte[] liveProperty, recordedProperty;

                try
                {
                    liveProperty = MessagePackSerializer.Serialize(property.PropertyType, property.GetValue(live), Options);
                    recordedProperty = MessagePackSerializer.Serialize(property.PropertyType, property.GetValue(recorded), Options);
                }
                catch
                {
                    continue;
                }

                if (!liveProperty.AsSpan().SequenceEqual(recordedProperty))
                {
                    report.Append($" '{property.Name}' encodes differently.");
                }
            }

            AppendKeyOrder(report, "Layer.GameplayLayerActualDepthLookup",
                live.Layer?.GameplayLayerActualDepthLookup, recorded.Layer?.GameplayLayerActualDepthLookup);
            AppendKeyOrder(report, "Entities.RoundData", live.Entities?.RoundData, recorded.Entities?.RoundData);
            AppendKeyOrder(report, "AdditionnalData", live.AdditionnalData, recorded.AdditionnalData);

            if (liveBytes.Length != recordedBytes.Length)
            {
                report.Append($" Lengths differ ({liveBytes.Length} vs {recordedBytes.Length}).");
            }

            return report.Length == 0 ? " Could not localize it." : report.ToString();
        }

        private static void AppendKeyOrder<TKey, TValue>(StringBuilder report, string name,
            Dictionary<TKey, TValue> live, Dictionary<TKey, TValue> recorded)
        {
            if (live == null || recorded == null)
            {
                return;
            }

            var liveKeys = string.Join(",", live.Keys);
            var recordedKeys = string.Join(",", recorded.Keys);

            if (liveKeys != recordedKeys)
            {
                report.Append($" {name} KEY ORDER differs: live [{liveKeys}] vs recorded [{recordedKeys}].");
            }
        }

        private static string LocalizeByteDiff(int frame, GameState a, GameState b, int lenA, int lenB)
        {
            var sb = new StringBuilder();
            sb.Append($"Checksum-only diff at frame {frame}: DeepEqual sees the states as equal, " +
                      $"but the serialized bytes differ (len {lenA} vs {lenB})\n");

            foreach (var prop in typeof(GameState).GetProperties())
            {
                byte[] pa, pb;
                try
                {
                    pa = MessagePackSerializer.Serialize(prop.PropertyType, prop.GetValue(a), Options);
                    pb = MessagePackSerializer.Serialize(prop.PropertyType, prop.GetValue(b), Options);
                }
                catch
                {
                    continue;
                }

                if (!pa.AsSpan().SequenceEqual(pb))
                {
                    sb.Append($"  -> property '{prop.Name}' serializes differently.\n");
                }
            }

            AppendDictOrder(sb, "Layer.GameplayLayerActualDepthLookup",
                a.Layer?.GameplayLayerActualDepthLookup, b.Layer?.GameplayLayerActualDepthLookup);
            AppendDictOrder(sb, "AdditionnalData", a.AdditionnalData, b.AdditionnalData);

            return sb.ToString();
        }

        private static void AppendDictOrder(StringBuilder sb, string name, IDictionary a, IDictionary b)
        {
            if (a == null || b == null)
            {
                return;
            }

            var keysA = string.Join(",", a.Keys.Cast<object>());
            var keysB = string.Join(",", b.Keys.Cast<object>());

            if (keysA != keysB)
            {
                sb.Append($"  -> {name} key ORDER differs\n");
                sb.Append($"       A: [{keysA}]\n");
                sb.Append($"       B: [{keysB}]\n");
            }
        }
    }
}
