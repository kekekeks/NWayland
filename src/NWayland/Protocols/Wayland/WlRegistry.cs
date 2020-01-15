using System;
using System.Runtime.InteropServices;
using NWayland.Core;

namespace NWayland.Protocols.Wayland
{
    public unsafe partial class WlRegistry
    {
        public T Bind<T>(uint name, IBindFactory<T> factory, int? version) where T : WlProxy
        {
            ref var iface = ref *factory.GetInterface();
            if (iface.Version < version)
                throw new ArgumentException(
                    $"Requested version {version} of {Marshal.PtrToStringAnsi((IntPtr) iface.Name)} is not supported by this version of NWayland. Bindings were generated for version {iface.Version}");
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
            return factory.Create(proxy, version ?? iface.Version, Display);
        }
    }
}