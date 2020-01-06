using System;
using NWayland.Core;

namespace NWayland.Protocols.Wayland
{
    public unsafe partial class WlRegistry
    {
        public T Bind<T>(uint name, IBindFactory<T> factory) where T : WlProxy
        {
            ref var iface = ref *factory.GetInterface();
            var args = new WlArgument[]
            {
                name,
                (IntPtr)iface.Name,
                iface.Version,
                IntPtr.Zero
            };
            var proxy = Interop.wl_proxy_marshal_array_constructor(Handle, 0, args, ref iface);
            if (proxy == IntPtr.Zero)
                return null;
            return factory.Create(proxy, Display);
        }
    }
}