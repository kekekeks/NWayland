using System;
using System.ComponentModel;
using NWayland.Interop;

namespace NWayland.Protocols.Wayland
{
    public partial class WlDisplay
    {
        public class UnhandledEventHandlerExceptionEventArgs : EventArgs
        {
            public WlProxy Proxy { get; }
            public Exception Exception { get; }

            public UnhandledEventHandlerExceptionEventArgs(WlProxy proxy, Exception exception)
            {
                Proxy = proxy;
                Exception = exception;
            }
        }

        public event EventHandler<UnhandledEventHandlerExceptionEventArgs> UnhandledEventHandlerException;

        WlDisplay(IntPtr handle) : base(handle, 1, null)
        {
        }

        public static WlDisplay Connect(string name = null)
        {
            var handle = LibWayland.wl_display_connect(name);
            if (handle == IntPtr.Zero)
                throw new Win32Exception();
            return new WlDisplay(handle);
        }

        public int Dispatch() => LibWayland.wl_display_dispatch(Handle);
        public int Roundtrip() => LibWayland.wl_display_roundtrip(Handle);

        internal void OnUnhandledException(WlProxy proxy, Exception exception)
        {
            UnhandledEventHandlerException?.Invoke(this, new UnhandledEventHandlerExceptionEventArgs(proxy, exception));
        }
    }
}