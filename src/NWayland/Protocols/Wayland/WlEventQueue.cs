using System;
using NWayland.Interop;

namespace NWayland.Protocols.Wayland
{
    public sealed class WlEventQueue : IDisposable
    {
        public WlEventQueue(WlDisplay display)
        {
            Handle = LibWayland.wl_display_create_queue(display.Handle);
        }

        public IntPtr Handle { get; }

        public void Dispose() => LibWayland.wl_event_queue_destroy(Handle);
    }
}
