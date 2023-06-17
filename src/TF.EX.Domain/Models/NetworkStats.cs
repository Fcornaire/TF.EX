using System.Runtime.InteropServices;

namespace TF.EX.Domain.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NetworkStats
    {
        public uint send_queue_len;
        public uint ping;
        public uint kbps_sent;
        public int local_frames_behind;
        public int remote_frames_behind;
    }
}
