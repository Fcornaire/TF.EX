using DeepEqual.Syntax;
using MessagePack;
using System.Collections;
using System.Text;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;

namespace TF.EX.Domain.Services
{
    internal class SyncTestUtilsService : ISyncTestUtilsService
    {
        ICollection<(int frame, byte[] bytes)> frames_gameStates = new List<(int, byte[])>();

        private static readonly MessagePackSerializerOptions Options =
            global::TF.EX.Common.SerializationOptions.GetDefaultOptionWithCompression();

        public void AddFrame(int frame, GameState gs)
        {
            var bytes = MessagePackSerializer.Serialize(gs, Options);
            frames_gameStates.Add((frame, bytes));
        }

        public string Compare(int frame)
        {
            StringBuilder msgBuilder = new StringBuilder();

            var gs = frames_gameStates.Where(x => x.frame == frame).ToList();

            for (var i = 0; i < gs.Count - 1; i++)
            {
                var bytesA = gs[i].bytes;
                var bytesB = gs[i + 1].bytes;

                var a = MessagePackSerializer.Deserialize<GameState>(bytesA, Options);
                var b = MessagePackSerializer.Deserialize<GameState>(bytesB, Options);

                bool deepEqualDiff = false;
                try
                {
                    a.ShouldDeepEqual(b);
                }
                catch (DeepEqual.Syntax.DeepEqualException e)
                {
                    deepEqualDiff = true;
                    msgBuilder.Append($"Diff at frame {frame} : {e.Message} \n");
                }

                if (!deepEqualDiff && !bytesA.AsSpan().SequenceEqual(bytesB))
                {
                    msgBuilder.Append(LocalizeByteDiff(frame, a, b, bytesA.Length, bytesB.Length));
                }
            }

            return msgBuilder.ToString();
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

        public void Remove(int fromFrame)
        {
            frames_gameStates = frames_gameStates.Where(x => x.frame >= fromFrame).ToList();
        }

        public void Reset()
        {
            frames_gameStates = new List<(int, byte[])>();
        }
    }
}
