using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWayland.Protocols.Wayland;

namespace NWayland.Interop
{
    public static unsafe class LibWayland
    {
        private const string Wayland = "libwayland-client.so.0";

        [DllImport(Wayland, SetLastError = true)]
        internal static extern IntPtr wl_display_connect(string? name);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_get_fd(IntPtr display);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern IntPtr wl_display_create_queue(IntPtr display);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_dispatch(IntPtr display);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_dispatch_queue(IntPtr display, IntPtr queue);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_dispatch_pending(IntPtr display);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_dispatch_queue_pending(IntPtr display, IntPtr queue);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_roundtrip(IntPtr display);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_roundtrip_queue(IntPtr display, IntPtr queue);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_prepare_read(IntPtr display);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_prepare_read_queue(IntPtr display, IntPtr queue);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_read_events(IntPtr display);

        [DllImport(Wayland, SetLastError = true)]
        internal static extern int wl_display_flush(IntPtr display);

        [DllImport(Wayland)]
        internal static extern void wl_display_cancel_read(IntPtr display);

        [DllImport(Wayland)]
        internal static extern void wl_display_disconnect(IntPtr display);

        [DllImport(Wayland)]
        internal static extern void wl_event_queue_destroy(IntPtr queue);

        [DllImport(Wayland)]
        internal static extern IntPtr wl_proxy_create_wrapper(IntPtr proxy);

        [DllImport(Wayland)]
        internal static extern void wl_proxy_set_queue(IntPtr wrapper, IntPtr queue);

        [DllImport(Wayland)]
        internal static extern void wl_proxy_marshal_array(IntPtr p, uint opcode, WlArgument* args);

        [DllImport(Wayland)]
        internal static extern IntPtr wl_proxy_marshal_array_constructor_versioned(IntPtr proxy, uint opcode, WlArgument* args, ref WlInterface @interface, uint version);

        [DllImport(Wayland)]
        private static extern int wl_proxy_add_dispatcher(IntPtr proxy, WlProxyDispatcherDelegate dispatcherFunc, IntPtr implementation, IntPtr data);

        [DllImport(Wayland)]
        private static extern uint wl_proxy_get_id(IntPtr proxy);

        [DllImport(Wayland)]
        internal static extern void wl_proxy_destroy(IntPtr proxy);

        [DllImport(Wayland)]
        internal static extern void wl_proxy_wrapper_destroy(IntPtr wrapper);

        private delegate int WlProxyDispatcherDelegate(IntPtr implementation, IntPtr target, uint opcode, ref WlMessage message, WlArgument* argument);

        private static readonly Dictionary<uint, WeakReference<WlProxy>> _proxies = new();

        private static readonly WlProxyDispatcherDelegate _dispatcher = WlProxyDispatcher;

        private static int WlProxyDispatcher(IntPtr implementation, IntPtr target, uint opcode, ref WlMessage message, WlArgument* arguments)
        {
            var id = (uint)implementation.ToPointer();

            WlProxy? proxy;
            lock (_proxies)
            {
                if (!_proxies.TryGetValue(id, out var weakRef))
                    return 0;
                if (!weakRef.TryGetTarget(out proxy))
                {
                    _proxies.Remove(id);
                    return 0;
                }
            }

            proxy.DispatchEvent(opcode, ref message, arguments);
            return 0;
        }

        public static uint RegisterProxy(WlProxy wlProxy)
        {
            lock (_proxies)
            {
                var id = wl_proxy_get_id(wlProxy.Handle);
                var idp = (IntPtr)new UIntPtr(id).ToPointer();
                var ret = wl_proxy_add_dispatcher(wlProxy.Handle, _dispatcher, idp, idp);
                if (ret == -1)
                    throw new NWaylandException($"Failed to add dispatcher for proxy of type {wlProxy.GetType().Name}");
                _proxies[id] = new WeakReference<WlProxy>(wlProxy);
                return id;
            }
        }

        public static void UnregisterProxy(uint id)
        {
            lock (_proxies)
                _proxies.Remove(id);
        }

