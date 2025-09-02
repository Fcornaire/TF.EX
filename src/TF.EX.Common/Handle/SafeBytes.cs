using MessagePack;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Text;

namespace TF.EX.Common.Handle
{
    public class SafeBytes<T> : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly nuint size;
        private readonly Action cleanup;
        public SafeBytes(T obj, bool useJson)
            : base(true)
        {
            var bytes = ToBytes(obj, useJson);

            int length = bytes.Length;
            IntPtr ptr = Marshal.AllocHGlobal(length);
            Marshal.Copy(bytes, 0, ptr, length);

            SetHandle(ptr);
            this.size = (nuint)length;
            this.cleanup = () => { Marshal.FreeHGlobal(handle); };
        }

        public SafeBytes(SafeBytesFFI sb, Action cleanup)
           : base(true)
        {
            SetHandle(sb.ptr);
            this.size = sb.size;
            this.cleanup = cleanup;
        }

        public byte[] PtrToBytes()
        {
            if (handle == IntPtr.Zero || size == 0)
            {
                return new byte[0];
            }

            byte[] result = new byte[(int)size];
            Marshal.Copy(handle, result, 0, (int)size);

            return result;
        }

        public SafeBytesFFI ToBytesFFI()
        {
            return new SafeBytesFFI
            {
                ptr = handle,
                size = size,
            };
        }

        protected override bool ReleaseHandle()
        {
            if (cleanup != null)
            {
                cleanup();
            }
            return true;
        }

        private byte[] ToBytes(T obj, bool useJson)
        {
            var options = useJson ? SerializationOptions.GetContractlessOptions() : SerializationOptions.GetDefaultOptionWithCompression();

            var serialized = MessagePackSerializer.Serialize(obj, options);

            if (!useJson)
            {
                return serialized;
            }

            var json = MessagePackSerializer.ConvertToJson(serialized);
            var bytes = Encoding.UTF8.GetBytes(json);
            return bytes;
        }

        public T ToStruct(bool useJson)
        {
            byte[] rawBytes = PtrToBytes();

            if (rawBytes.Length == 0)
            {
                return default;
            }

            if (useJson)
            {
                var json = Encoding.UTF8.GetString(rawBytes);
                var bytes = MessagePackSerializer.ConvertFromJson(json, SerializationOptions.GetContractlessOptions());
                var result = MessagePackSerializer.Deserialize<T>(bytes, SerializationOptions.GetContractlessOptions());
                return result;
            }

            return MessagePackSerializer.Deserialize<T>(rawBytes, SerializationOptions.GetDefaultOptionWithCompression());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SafeBytesFFI
    {
        public IntPtr ptr;
        public nuint size;
    }
}
