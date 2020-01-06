using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace NWayland.Core
{
    public class NWaylandMarshalledString : SafeHandle
    {
        private GCHandle _gcHandle;
        private byte[] _data;

        public NWaylandMarshalledString(string s) : base(IntPtr.Zero, true)
        {
            if (s == null)
                return;
            var len = Encoding.UTF8.GetByteCount(s);
            _data = ArrayPool<byte>.Shared.Rent(len + 1);
            Encoding.UTF8.GetBytes(s, 0, s.Length, _data, 0);
            _data[len] = 0;
            _gcHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            handle = _gcHandle.AddrOfPinnedObject();
        }

        public int ByteLen => _data.Length;

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                handle = IntPtr.Zero;
                if (_data != null)
                    ArrayPool<byte>.Shared.Return(_data);
                _data = null;
                _gcHandle.Free();
            }

            return true;
        }

        public override bool IsInvalid => false;

    }

}