using System.Runtime.InteropServices;
using TF.EX.Common.Handle;

namespace TF.EX.Domain.Externals
{
    public class MatchboxClientFFI
    {
        [DllImport("matchbox_client_ffi.dll")]
        public static extern void initialize(string room_url);

        [DllImport("matchbox_client_ffi.dll")]
        public static extern void disconnect();

        [DllImport("matchbox_client_ffi.dll")]
        public static extern void send_message(string toSend, string peerId);

        [DllImport("matchbox_client_ffi.dll")]
        public static extern SafeBytesFFI poll_message();

        [DllImport("matchbox_client_ffi.dll")]
        public static extern void free_messages(SafeBytesFFI toFree);
    }
}
