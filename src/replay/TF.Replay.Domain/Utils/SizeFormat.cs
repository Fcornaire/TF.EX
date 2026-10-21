using System.Globalization;

namespace TF.Replay.Domain.Utils
{
    public static class SizeFormat
    {
        private const double Kilobyte = 1024d;
        private const double Megabyte = Kilobyte * 1024d;
        private const double Gigabyte = Megabyte * 1024d;

        public static string Of(long bytes)
        {
            if (bytes >= Gigabyte)
            {
                return Format(bytes / Gigabyte, "0.00") + " GB";
            }

            if (bytes >= Megabyte)
            {
                return Format(bytes / Megabyte, "0.0") + " MB";
            }

            return Format(bytes / Kilobyte, "0") + " KB";
        }

        private static string Format(double value, string pattern) => value.ToString(pattern, CultureInfo.InvariantCulture);
    }
}
