using DeepEqual.Syntax;
using MessagePack;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using TF.Replay.Domain;
using TF.Replay.Domain.Models;
using TF.State.Domain;
using TF.State.Domain.Models;
using Xunit;
using Record = TF.Replay.Domain.Models.Record;

namespace TF.EX.Utils
{
    public class ReplayComparer
    {
        private readonly string _replaysFolder =
            Path.Combine(Environment.GetEnvironmentVariable("TFPath") ?? ".", "Replays");

        private static async Task<List<Record>> LoadRecords(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var replay = await MessagePackSerializer.DeserializeAsync<TF.Replay.Domain.Models.Replay>(
                stream, ReplaySerialization.Default);

            return replay.Record;
        }

        private static GameState Decode(Record record) =>
            MessagePackSerializer.Deserialize<GameState>(record.State, StateSerialization.Options);

        [Fact]
        public async Task TestReplayComparison()
        {
            string replayFilePath1 = Path.Combine(_replaysFolder, "06-09-2025T20-11-45.tow");
            string replayFilePath2 = Path.Combine(_replaysFolder, "06-09-2025T20-11-46.tow");

            if (!File.Exists(replayFilePath1) || !File.Exists(replayFilePath2))
            {
                return;
            }

            var record1 = await LoadRecords(replayFilePath1);
            var record2 = await LoadRecords(replayFilePath2);

            var diff = new ConcurrentDictionary<int, string>();
            var msgBuilder = new StringBuilder();

            Parallel.ForEach(Enumerable.Range(0, Math.Min(record1.Count, record2.Count)), i =>
            {
                try
                {
                    var a = Decode(record1[i]);
                    var b = Decode(record2[i]);

                    b.Session.Scores = a.Session.Scores;
                    b.Session.OldScores = a.Session.OldScores;

                    a.ShouldDeepEqual(b);
                }
                catch (DeepEqualException e)
                {
                    diff.TryAdd(i, $"Diff at frame {i} : {e.Message} \n\n");
                }
            });

            foreach (var item in diff.ToImmutableSortedDictionary(d => d.Key, d => d.Value))
            {
                msgBuilder.Append(item.Value);
            }

            var msg = msgBuilder.ToString();

            File.WriteAllText(Path.Combine(_replaysFolder, "diff.txt"),
                string.IsNullOrEmpty(msg) ? "No diff!" : msg);
        }
    }

    public class InputCodecTests
    {
        [Fact]
        public void AimAxesRoundTripBitExact()
        {
            var random = new Random(1234);

            for (int i = 0; i < 10_000; i++)
            {
                var value = (float)(random.NextDouble() * 2.0 - 1.0);

                Assert.Equal(value, InputCodec.ToFloat(InputCodec.FromFloat(value)));
            }
        }

        [Fact]
        public void SeatCountFollowsStride()
        {
            Assert.Equal(16, InputCodec.Stride);
            Assert.Equal(4, InputCodec.SeatCount(new int[InputCodec.Stride * 4]));
            Assert.Equal(0, InputCodec.SeatCount(null));
        }
    }
}
