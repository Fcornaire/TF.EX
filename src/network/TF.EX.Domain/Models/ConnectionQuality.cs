using Microsoft.Xna.Framework;

namespace TF.EX.Domain.Models
{
    public enum ConnectionQuality
    {
        Good,
        Ok,
        Bad,
    }

    public static class ConnectionQualities
    {
        private const int MIN_GRADED_SAMPLES = 12;

        public static ConnectionQuality GetQuality(PingStats stats)
        {
            if (stats.rtt < 0)
            {
                return ConnectionQuality.Bad;
            }

            var latency = GetQualityByLatency(stats.rtt);

            if (stats.samples < MIN_GRADED_SAMPLES)
            {
                return latency;
            }

            return GetWorst(latency, GetWorst(GetQualityByGrade(ToActualDelayInFrames(stats.spike), 1, 3), GetQualityByGrade(stats.loss_percent, 5, 15)));
        }

        public static ConnectionQuality GetQualityByLatency(int rttMs) => GetQualityByGrade(ToActualDelayInFrames(rttMs), 2, 4);

        public static Color ToColor(this ConnectionQuality quality)
        {
            return quality switch
            {
                ConnectionQuality.Good => Color.LightGreen,
                ConnectionQuality.Ok => Color.Yellow,
                _ => Color.Red,
            };
        }

        private static double ToActualDelayInFrames(int rttMs) => rttMs / 2.0 / NetplayPreferences.RollbackFrameMs;

        private static ConnectionQuality GetQualityByGrade(double value, double maxGood, double maxOk)
        {
            if (value <= maxGood)
            {
                return ConnectionQuality.Good;
            }

            return value <= maxOk ? ConnectionQuality.Ok : ConnectionQuality.Bad;
        }

        private static ConnectionQuality GetWorst(ConnectionQuality a, ConnectionQuality b) => a > b ? a : b;
    }
}
