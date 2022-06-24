using System;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgShell;

namespace SimpleWindow
{
    public class WlWindow : WlCallback.IEvents, XdgWmBase.IEvents, XdgSurface.IEvents, XdgToplevel.IEvents
    {
        private readonly WlShm _shm;
        private readonly WlSurface _surface;

        private int _width = 400;
        private int _height = 600;
        private int _bufferSize;
        private WlBuffer? _buffer;

        public WlWindow(WlShm shm, WlSurface surface)
        {
            _shm = shm;
            _surface = surface;
        }

        public bool Closed { get; private set; }

        public void OnPing(XdgWmBase eventSender, uint serial) => eventSender.Pong(serial);

        public void OnConfigure(XdgSurface eventSender, uint serial) => eventSender.AckConfigure(serial);

        public void OnConfigure(XdgToplevel eventSender, int width, int height, ReadOnlySpan<XdgToplevel.StateEnum> states)
        {
            if (width == 0 || height == 0)
                return;
            _width = width;
            _height = height;
        }

        public void OnClose(XdgToplevel eventSender) => Closed = true;

        public void OnConfigureBounds(XdgToplevel eventSender, int width, int height) { }

        public void OnDone(WlCallback eventSender, uint callbackData)
        {
            eventSender.Dispose();
            Draw();
        }

        public unsafe void Draw()
        {
            var stride = _width * 4;
            var size = stride * _height;
            if (_bufferSize != size || _buffer is null)
            {
                _buffer?.Dispose();
                var fd = FdHelper.CreateAnonymousFile(size);
                var pool = _shm.CreatePool(fd, size);
                _buffer = pool.CreateBuffer(0, _width, _height, stride, WlShm.FormatEnum.Argb8888);

                var pixels = (uint*)LibC.mmap(IntPtr.Zero, new IntPtr(size), MemoryProtection.PROT_READ | MemoryProtection.PROT_WRITE, SharingType.MAP_SHARED, fd, IntPtr.Zero);
                for (var y = 0; y < _height; y++)
                {
                    for (var x = 0; x < _width; x++)
                    {
                        if ((x + y / 8 * 8) % 16 < 8)
                            pixels[y * _width + x] = 0xFF666666;
                        else
                            pixels[y * _width + x] = 0xFFEEEEEE;
                    }
                }


                pool.Dispose();
                LibC.close(fd);
                _bufferSize = size;
            }

            _surface.Frame().Events = this;
            _surface.Attach(_buffer, 0, 0);
            _surface.Commit();
        }
    }
}
