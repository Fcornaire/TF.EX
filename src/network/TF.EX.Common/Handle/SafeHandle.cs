namespace TF.EX.Common.Handle
{
    using System;
    using System.Runtime.InteropServices;

    public class SafeHandle<T> : IDisposable where T : struct
    {
        private IntPtr _ptr;
        private GCHandle _handle;

        public SafeHandle(T data)
        {
            _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            _ptr = _handle.AddrOfPinnedObject();
        }

        public IntPtr Ptr
        {
            get { return _ptr; }
        }

        public T Value
        {
            get { return (T)_handle.Target; }
        }

        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }
    }
}
