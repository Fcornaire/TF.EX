using System.Runtime.InteropServices;

namespace TF.EX.Domain.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PingStats
    {
        public int rtt;
        public int spike;
        public int loss_percent;
        public int samples;
    }
}
