using System;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgShell;
using OpenGL;
using OpenGL.Egl;
using OpenGL.Surfaces;

namespace EglWindow
{
    public class WlEglWindow :  EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo, IDisposable, WlCallback.IEvents, XdgWmBase.IEvents, XdgSurface.IEvents, XdgToplevel.IEvents
    {
        private readonly WlSurface _surface;
        private readonly IGlPlatformSurfaceRenderTarget _renderTarget;

        private float _t;
        private Color _color1 = Color.Red;
        private Color _color2 = Color.Blue;

        public WlEglWindow(WlDisplay display, WlSurface surface)
        {
            _surface = surface;
            var egl = EglPlatformOpenGlInterface.TryCreate(() => new EglDisplay(new EglInterface(), false, 0x31D8, display.Handle, null));
            Handle = LibWaylandEgl.wl_egl_window_create(surface.Handle, 400, 600);
            var glSurface = new EglGlPlatformSurface(egl, this);
            _renderTarget = glSurface.CreateGlRenderTarget();
        }

        public bool Closed { get; private set; }

        public IntPtr Handle { get; }

        public PixelSize Size { get; private set; }

        public double Scaling => 1;

        public void OnDone(WlCallback eventSender, uint callbackData)
        {
            eventSender.Dispose();
            Draw();
        }

        public void OnPing(XdgWmBase eventSender, uint serial) => eventSender.Pong(serial);

        public void OnConfigure(XdgSurface eventSender, uint serial) => eventSender.AckConfigure(serial);

        public void OnConfigure(XdgToplevel eventSender, int width, int height, ReadOnlySpan<XdgToplevel.StateEnum> states)
        {
            if (width == 0 || height == 0)
                return;

            Size = new PixelSize(width, height);
            LibWaylandEgl.wl_egl_window_resize(Handle, width, height, 0, 0);
        }

        public void OnClose(XdgToplevel eventSender) => Closed = true;

        public void OnConfigureBounds(XdgToplevel eventSender, int width, int height) { }

        public void OnWmCapabilities(XdgToplevel eventSender, ReadOnlySpan<XdgToplevel.WmCapabilitiesEnum> capabilities) { }

        public void Draw()
        {
            _surface.Frame().Events = this;
            _t += 0.01f;
            var t = MathF.Cos(_t) * 0.5f + 0.5f;
            var c = Color.Lerp(_color2, _color1, t);
            using var session = _renderTarget.BeginDraw();
            session.Context.GlInterface.ClearColor(c.R, c.G, c.B, 1);
            session.Context.GlInterface.Clear(GlConsts.GL_COLOR_BUFFER_BIT);
        }

        public void Dispose() => LibWaylandEgl.wl_egl_window_destroy(Handle);

        private struct Color
        {
            public float R;
            public float G;
            public float B;

            public static readonly Color Black = new() { R = 0, G = 0, B = 0 };
            public static readonly Color White = new() { R = 1, G = 1, B = 1 };
            public static readonly Color Red = new() { R = 1, G = 0, B = 0 };
            public static readonly Color Green = new() { R = 0, G = 1, B = 0 };
            public static readonly Color Blue = new() { R = 0, G = 0, B = 1 };

            public static Color Lerp(Color a, Color b, float t)
            {
                Color c;
                c.R = (1 - t) * a.R + t * b.R;
                c.G = (1 - t) * a.G + t * b.G;
                c.B = (1 - t) * a.B + t * b.B;
                return c;
            }
        }
    }
}
