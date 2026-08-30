using Microsoft.Xna.Framework.Input;
using Monocle;
using TF.Replay.Domain.CustomComponent;
using TF.Replay.Domain.Extensions;
using TF.Replay.Domain.Ports;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class Takeover
    {
        public enum Phase { Off, Countdown, Active, Done }

        private const double CountdownSeconds = 2.0;
        private const int ResultsGraceFrames = 100;
        private const int MinimumFrames = 300;
        private const Keys TriggerKey = Keys.T;
        private const Buttons TriggerButton = Buttons.Start;

        public static Phase State { get; private set; }
        public static int Seat { get; private set; }
        public static int StartFrame { get; private set; }

        private static long _countdownEnd;

        public static int CountdownSecondsLeft => 
            (int)Math.Ceiling(Math.Max(0, _countdownEnd - System.Diagnostics.Stopwatch.GetTimestamp()) / (double)System.Diagnostics.Stopwatch.Frequency);

        private static int _padIndex = -1;
        private static PlayerInput[] _seatsBefore;
        private static TakeoverDonePrompt _prompt;
        private static PlayerInput _device;
        private static int[] _liveFlat;
        private static int _activeTicks;
        private static int _recordedEndTick = -1;

        public static InputState LiveInput { get; private set; }

        public static bool IsCapturing { get; private set; }

        public static bool IsLive => State == Phase.Active;

        public static bool SuppressesPlaybackChecks => State is Phase.Active or Phase.Done;

        public static void Reset()
        {
            RestoreReplaySeats();

            State = Phase.Off;
            Seat = -1;
            StartFrame = 0;
            _countdownEnd = 0;
            _padIndex = -1;
            _seatsBefore = null;
            _prompt = null;
            _device = null;
            _liveFlat = null;
            _activeTicks = 0;
            _recordedEndTick = -1;
            LiveInput = new InputState();
            IsCapturing = false;
        }

        public static bool HandleControls(IReplayService service)
        {
            switch (State)
            {
                case Phase.Off:
                    HandleSeatCycling(service);
                    return HandleTrigger(service);

                case Phase.Countdown:
                    if (System.Diagnostics.Stopwatch.GetTimestamp() < _countdownEnd)
                    {
                        return true;
                    }

                    Begin(service);
                    return false;

                case Phase.Active:
                    CaptureLiveInput();
                    _activeTicks++;

                    if (TFGame.Instance?.Scene is Level level && ResultsShown(level))
                    {
                        OnRoundEnded();

                        return false;
                    }

                    ProbeRecordedEnd(service);

                    var tickScale = Math.Max(1, (service?.GetReplay()?.Informations?.TickRateOrLegacy ?? Models.ReplayInfo.LegacyTickRate) / 60);

                    if (_recordedEndTick >= 0
                        && _activeTicks >= Math.Max(_recordedEndTick + ResultsGraceFrames * tickScale, MinimumFrames * tickScale))
                    {
                        OnRoundEnded();
                    }

                    return false;

                case Phase.Done:
                    return true;

                default:
                    return false;
            }
        }

        public static bool StopPressed() => State == Phase.Active && TriggerPressed(out _);

        private static bool ResultsShown(Level level) => level.Get<VersusMatchResults>() != null || level.Get<VersusRoundResults>() != null;

        private static void ProbeRecordedEnd(IReplayService service)
        {
            if (_recordedEndTick >= 0 || service == null)
            {
                return;
            }

            if (service.PlaybackFrame >= service.LastFrame)
            {
                _recordedEndTick = _activeTicks;
                return;
            }

            var state = service.GetRecordAt(service.PlaybackFrame)?.State;
            var stateApi = ServiceCollections.ResolveStateApi();

            if (state == null || stateApi == null)
            {
                return;
            }

            try
            {
                if (stateApi.IsRoundResultsShown(state) || !stateApi.IsRoundStarted(state))
                {
                    _recordedEndTick = _activeTicks;
                }
            }
            catch
            {
            }
        }

        public static void OnRoundEnded()
        {
            if (State != Phase.Active)
            {
                return;
            }

            State = Phase.Done;

            if (TFGame.Instance?.Scene is Level level)
            {
                level.Add(_prompt = new TakeoverDonePrompt());
            }
        }

        public static void Retry()
        {
            RemovePrompt();
            InputDisplayerOverlay.ReloadSession();
            _activeTicks = 0;
            _recordedEndTick = -1;
            State = Phase.Active;
            Sounds.ui_clickSpecial.Play();
        }

        public static void ContinueReplay()
        {
            RemovePrompt();
            RestoreReplaySeats();
            InputDisplayerOverlay.ReloadSession();
            State = Phase.Off;
            Sounds.ui_click.Play();
        }

        public static bool PadPressed(Buttons button)
        {
            for (int i = 0; i < MInput.XGamepads.Count; i++)
            {
                if (MInput.XGamepads[i].Attached && MInput.XGamepads[i].Pressed(button))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemovePrompt()
        {
            if (_prompt?.Scene != null)
            {
                _prompt.RemoveSelf();
            }

            _prompt = null;
        }

        public static void RestoreReplaySeats()
        {
            if (_seatsBefore != null)
            {
                TFGame.PlayerInputs = _seatsBefore;
                _seatsBefore = null;
            }
        }

        private static bool HandleTrigger(IReplayService service)
        {
            if (service == null || !service.IsPlayback || TFGame.Instance?.Scene is not Level level)
            {
                return false;
            }

            if (!TriggerPressed(out var padIndex))
            {
                return false;
            }

            var refusal = StartRefusal(service, level);

            if (refusal != null)
            {
                Sounds.ui_invalid.Play();
                ServiceCollections.Notify(refusal);
                return false;
            }

            _padIndex = padIndex;
            Seat = ValidSeat();
            _countdownEnd = System.Diagnostics.Stopwatch.GetTimestamp()
                            + (long)(CountdownSeconds * System.Diagnostics.Stopwatch.Frequency);
            State = Phase.Countdown;
            Sounds.ui_click.Play();

            return true;
        }

        private static string StartRefusal(IReplayService service, Level level)
        {
            if ((Modes)(service.GetReplay()?.Informations?.Mode ?? -1) == Modes.Trials)
            {
                return "TAKEOVER IS FOR VERSUS REPLAYS ONLY";
            }

            if (service.SeekBlockedBy != null)
            {
                return $"NO TAKEOVER: {service.SeekBlockedBy} SAVES NO STATE".ToUpperInvariant();
            }

            if (service.PlaybackFrame > service.LastFrame)
            {
                return "THE REPLAY IS OVER";
            }

            if (!(level.Session?.RoundLogic?.RoundStarted ?? false) || AtRoundEnd(level))
            {
                return "TAKEOVER MUST START AFTER ROUND STARTED";
            }

            return null;
        }

        private static bool AtRoundEnd(Level level) => level.Get<VersusRoundResults>() != null || level.Get<VersusMatchResults>() != null;

        private static bool TriggerPressed(out int padIndex)
        {
            padIndex = -1;

            if (MInput.Keyboard.Pressed(TriggerKey))
            {
                return true;
            }

            for (int i = 0; i < MInput.XGamepads.Count; i++)
            {
                if (MInput.XGamepads[i].Attached && MInput.XGamepads[i].Pressed(TriggerButton))
                {
                    padIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static int DefaultSeat()
        {
            for (int seat = 0; seat < TFGame.Players.Length; seat++)
            {
                if (TFGame.Players[seat])
                {
                    return seat;
                }
            }

            return 0;
        }

        public static int DisplaySeat => ValidSeat();

        private static int ValidSeat() => Seat >= 0 && Seat < TFGame.Players.Length && TFGame.Players[Seat] ? Seat : DefaultSeat();

        public static void CycleSeat(int direction)
        {
            var current = ValidSeat();
            var count = TFGame.Players.Length;

            for (int step = 1; step < count; step++)
            {
                var candidate = ((current + direction * step) % count + count) % count;

                if (TFGame.Players[candidate])
                {
                    Seat = candidate;
                    Sounds.ui_click.Play();
                    return;
                }
            }
        }

        private static void HandleSeatCycling(IReplayService service)
        {
            if (!PickerVisible(service))
            {
                return;
            }

            for (int i = 0; i < MInput.XGamepads.Count; i++)
            {
                if (!MInput.XGamepads[i].Attached)
                {
                    continue;
                }

                if (MInput.XGamepads[i].Pressed(Buttons.LeftTrigger))
                {
                    CycleSeat(-1);
                }

                if (MInput.XGamepads[i].Pressed(Buttons.RightTrigger))
                {
                    CycleSeat(1);
                }
            }
        }

        public static bool PickerVisible(IReplayService service)
            => State == Phase.Off
               && service?.IsPlayback == true
               && service.SeekBlockedBy == null
               && service.PlaybackFrame <= service.LastFrame
               && (Modes)(service.GetReplay()?.Informations?.Mode ?? -1) != Modes.Trials;

        private static void Begin(IReplayService service)
        {
            StartFrame = service.PlaybackFrame;
            _seatsBefore = (PlayerInput[])TFGame.PlayerInputs.Clone();

            var device = ResolveDevice();

            for (int seat = 0; seat < TFGame.PlayerInputs.Length; seat++)
            {
                if (!ReferenceEquals(TFGame.PlayerInputs[seat], device))
                {
                    continue;
                }

                TFGame.PlayerInputs[seat] = StandalonePlayback.IsActive
                    ? new PlaybackController(seat)
                    : new KeyboardInput();
            }

            TFGame.PlayerInputs[Seat] = device;
            _device = device;
            _activeTicks = 0;
            _recordedEndTick = -1;
            State = Phase.Active;
            Sounds.ui_clickSpecial.Play();

            CaptureLiveInput();
        }

        private static void CaptureLiveInput()
        {
            if (_device == null)
            {
                return;
            }

            IsCapturing = true;

            try
            {
                LiveInput = _device.GetState();
            }
            finally
            {
                IsCapturing = false;
            }

            _liveFlat ??= new int[Models.InputCodec.Stride];
            _liveFlat[Models.InputCodec.JumpCheck] = LiveInput.JumpCheck ? 1 : 0;
            _liveFlat[Models.InputCodec.JumpPressed] = LiveInput.JumpPressed ? 1 : 0;
            _liveFlat[Models.InputCodec.ShootCheck] = LiveInput.ShootCheck ? 1 : 0;
            _liveFlat[Models.InputCodec.ShootPressed] = LiveInput.ShootPressed ? 1 : 0;
            _liveFlat[Models.InputCodec.AltShootCheck] = LiveInput.AltShootCheck ? 1 : 0;
            _liveFlat[Models.InputCodec.AltShootPressed] = LiveInput.AltShootPressed ? 1 : 0;
            _liveFlat[Models.InputCodec.DodgeCheck] = LiveInput.DodgeCheck ? 1 : 0;
            _liveFlat[Models.InputCodec.DodgePressed] = LiveInput.DodgePressed ? 1 : 0;
            _liveFlat[Models.InputCodec.ArrowPressed] = LiveInput.ArrowsPressed ? 1 : 0;
            _liveFlat[Models.InputCodec.MoveX] = LiveInput.MoveX;
            _liveFlat[Models.InputCodec.MoveY] = LiveInput.MoveY;
            _liveFlat[Models.InputCodec.AimX] = Models.InputCodec.FromFloat(LiveInput.AimAxis.X);
            _liveFlat[Models.InputCodec.AimY] = Models.InputCodec.FromFloat(LiveInput.AimAxis.Y);
        }

        public static int[] GetLiveInputFlat() => IsLive ? _liveFlat : null;

        private static PlayerInput ResolveDevice()
        {
            if (_padIndex < 0)
            {
                return TFGame.PlayerInputs.FirstOrDefault(input => input is KeyboardInput) ?? new KeyboardInput();
            }

            return TFGame.PlayerInputs.FirstOrDefault(input => input is XGamepadInput pad && pad.XGamepadIndex == _padIndex) ?? new XGamepadInput(_padIndex);
        }
    }
}
