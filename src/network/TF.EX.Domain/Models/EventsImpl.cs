using System;
using System.Runtime.InteropServices;
using TF.EX.Domain.Externals;

namespace TF.EX.Domain.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Events
    {
        public IntPtr data;
        public int len;
        public int cap;
    }

    public class EventsImpl : IDisposable
    {
        public string[] _events { get; internal set; }

        public EventsHandle Handle { get; internal set; }

        public EventsImpl(Events events)
        {
            Handle = new EventsHandle(events);

            IntPtr[] c_strings = new IntPtr[events.len];
            Marshal.Copy(events.data, c_strings, 0, events.len);
            _events = new string[events.len];
            for (int i = 0; i < events.len; i++)
            {
                _events[i] = Marshal.PtrToStringAnsi(c_strings[i]);
            }
        }

        public void Dispose()
        {
            Handle.Dispose();
        }
    }

    public enum Event
    {
        Synchronizing = 0,
        Synchronized = 1,
        Disconnected = 2,
        NetworkInterrupted = 3,
        WaitRecommendation = 4,
        NetworkResumed = 5,
        DesyncDetected = 6,
    }

    public static class EventsStructExtension
    {
        public static EventsImpl ToModel(this Events eventStruct) => new EventsImpl(eventStruct);

    }


    public static class EventsExtension
    {
        public static Event ToModel(this string evt)
        {
            return (Event)Enum.Parse(typeof(Event), evt, true);
        }

    }


    public class EventsHandle : SafeHandle
    {
        private Events _events;
        public EventsHandle(Events events) : base(IntPtr.Zero, true)
        {
            SetHandle(events.data);
            _events = events;
        }

        public override bool IsInvalid
        {
            get { return this.handle == IntPtr.Zero; }
        }


        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
            {
                GGRSFFI.netplay_events_free(_events);
            }

            return true;
        }
    }
}
