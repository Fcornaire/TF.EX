using Microsoft.Win32.SafeHandles;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace TF.EX.Common.Handle
{
    public class SafeBytes<T> : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly int size;
        private readonly Action cleanup;
        public SafeBytes(T obj, bool useCompression)
            : base(true)
        {
            var bytes = ToBytes(obj, useCompression);

            int length = bytes.Length;
            IntPtr ptr = Marshal.AllocHGlobal(length);
            Marshal.Copy(bytes, 0, ptr, length);

            SetHandle(ptr);
            this.size = length;
            this.cleanup = () => { Marshal.FreeHGlobal(handle); };
        }

        public SafeBytes(SafeBytesFFI sb, Action cleanup)
           : base(true)
        {
            SetHandle(sb.ptr);
            this.size = sb.size;
            this.cleanup = cleanup;
        }

        public byte[] ToBytes()
        {
            if (handle == IntPtr.Zero || size == 0)
            {
                return new byte[0];
            }

            byte[] result = new byte[size];
            Marshal.Copy(handle, result, 0, size);

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

        private byte[] ToBytes(T obj, bool useCompression)
        {
            var json = JsonSerializer.Serialize(obj);
            var bytes = Encoding.UTF8.GetBytes(json);

            if (!useCompression)
            {
                return bytes;
            }

            var compressedBytes = Compress(bytes);
            return compressedBytes;
        }

        public T ToStruct(bool useDecompression)
        {
            byte[] bytes = ToBytes();

            if (useDecompression)
            {
                bytes = Decompress(bytes);
            }

            var json = Encoding.UTF8.GetString(bytes);

            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json);
        }

        private byte[] Compress(byte[] data)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }
        public byte[] Decompress(byte[] compressedData)
        {
            using (MemoryStream input = new MemoryStream(compressedData))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
                    {
                        gzip.CopyTo(output);
                    }
                    return output.ToArray();
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SafeBytesFFI
    {
        public IntPtr ptr;
        public int size;
    }
}