        public static WlProxy? FindByNative(IntPtr proxy)
        {
            lock (_proxies)
            {
                var id = wl_proxy_get_id(proxy);
                if (!_proxies.TryGetValue(id, out var weakRef))
                {
                    // TODO: Investigate
                    // It's unclear if we should create a new managed object for wl_proxy here
                    // since it means that said proxy was created by the native code
                    return null;
                }

                if (!weakRef.TryGetTarget(out var target))
                    _proxies.Remove(id);
                return target;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct WlMessage
    {
        public byte* Name;
        public byte* Signature;
        public WlInterface** Types;

        public WlMessage(string name, string signature, WlInterface*[]? types)
        {
            types ??= OneNullType;
            var pTypes = (WlInterface**)Marshal.AllocHGlobal(IntPtr.Size * types.Length);
            for (var c = 0; c < types.Length; c++)
                pTypes[c] = types[c];
            Name = (byte*)Marshal.StringToHGlobalAnsi(name);
            Signature = (byte*)Marshal.StringToHGlobalAnsi(signature);
            Types = pTypes;
        }

        private static readonly WlInterface*[] OneNullType = { null };
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct WlInterface
    {
        public byte* Name;
        public int Version;
        public int MethodCount;
        public WlMessage* Methods;
        public int EventCount;
        public WlMessage* Events;

        public WlInterface(string name, int version, WlMessage[]? methods, WlMessage[]? events)
        {
            Name = (byte*)Marshal.StringToHGlobalAnsi(name);
            Version = version;
            MethodCount = methods?.Length ?? 0;
            Methods = UnmanagedCopy(methods);
            EventCount = events?.Length ?? 0;
            Events = UnmanagedCopy(events);
        }

        public static WlInterface* GeneratorAddressOf(ref WlInterface s)
        {
            fixed (WlInterface* addr = &s)
                return addr;
        }

        public static T* UnmanagedCopy<T>(T[]? arr) where T : unmanaged
        {
            if (arr is null || arr.Length == 0)
                return null;
            var ptr = (T*)Marshal.AllocHGlobal(sizeof(T) * arr.Length);
            for (var c = 0; c < arr.Length; c++)
                ptr[c] = arr[c];
            return ptr;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct WlArgument
    {
        [FieldOffset(0)]
        public int Int32;
        [FieldOffset(0)]
        public uint UInt32;
        [FieldOffset(0)]
        public IntPtr IntPtr;
        [FieldOffset(0)]
        public WlFixed WlFixed;

        public static implicit operator WlArgument(int value) => new() { Int32 = value };
        public static implicit operator WlArgument(uint value) => new() { UInt32 = value };
        public static implicit operator WlArgument(IntPtr value) => new() { IntPtr = value };
        public static implicit operator WlArgument(WlFixed value) => new() { WlFixed = value };
        public static implicit operator WlArgument(WlProxy? value) => new() { IntPtr = value?.Handle ?? IntPtr.Zero };
        public static implicit operator WlArgument(SafeHandle? value) => new() { IntPtr = value?.DangerousGetHandle() ?? IntPtr.Zero };
        public static implicit operator WlArgument(WlArray* value) => new() { IntPtr = (IntPtr)value };

        public static readonly WlArgument NewId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe ref struct WlArray
    {
        public IntPtr Size;
        public IntPtr Alloc;
        public IntPtr Data;

        public static Span<T> SpanFromWlArrayPtr<T>(IntPtr wlArrayPointer) where T : unmanaged
            => wlArrayPointer == IntPtr.Zero ? Span<T>.Empty : ((WlArray*)wlArrayPointer.ToPointer())->AsSpan<T>();

        public Span<T> AsSpan<T>() where T : unmanaged
        {
            var size = Size.ToInt32() / sizeof(T);
            return size == 0 ? Span<T>.Empty : new Span<T>(Data.ToPointer(), size);
        }

        public static WlArray FromPointer<T>(T* ptr, int count) where T : unmanaged
        {
            var size = new IntPtr(sizeof(T) * count);
            return new WlArray
            {
                Size = size,
                Alloc = size,
                Data = (IntPtr)ptr
            };
        }
    }
}
