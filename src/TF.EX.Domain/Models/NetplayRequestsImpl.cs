using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using TF.EX.Domain.Externals;

namespace TF.EX.Domain.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NetplayRequets
    {
        public IntPtr data;
        public int len;
    }

    public enum NetplayRequest
    {
        SaveGameState = 0,
        LoadGameState = 1,
        AdvanceFrame = 2,
    }

    public class NetplayRequestsImpl : IDisposable
    {
        public List<NetplayRequest> _requests { get; internal set; }

        public NetplayRequestsHandle Handle { get; internal set; }

        public NetplayRequestsImpl(NetplayRequets netplayRequets)
        {
            Handle = new NetplayRequestsHandle(netplayRequets);

            int[] requests = new int[netplayRequets.len];
            Marshal.Copy(netplayRequets.data, requests, 0, netplayRequets.len);

            _requests = requests.Select(req => req.ToModel()).ToList();
        }

        public void Dispose()
        {
            Handle.Dispose();
        }
    }

    public class NetplayRequestsHandle : SafeHandle
    {
        private readonly NetplayRequets _netplayRequets;
        public NetplayRequestsHandle(NetplayRequets netplayRequets) : base(IntPtr.Zero, true)
        {
            SetHandle(netplayRequets.data);
            _netplayRequets = netplayRequets;
        }

        public override bool IsInvalid
        {
            get { return this.handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
            {
                GGRSFFI.netplay_requests_free(_netplayRequets);
            }

            return true;
        }
    }

    public static class NeplayRequestExtension
    {
        public static NetplayRequestsImpl ToModel(this NetplayRequets reqs) => new NetplayRequestsImpl(reqs);

        public static NetplayRequest ToModel(this int npReq)
        {
            switch (npReq)
            {
                case 0: return NetplayRequest.SaveGameState;
                case 1: return NetplayRequest.LoadGameState;
                case 2: return NetplayRequest.AdvanceFrame;
                default: throw new NotImplementedException();
            }
        }

        public static string GetDescription(this Enum GenericEnum)
        {
            Type genericEnumType = GenericEnum.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((System.ComponentModel.DescriptionAttribute)_Attribs.ElementAt(0)).Description;
                }
            }
            return GenericEnum.ToString();
        }
    }
}
