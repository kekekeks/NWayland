using System;
using System.Runtime.InteropServices;
using NWayland.Interop;

namespace NWayland.Protocols.Wayland
{
    public unsafe partial class WlRegistry
    {
        public T? Bind<T>(uint name, IBindFactory<T> factory, int version) where T : WlProxy
        {
            ref var @interface = ref *factory.GetInterface();
            if (@interface.Version < version)
                throw new ArgumentException($"Requested version {version} of {Marshal.PtrToStringAnsi((IntPtr)@interface.Name)} is not supported by this version of NWayland. Bindings were generated for version {@interface.Version}");
            var args = stackalloc WlArgument[]
            {
                name,
                (IntPtr)@interface.Name,
                @interface.Version,
                IntPtr.Zero
            };
            var proxy = LibWayland.wl_proxy_marshal_array_constructor(Handle, 0, args, ref @interface);
            return proxy == IntPtr.Zero ? null : factory.Create(proxy, version, Display);
        }
    }
}
