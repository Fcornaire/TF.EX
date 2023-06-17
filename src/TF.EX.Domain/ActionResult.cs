using System.Runtime.InteropServices;
using TF.EX.Common.Handle;
using TF.EX.Domain.Models;

namespace TF.EX.Domain
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ActionResult
    {
        public SafeBytesFFI Data;
        public Status Status;
    }
}
