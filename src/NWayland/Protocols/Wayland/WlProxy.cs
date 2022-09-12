using System;
using NWayland.Interop;

namespace NWayland.Protocols.Wayland
{
    public abstract unsafe class WlProxy : IDisposable
    {
        private readonly uint _id;

        private bool _disposed;

        protected WlProxy(IntPtr handle, int version)
        {
            Version = version;
            Handle = handle;
            if (this is WlDisplay)
                return;
            _id = LibWayland.RegisterProxy(this);
        }

        public int Version { get; }

        public IntPtr Handle { get; }

        protected abstract WlInterface* GetWlInterface();

        protected abstract void DispatchEvent(uint opcode, WlArgument* arguments);

        internal void DispatchEvent(uint opcode, ref WlMessage message, WlArgument* arguments)
        {
            // Sanity checks
            // TODO: trigger a warning or something if this happens for some weird reason
            var @interface = GetWlInterface();
            if (opcode >= @interface->EventCount)
                return;
            var protocolMsg = @interface->Events[opcode];
            if(!strcmp(protocolMsg.Name, message.Name))
                return;
            if(!strcmp(protocolMsg.Signature, message.Signature))
                return;
            DispatchEvent(opcode, arguments);
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
                return;
            LibWayland.UnregisterProxy(_id);
            LibWayland.wl_proxy_destroy(Handle);
            _disposed = true;
        }

        protected static T? FromNative<T>(IntPtr proxy) where T : WlProxy => proxy == IntPtr.Zero ? null : LibWayland.FindByNative(proxy) as T;

        private static bool strcmp(byte* left, byte* right)
        {
            for (var c = 0;; c++)
            {
                if (left[c] != right[c])
                    return false;
                if (left[c] == 0)
                    return true;
            }
        }
    }
}
