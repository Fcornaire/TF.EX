using System.Runtime.InteropServices;
using System.Text;
using TF.EX.Domain.Extensions;

namespace TF.EX.Domain.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Status
    {
        public short is_ok;
        public IntPtr info;
    }

    public class StatusImpl : IDisposable
    {
        public bool IsOk { get; internal set; }
        public InfoHandle Info { get; internal set; }

        public StatusImpl(Status status, Action<IntPtr> free)
        {
            IsOk = status.is_ok.ToStatusBoolModel();
            Info = new InfoHandle(status.info, free);
        }
        public void Dispose()
        {
            Info.Dispose();
        }
    }

    public class InfoHandle : SafeHandle
    {
        private Action<IntPtr> _free;
        public InfoHandle(IntPtr handle, Action<IntPtr> free) : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
            _free = free;
        }

        public override bool IsInvalid
        {
            get { return this.handle == IntPtr.Zero; }
        }

        public string AsString()
        {
            int len = 0;
            while (Marshal.ReadByte(handle, len) != 0) { ++len; }
            byte[] buffer = new byte[len];
            Marshal.Copy(handle, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        protected override bool ReleaseHandle()
        {
            //TODO: huh ?
            if (!this.IsInvalid)
            {
                _free(handle);
            }

            return true;
        }
    }
}
