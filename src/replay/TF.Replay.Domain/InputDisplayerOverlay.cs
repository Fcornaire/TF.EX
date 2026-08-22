using TF.Replay.Domain.Interop;
using TF.Replay.Domain.Models;
using TF.Replay.Domain.Ports;

namespace TF.Replay.Domain
{
    public static class InputDisplayerOverlay
    {
        private static IInputDisplayerApi _api;
        private static bool _resolved;
        private static object _session;
        private static int _takeoverFrame = -1;

        public static bool Render(IReplayService service)
        {
            var api = Api();

            if (api == null || !api.IsEnabled)
            {
                return false;
            }

            var replay = service?.GetReplay();

            if (replay == null || !service.IsPlayback)
            {
                Release(api);
                return false;
            }

            if (!ReferenceEquals(replay, _session))
            {
                Load(api, replay);
                _session = replay;
            }

            if (Takeover.IsLive)
            {
                _takeoverFrame = service.PlaybackFrame < service.LastFrame
                    ? service.PlaybackFrame
                    : Math.Max(_takeoverFrame + 1, service.PlaybackFrame);

                var live = Takeover.GetLiveInputFlat();

                if (live != null)
                {
                    api.PushSeat(_takeoverFrame, Takeover.Seat,
                        live[InputCodec.MoveX],
                        live[InputCodec.MoveY],
                        live[InputCodec.JumpCheck] != 0,
                        live[InputCodec.ShootCheck] != 0,
                        live[InputCodec.AltShootCheck] != 0,
                        live[InputCodec.DodgeCheck] != 0);
                }

                api.RenderAt(_takeoverFrame);

                return true;
            }

            _takeoverFrame = -1;

            api.RenderAt(service.PlaybackFrame);

            return true;
        }

        public static void ReloadSession() => _session = null;

        public static void Reset()
        {
            if (_api != null)
            {
                Release(_api);
            }

            _api = null;
            _resolved = false;
        }

        private static IInputDisplayerApi Api()
        {
            if (!_resolved)
            {
                _resolved = true;
                _api = ServiceCollections.ResolveModCollections()?.ResolveInputDisplayer();
            }

            return _api;
        }

        private static void Release(IInputDisplayerApi api)
        {
            if (_session == null)
            {
                return;
            }

            _session = null;
            api.EndSession();
        }

        private static void Load(IInputDisplayerApi api, Models.Replay replay)
        {
            var seats = 0;

            foreach (var record in replay.Record)
            {
                seats = Math.Max(seats, InputCodec.SeatCount(record.Inputs));
            }

            seats = Math.Min(seats, 4);

            api.BeginSession(seats);

            if (seats == 0)
            {
                return;
            }

            foreach (var record in replay.Record)
            {
                if (record.Inputs == null)
                {
                    continue;
                }

                for (int seat = 0; seat < seats; seat++)
                {
                    Push(api, record.Frame, seat, record.Inputs);
                }
            }
        }

        private static void Push(IInputDisplayerApi api, int frame, int seat, int[] inputs)
        {
            var at = InputCodec.Offset(seat);

            if (at + InputCodec.Stride > inputs.Length)
            {
                api.PushSeat(frame, seat, 0, 0, false, false, false, false);
                return;
            }

            api.PushSeat(frame, seat,
                inputs[at + InputCodec.MoveX],
                inputs[at + InputCodec.MoveY],
                inputs[at + InputCodec.JumpCheck] != 0,
                inputs[at + InputCodec.ShootCheck] != 0,
                inputs[at + InputCodec.AltShootCheck] != 0,
                inputs[at + InputCodec.DodgeCheck] != 0);
        }
    }
}
