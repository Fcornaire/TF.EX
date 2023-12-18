﻿using DeepEqual.Syntax;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using TF.EX.Domain.Services;
using Xunit;

namespace TF.EX.Utils
{
    public class ReplayComparer
    {
        private readonly string _replaysFolder = $"{Path.Combine(Environment.GetEnvironmentVariable("TFPath"), "Replays")}";


        [Fact]
        public async Task TestReplayComparison()
        {
            string replayFilePath1 = Path.Combine(_replaysFolder, "18-12-2023T21-39-17.tow"); //Weird desynch
            string replayFilePath2 = Path.Combine(_replaysFolder, "18-12-2023T21-39-17_gog.tow");

            List<TF.EX.Domain.Models.Record> record1 = (await ReplayService.ToReplay(replayFilePath1)).Record;
            List<TF.EX.Domain.Models.Record> record2 = (await ReplayService.ToReplay(replayFilePath2)).Record;

            ConcurrentDictionary<int, string> diff = new ConcurrentDictionary<int, string>();
            StringBuilder msgBuilder = new StringBuilder();
            Parallel.ForEach(Enumerable.Range(0, Math.Min(record1.Count(), record2.Count())), i =>
            {
                try
                {
                    record2[i].GameState.MatchStats = record2[i].GameState.MatchStats.Reverse();
                    record1[i].GameState.ShouldDeepEqual(record2[i].GameState);
                }
                catch (DeepEqual.Syntax.DeepEqualException e)
                {
                    diff.TryAdd(i, $"{msgBuilder}Diff at frame {i} : {e.Message} \n\n");
                }
            });

            var sorted = diff.ToImmutableSortedDictionary(diff => diff.Key, diff => diff.Value);

            foreach (var item in sorted)
            {
                msgBuilder.Append(item.Value);
            }

            string msg = msgBuilder.ToString();

            if (!string.IsNullOrEmpty(msg))
            {
                string diffFilePath = Path.Combine(_replaysFolder, "diff.txt");
                using StreamWriter sw = new StreamWriter(diffFilePath);
                sw.Write(msg);
            }
            else
            {
                string diffFilePath = Path.Combine(_replaysFolder, "diff.txt");
                using StreamWriter sw = new StreamWriter(diffFilePath);
                sw.Write("No diff!");
            }
        }

        [Theory]
        [InlineData(8854)]
        public async Task TestReplayComparisonAtSpecificFrame(int frame)
        {
            string replayFilePath1 = Path.Combine(_replaysFolder, "21-10-2023T19-41-50_gog.tow");
            string replayFilePath2 = Path.Combine(_replaysFolder, "21-10-2023T19-41-41_lap.tow");

            List<TF.EX.Domain.Models.Record> record1 = (await ReplayService.ToReplay(replayFilePath1)).Record;
            List<TF.EX.Domain.Models.Record> record2 = (await ReplayService.ToReplay(replayFilePath2)).Record;

            try
            {
                var rec = record1.ToList()[frame + 1];
                var rec2 = record2.ToList()[frame + 1];
                rec.ShouldDeepEqual(rec2);
            }
            catch (DeepEqual.Syntax.DeepEqualException e)
            {
                Console.WriteLine(e);
                string diffFilePath = Path.Combine(_replaysFolder, "diff_specific_frame.txt");
                File.WriteAllText(diffFilePath, $"Diff at frame {frame} : {e.Message} \n\n");
            }
        }
    }
}
