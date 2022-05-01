using System;
using NWayland.Interop;

namespace NWayland.Protocols.Wayland
{
    public abstract unsafe class WlProxy : IDisposable
    {
        private readonly uint _id;

        public int Version { get; }
        public IntPtr Handle { get; }
        public WlDisplay Display { get; }

        public WlProxy(IntPtr handle, int version, WlDisplay display)
        {
            Version = version;
            Handle = handle;
            Display = display;

            if (this is WlDisplay d)
            {
                Display = d;
            }
            else
            {
                if (display is null)
                    throw new ArgumentNullException(nameof(display));
                _id = LibWayland.RegisterProxy(this);
            }
        }

        protected abstract WlInterface* GetWlInterface();

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

        protected static T? FromNative<T>(IntPtr proxy) where T : WlProxy => LibWayland.FindByNative(proxy) as T;

        protected virtual void CallWaylandDestructor() { }

        public void Dispose()
        {
            if (this is WlDisplay wlDisplay)
            {
                LibWayland.wl_display_disconnect(wlDisplay.Handle);
            }
            else
            {
                CallWaylandDestructor();
                LibWayland.UnregisterProxy(_id);
                LibWayland.wl_proxy_destroy(Handle);
            }
        }
    }
}
