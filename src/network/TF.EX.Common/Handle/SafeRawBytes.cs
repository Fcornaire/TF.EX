using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace TF.EX.Common.Handle
{
    //Same as SafeBytes<T> but for blobs that are ALREADY encoded
    public class SafeRawBytes : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly nuint size;
        private readonly Action cleanup;

        public SafeRawBytes(byte[] bytes)
            : base(true)
        {
            var length = bytes?.Length ?? 0;
            var ptr = Marshal.AllocHGlobal(Math.Max(length, 1));

            if (length > 0)
            {
                Marshal.Copy(bytes, 0, ptr, length);
            }

            SetHandle(ptr);
            size = (nuint)length;
            cleanup = () => { Marshal.FreeHGlobal(handle); };
        }

        public SafeRawBytes(SafeBytesFFI sb, Action cleanup)
            : base(true)
        {
            SetHandle(sb.ptr);
            size = sb.size;
            this.cleanup = cleanup;
        }

        public byte[] PtrToBytes()
        {
            if (handle == IntPtr.Zero || size == 0)
            {
                return new byte[0];
            }

            var result = new byte[(int)size];
            Marshal.Copy(handle, result, 0, (int)size);

            return result;
        }

        public SafeBytesFFI ToBytesFFI() => new SafeBytesFFI { ptr = handle, size = size };

        protected override bool ReleaseHandle()
        {
            cleanup?.Invoke();
            return true;
        }
    }
}
