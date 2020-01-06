using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using NWayland.Protocols.Wayland;

namespace NWayland.Core
{
    public abstract unsafe class WlProxy
    {
        public IntPtr Handle { get; }
        public WlDisplay Display { get; protected set; }

        public WlProxy(IntPtr handle, WlDisplay display)
        {
            Handle = handle;
            Display = display;
            if (this is WlDisplay d)
                Display = d;
            else
                Interop.RegisterProxy(this);
        }

        protected abstract WlInterface* GetWlInterface();

        static bool strcmp(byte* left, byte* right)
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
        
        internal unsafe void DispatchEvent(uint opcode, ref WlMessage message, WlArgument* arguments)
        {
            // Sanity checks
            // TODO: trigger a warning or something if this happens for some weird reason
            var iface = GetWlInterface();
            if (opcode >= iface->EventCount)
                return;
            var protocolMsg = iface->Events[opcode];
            if(!strcmp(protocolMsg.Name, message.Name))
                return;
            if(!strcmp(protocolMsg.Signature, message.Signature))
                return;
            DispatchEvent(opcode, arguments);
        }

        protected static T FromNative<T>(IntPtr proxy) where T : WlProxy
        {
            return Interop.FindByNative(proxy) as T;
        }
    }
}