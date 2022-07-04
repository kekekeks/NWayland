using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace NWayland.Interop
{
    public class NWaylandMarshalledString : SafeHandle
    {
        private GCHandle _gcHandle;
        private byte[]? _data;

        public NWaylandMarshalledString(string? s) : base(IntPtr.Zero, true)
        {
            if (s is null)
                return;
            var len = Encoding.UTF8.GetByteCount(s);
            _data = ArrayPool<byte>.Shared.Rent(len + 1);
            Encoding.UTF8.GetBytes(s, 0, s.Length, _data, 0);
            _data[len] = 0;
            _gcHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            handle = _gcHandle.AddrOfPinnedObject();
        }

        public override bool IsInvalid => false;

        protected override bool ReleaseHandle()
        {
            if (handle == IntPtr.Zero)
                return true;
            handle = IntPtr.Zero;
            if (_data is not null)
                ArrayPool<byte>.Shared.Return(_data);
            _data = null;
            _gcHandle.Free();
            return true;
        }
    }
}
