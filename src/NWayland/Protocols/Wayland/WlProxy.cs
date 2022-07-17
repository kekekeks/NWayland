using System;
using NWayland.Interop;

namespace NWayland.Protocols.Wayland
{
    public abstract unsafe class WlProxy : IDisposable
    {
        private readonly uint _id;
        private readonly bool _isWrapper;

        protected WlProxy(IntPtr handle, int version, bool isWrapper = false)
        {
            _isWrapper = isWrapper;
            Version = version;
            Handle = handle;
            if (this is WlDisplay || _isWrapper)
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

        protected virtual void CallWaylandDestructor() { }

        public void Dispose()
        {
            if (this is WlDisplay)
            {
                LibWayland.wl_display_disconnect(Handle);
            }
            else if (!_isWrapper)
            {
                CallWaylandDestructor();
                LibWayland.UnregisterProxy(_id);
                LibWayland.wl_proxy_destroy(Handle);
            }
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
