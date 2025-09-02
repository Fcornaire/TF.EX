using System.Runtime.InteropServices;
using TF.EX.Common.Handle;
using TF.EX.Domain.Models;

namespace TF.EX.Domain.Externals
{
    public class GGRSFFI
    {
        public static bool IsInInit = false;

        [DllImport("ggrs_ffi")]
        public static extern Status netplay_init(SafeBytesFFI netplay_conf);

        [DllImport("ggrs_ffi")]
        public static extern Status netplay_poll();

        [DllImport("ggrs_ffi")]
        public static extern Status netplay_is_synchronized();

        [DllImport("ggrs_ffi")]
        public static extern Status netplay_is_disconnected();

        [DllImport("ggrs_ffi")]
        public static extern void status_info_free(IntPtr info);

        [DllImport("ggrs_ffi")]
        public static extern Events netplay_events();

        [DllImport("ggrs_ffi")]
        public static extern void netplay_events_free(Events events);

        [DllImport("ggrs_ffi")]
        public static extern Status netplay_advance_frame(Input input);

        [DllImport("ggrs_ffi")]
        public static extern NetplayRequets netplay_get_requests();

        [DllImport("ggrs_ffi")]
        public static extern void netplay_requests_free(NetplayRequets requests);

        [DllImport("ggrs_ffi")]
        public static extern Status netplay_save_game_state(SafeBytesFFI gameState);

        [DllImport("ggrs_ffi")]
        public static extern Inputs netplay_advance_game_state();

        [DllImport("ggrs_ffi")]
        public static extern ActionResult netplay_load_game_state();

        [DllImport("ggrs_ffi")]
        public static extern void netplay_inputs_free(Inputs inputs);

        [DllImport("ggrs_ffi")]
        public static extern Status netplay_network_stats(IntPtr stats);

        [DllImport("ggrs_ffi")]
        public static extern int netplay_frames_ahead();

        [DllImport("ggrs_ffi")]
        public static extern void netplay_free_game_state(SafeBytesFFI safeByte);

        [DllImport("ggrs_ffi")]
        public static extern int netplay_current_frame();

        [DllImport("ggrs_ffi")]
        public static extern Status netplay_reset();

        [DllImport("ggrs_ffi")]
        public static extern int netplay_local_player_handle();

        [DllImport("ggrs_ffi")]
        public static extern int netplay_remote_player_handle();
    }
}
