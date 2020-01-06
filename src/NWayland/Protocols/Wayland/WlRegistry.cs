using System;
using NWayland.Core;

namespace NWayland.Protocols.Wayland
{
    public unsafe partial class WlRegistry
    {
        public T Bind<T>(uint name, IBindFactory<T> factory) where T : WlProxy
        {
            var proxy = Interop.wl_proxy_marshal_array_constructor(this.Handle, 0, new[]
            {
                name,
                WlArgument.NewId
            }, ref *factory.GetInterface());
            if (proxy == IntPtr.Zero)
                return null;
            return factory.Create(proxy, Display);
        }
    }
}