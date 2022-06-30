using System;
using NWayland.Interop;

namespace NWayland.Protocols.Wayland
{
    public sealed class WlProxyWrapper<T> : IDisposable where T : WlProxy
    {
        public WlProxyWrapper(T proxy, IBindFactory<T> bindFactory)
        {
            var handle = LibWayland.wl_proxy_create_wrapper(proxy.Handle);
            Value = bindFactory.Create(handle, proxy.Version, proxy.Display, true);
        }

        public T Value { get; }

        public void SetQueue(WlEventQueue queue) => LibWayland.wl_proxy_set_queue(Value.Handle, queue.Handle);

        public void Dispose() => LibWayland.wl_proxy_wrapper_destroy(Value.Handle);
    }
}
